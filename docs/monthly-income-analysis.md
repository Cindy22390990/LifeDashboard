# Monthly Income 與 LifeDashboard 架構分析

## Current State

### Responsibilities 頁面的薪資欄位

[FixedCommitmentPage.tsx](../LifeDashboard.Web/src/presentation/pages/FixedCommitmentPage.tsx) 將薪資輸入保存在元件內的 `monthlyIncome` React state，透過 input 的 `onChange` 更新。頁面沒有提交薪資的 handler；該 state 不會進入 Zustand store、API request 或持久化儲存，離開或重新載入頁面後也不會保留。

[fixedCommitmentStore.ts](../LifeDashboard.Web/src/application/stores/fixedCommitmentStore.ts) 只管理固定責任的讀取、新增、修改與刪除。[FixedCommitment.ts](../LifeDashboard.Web/src/domain/FixedCommitment.ts) 也只有固定責任資料與 payload 型別，沒有 Monthly Income model。

### Dashboard 與 Budget Radar 的收入來源

Dashboard 已有一般交易輸入表單，可選擇 `Income`、帳戶、金額與交易日期；收入不要求支出分類。[DashboardPage.tsx](../LifeDashboard.Web/src/presentation/pages/DashboardPage.tsx) 將表單資料交給 `transactionStore.addTransaction`，成功後重新取得 Dashboard Summary。[Transaction.ts](../LifeDashboard.Web/src/domain/Transaction.ts) 定義 Income 交易 payload。

Backend 的 [TransactionService.cs](../LifeDashboard.Application/Services/TransactionService.cs) 建立 Income Transaction 時，將金額加到指定帳戶 `CurrentBalance`，並透過 Unit of Work 儲存；失敗時 rollback。[TransactionController.cs](../LifeDashboard.Api/Controllers/TransactionController.cs) 提供已授權的 `POST /api/Transaction`。交易新增 DTO 包含 `AccountId`、`Amount`、`Type`、`Description`、`TransactionDate` 等欄位，見 [CreateTransactionDto.cs](../LifeDashboard.Application/Models/DTOs/CreateTransactionDto.cs)。

[BudgetRadarService.cs](../LifeDashboard.Application/Services/BudgetRadarService.cs) 依使用者 `SystemSetting.Payday` 決定薪資週期，再以 `ITransactionRepository.GetTotalAmountAsync` 加總週期內的 `TransactionType.Income`。查詢依交易日期、使用者及交易類型篩選，見 [TransactionRepository.cs](../LifeDashboard.Infrastructure/Repositories/TransactionRepository.cs)。收入不依賴分類桶。

接著 Service 將週期收入、固定責任與生活支出交給 [BudgetRadarAssembler.cs](../LifeDashboard.Application/Services/BudgetRadarAssembler.cs) 和 [BudgetSnapshot.cs](../LifeDashboard.Domain/ValueObjects/BudgetSnapshot.cs)；目前定義的 `TotalLivingBudget = 週期收入 - 固定責任總額`。Dashboard Summary 透過 [DashboardService.cs](../LifeDashboard.Application/Services/DashboardService.cs) 組合 Budget Radar、帳戶與目標，再由 [DashboardController.cs](../LifeDashboard.Api/Controllers/DashboardController.cs) 提供授權的 `GET /api/dashboard/summary?currentDate=...`。前端 [dashboardSummaryStore.ts](../LifeDashboard.Web/src/application/stores/dashboardSummaryStore.ts) 取得摘要，Dashboard 在新增交易成功後重新載入摘要。

因此，現有架構已有「建立收入交易 → 同步帳戶餘額 → Budget Radar 納入週期收入」的路徑；缺少的是 Responsibilities 頁面本地薪資欄位到該路徑的連接。

## Data Flow Diagram

### 目前 Responsibilities 薪資欄位

```text
使用者輸入薪資
    ↓
FixedCommitmentPage.tsx 的 monthlyIncome React state
    ↓
中斷：沒有呼叫 fixedCommitmentStore 或其他 store
    ↓
沒有 API request
    ↓
沒有 Domain Entity / Database 寫入
    ↓
BudgetRadar 不會讀到這個值
```

### 現有 Income Transaction 路徑

```text
Dashboard Income 表單
    ↓
transactionStore.addTransaction
    ↓
POST /api/Transaction（CreateTransactionDto）
    ↓
TransactionService：新增 Income Transaction
    ├── 更新指定 Account.CurrentBalance
    └── UnitOfWork transaction：SaveChanges + Commit；例外時 Rollback
    ↓
Transaction / Account 持久化
    ↓
BudgetRadarService：依 Payday 算薪資週期
    ↓
TransactionRepository：加總週期內 Income Transactions
    ↓
BudgetSnapshot：收入 - 固定責任 - 生活支出
    ↓
Dashboard Summary / BudgetRadarPanel
```

### 欄位中斷位置

薪資輸入在 React 元件 state 之後即中斷；不是 store 保存失敗，也不是 API 或資料庫拒絕，而是目前沒有任何 submit 或資料流把此欄位送出。另一方面，Budget Radar 的收入來源已明確是 Income Transactions，而不是 Responsibilities 頁面的輸入值。

## Option Comparison

| 面向 | 方案 A：Budget Radar 專用輸入，不建交易 | 方案 B：以 Income Transaction 表示 |
|---|---|---|
| 資料語意 | 預算計算用收入值，不代表帳戶實際入帳 | 一筆實際收入入帳紀錄，並且指定入帳帳戶 |
| Budget Radar | 需要明確新增另一個收入來源；目前 Radar 只加總 Income Transactions | 已有的週期收入查詢可直接納入 |
| 帳戶餘額 | 不更新 Account；若實際已收到薪資，帳戶總額不會隨此輸入同步 | 建立交易時增加指定 Account.CurrentBalance |
| 持久化 | 若僅送一次計算 request，刷新後或歷史查詢無資料；若要保存，需定義按月資料與讀寫路徑 | 以既有 Transaction 持久化，並沿用交易清單、日期與刪除流程 |
| 需新增/調整 | 至少需要定義 Budget Radar 如何接收此值、如何辨識所屬週期及缺值行為。若要跨頁、刷新或歷史查詢保留，還需持久化模型、API/DTO 與前端 state/service | 不需要新增收入 Entity、DTO 或 API；若直接由現有交易表單記錄，使用現有 Income 交易路徑即可。若要在 Responsibilities 頁新增快捷入口，仍需處理帳戶選擇、日期及交易提交 |
| 對既有模組影響 | Budget Radar Application/API、前端 store/service 與資料來源；若持久化則另及 Domain、Infrastructure、migration | Transaction、Account balance、Dashboard refresh 與 Budget Radar 的既有整合 |
| 主要限制/風險 | 成為與 Transaction 並行的第二個收入來源；可能與已記錄薪資重複計算，也不代表帳戶資產 | 每筆收入必須指定帳戶與交易日期；若只輸入一個薪資總額，使用者仍需決定應記入哪個帳戶。薪資週期由 Payday 決定，不必然等同自然月 |

### Option A 所需的具體架構決定

若採用 A，必須先決定輸入值是：

1. 只在當次計算 request 使用的暫時參數，或
2. 需要保存並供重新載入、跨頁、歷史預算查詢使用的「某薪資週期收入」資料。

這兩種做法的 API、持久化需求和歷史查詢行為不同。目前專案沒有 Monthly Income Entity、DTO、store 或 API，因此不能把目前頁面 state 視為已存在的預算輸入契約。

### Option B 與 Transaction Atomicity

既有 Income Transaction 流程符合專案的交易原子性方向：`TransactionService` 在自有交易流程中先 Begin，建立交易並更新 Account，執行 `SaveChangesAsync` 後 Commit；發生例外時 Rollback。刪除一般收入交易時也在 Unit of Work transaction 中刪除交易並反向調整餘額。這避免「收入交易存在但帳戶餘額未同步」的分離寫入。

因此 B 不需要建立新的薪資 Domain；收入仍使用 `Transaction` Entity 與 `TransactionType.Income`。需要留意，將薪資欄位直接轉成交易會使帳戶餘額增加，這是它與 A 最重要的行為差異。

## Recommended Architecture

### Phase 3 Budget Radar MVP

依現有程式與 [CoreTransactionBudgetRadarSpec.md](./CoreTransactionBudgetRadarSpec.md) 的收入計算規則，Phase 3 MVP 最一致且最小的方案是 **沿用方案 B：Budget Radar 的收入只以薪資週期內已記錄的 Income Transactions 為準**。

理由：

- Budget Radar Service 已按 `Payday` 算出週期，並加總該週期的 Income Transactions；這是目前實作與規格文件已有的來源，不需引入另一個預算收入來源。
- Income Transaction 已有 API、DTO、Domain Entity、前端 store 和帳戶餘額更新流程。
- `DashboardPage` 在新增交易後會重新抓取 Dashboard Summary，因此新增的 Income Transaction 可由既有路徑反映到 Budget Radar。
- Fixed Commitment 規格明定固定責任在 Budget Radar 計算時扣除，不自動建立 Transaction。收入則採用現有交易規格，不應在固定責任資料或本地頁面 state 中另行推算。

對現有「本月實際收到的薪資」欄位，MVP 不應直接把 React state 當成 Budget Radar 的另一個輸入，也不應默默建立收入交易。若產品決定此欄位代表「實際入帳薪資」，後續可採以下其中一種明確 UX：

- 引導使用者使用 Dashboard 現有的 Income Transaction 表單記錄；或
- 經需求確認後，提供一個呼叫現有交易流程的快捷入口，仍必須明確取得入帳帳戶與交易日期。

前者是最小、已存在的操作方式。後者是頁面 UX 改進，並非目前架構缺少薪資儲存 API。

### 未來擴充方向

若日後需求是「不調整帳戶餘額的計畫收入、預估薪資或替代收入假設」，再考慮方案 A。屆時應先定義週期識別、保存期限、歷史查詢與如何避免和實際 Income Transactions 重複計算，再選擇暫時計算輸入或持久化週期收入資料。這是新的預算來源，不應混同於實際入帳交易。

如果產品要求在 Responsibilities 頁直接登錄實際薪資，可優先重用現有 Transaction API/Service/Repository/Entity；新增的前端入口仍須遵守交易表單必要欄位與既有 atomicity，不應在 UI 自行改帳戶餘額。

## Risks

1. **重複計算收入**：同一筆薪資若既建立 Income Transaction，又被另外的 Monthly Income 欄位送入 Budget Radar，會重複計入。
2. **「本月」與薪資週期不一致**：Responsibilities 欄位標示「本月」，但 Budget Radar 使用 `SystemSetting.Payday` 切分薪資週期；跨月日期的收入依交易日期歸入週期，不按自然月輸入月份歸屬。
3. **帳戶資訊不可省略（方案 B）**：既有交易 payload 要求 `accountId`。單一金額欄位不足以建立有效收入交易，必須由使用者選擇或確認入帳帳戶。
4. **一個欄位與多筆收入**：Budget Radar 會加總週期內 Income Transactions；目前沒有獨立的「薪資」交易類別或專用去重規則。多筆薪資/其他收入會一起計入週期收入。
5. **交易刪除會反向調整餘額**：收入不是只供預算顯示；透過交易建立後，刪除交易會依現有流程反向調整 Account balance。
6. **Summary 不提供原始薪資總額**：`RadarStateDto` 目前回傳 DailySafeLimit、TodaySpent、TodayRemaining、RemainingDays、TotalLivingBudget 與 UncategorizedExpenseAmount，沒有直接回傳 `PeriodIncome` 或 `FixedCommitmentTotal`。目前可取得計算後預算，但無法只靠這個 DTO 呈現等式中的每個原始項目。
7. **現有薪資輸入不持久化**：目前欄位只適合作為尚未接通的 UI 草稿；重新整理後不保留，也不會影響任何預算。

尚需產品明確決定、不能由現有程式推定的事項：直接在 Responsibilities 頁輸入時要記入哪個帳戶、交易日期採何值，以及「薪資」是否應與其他 Income Transaction 分開顯示。這些決定不影響以既有交易作為 Phase 3 預算收入來源的建議。

## Implementation Boundary

本文件為資料流與架構分析；沒有修改程式碼或提出本階段必須新增的 Domain、DTO、API 或 migration。

若依建議落地最小 MVP：

- Budget Radar 收入來源維持目前的週期 Income Transaction 加總。
- 固定責任仍依既有有效期間資料扣除，且不自動建立付款交易。
- 實際薪資需透過現有 Income Transaction 流程記錄；不從 Responsibilities 的本地 state 另行計算。
- 不在 Frontend 計算週期收入、固定責任總額或預算結果；由 Backend Budget Radar 負責計算。
- 在決定將收入交易入口放到 Responsibilities 頁之前，先確認帳戶、日期與薪資分類/顯示規則；不得略過既有帳戶 balance atomicity。
- 若要採方案 A，應視為獨立需求與資料來源設計，先完成上述週期、持久化、歷史查詢及避免重複計算的規格，再評估 API/DTO/Domain 範圍。

本次未更動任何 code、API contract、資料庫或 migration。
