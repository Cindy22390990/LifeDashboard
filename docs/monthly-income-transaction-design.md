# Monthly Income → Income Transaction 設計

## 文件目的與範圍

本文件設計如何將 Responsibilities 頁的「本月實際收到的薪資」轉為既有 Income Transaction，供 Account Balance 與 Budget Radar 沿用現有資料流。

本文件是設計規格，不代表已決定所有產品行為。**本次未修改程式碼、Transaction Domain、API、資料庫或 migration。** 背景分析見 [monthly-income-analysis.md](./monthly-income-analysis.md)。

## 1. Current Architecture

### Responsibilities 薪資欄位現況

[FixedCommitmentPage.tsx](../LifeDashboard.Web/src/presentation/pages/FixedCommitmentPage.tsx) 目前將薪資輸入保存在元件內的 `monthlyIncome` state，input 的 `onChange` 只更新此 state。該頁沒有薪資 submit handler，也沒有把薪資傳給 store 或 API。

[fixedCommitmentStore.ts](../LifeDashboard.Web/src/application/stores/fixedCommitmentStore.ts) 只管理 FixedCommitment CRUD；目前沒有 MonthlyIncome store、model 或 API。因此薪資資料流在 React state 中斷：

```text
FixedCommitmentPage
    ↓
User Input
    ↓
monthlyIncome local state
    ↓
中斷：沒有 submit、store action 或 API call
    ├── Account Balance 不變
    ├── Transaction 不建立
    └── Budget Radar 不會讀取此輸入
```

### 為什麼沒有影響既有模組

- **Account Balance**：只有 TransactionService 的 Income Transaction 流程會依交易金額增加指定帳戶餘額；頁面 state 沒有呼叫此流程。
- **Transaction**：目前的薪資欄位沒有組成 `CreateTransactionDto`，因此沒有建立 `Transaction` Entity 或寫入資料庫。
- **Budget Radar**：BudgetRadarService 依 `SystemSetting.Payday` 決定薪資週期，再加總該週期的 Income Transactions。它不讀取 React state，也沒有 MonthlyIncome 資料來源。

### 已存在可重用的 Income Transaction 能力

Dashboard 現有交易表單支援收入，要求交易日期、帳戶及大於 0 的金額；收入分類目前可選填。前端以 `transactionStore` 和 `TransactionRepository` 呼叫既有交易 API。

Backend 的 `CreateTransactionDto` 已包含本設計需要的欄位：

| 輸入資料 | 既有 DTO 欄位 | 用途 |
|---|---|---|
| 薪資金額 | `Amount` | 收入金額 |
| 入帳日期 | `TransactionDate` | Budget Radar 判斷收入薪資週期的日期 |
| 入帳帳戶 | `AccountId` | 增加餘額的帳戶 |
| 分類 | `CategoryId` | 可選的既有使用者分類 |
| 交易類型 | `Type` | `Income`，目前 enum 數值為 2 |
| 備註 | `Description` | 可用於記錄薪資來源或說明 |

完整既有交易流程見 [CreateTransactionDto.cs](../LifeDashboard.Application/Models/DTOs/CreateTransactionDto.cs)、[TransactionController.cs](../LifeDashboard.Api/Controllers/TransactionController.cs)、[TransactionService.cs](../LifeDashboard.Application/Services/TransactionService.cs) 與 [BudgetRadarService.cs](../LifeDashboard.Application/Services/BudgetRadarService.cs)。

## 2. Income Transaction Flow Design

### 預期使用者輸入

未來要將薪資欄位提交為交易時，表單至少需要完整提供：

1. **薪資金額**：正數。
2. **入帳日期**：實際入帳日，映射到 `TransactionDate`。
3. **入帳帳戶**：使用者所屬且可用的 Account，映射到 `AccountId`。
4. **分類**：使用者既有 Category，映射到 `CategoryId`；是否必填由產品決定。現有一般收入表單允許不分類，Budget Radar 的收入加總也不按分類篩選。

僅有金額不足以建立目前的交易：AccountId 和 TransactionDate 都是既有新增交易流程必要資料。頁面不得假設唯一帳戶、默認日期或默認分類，除非產品明確核准該預設行為。

### 建議 payload 對應

```text
使用者輸入
  amount       = 薪資金額
  date         = 實際入帳日
  accountId    = 入帳帳戶
  categoryId   = 選填的薪資/收入分類
  description  = 使用者備註（可空）
        ↓
CreateTransactionDto
  Amount          = amount
  TransactionDate = date
  AccountId       = accountId
  CategoryId      = categoryId 或 null
  Type            = TransactionType.Income (2)
  Description     = description
```

這是對既有交易 API 的呼叫，不是新增 MonthlyIncome DTO 或薪資專用 API。

### Backend 到 Budget Radar

```text
收入表單
    ↓
transactionStore.addTransaction
    ↓
TransactionRepository.create
    ↓
POST /api/Transaction（JWT）
    ↓
TransactionController：由 JWT 取得 UserId
    ↓
TransactionService
    ├── 驗證 Amount > 0、交易類型與選填 Category
    ├── 確認 Account 屬於目前使用者
    ├── 建立 Transaction(Type = Income)
    ├── Account.CurrentBalance += Amount
    ├── 新增 Transaction
    └── SaveChanges + Commit；例外時 Rollback
    ↓
Transaction 與 Account 同步持久化
    ↓
下一次 Dashboard Summary / Budget Radar 查詢
    ├── 依 currentDate 與 SystemSetting.Payday 求出薪資週期
    ├── 依 UserId、TransactionDate 範圍及 Type = Income 加總
    └── 將週期收入交給 BudgetSnapshot 計算
```

目前 Dashboard 在新增交易成功後會重新抓取 Dashboard Summary。若由 Responsibilities 頁新增交易，該頁之後也需要明確觸發摘要刷新或導回 Dashboard；Budget Radar 不會因為資料庫有新交易而自動推送更新。

### 分類規則

一般收入交易的 `CategoryId` 目前可為 null；若有選分類，TransactionService 會確認該分類屬於目前使用者。BudgetRadarService 加總收入時不指定 `BucketType`，因此薪資分類可供交易紀錄辨識，但目前不改變收入是否納入 Budget Radar。

不應為了將薪資納入 Budget Radar 而修改 Transaction Domain。若產品要求專屬「薪資」標記、薪資報表或防重複規則，須另行定義是否可由既有 Category / Description 表達，以及是否超出本 MVP。

## 3. Transaction Boundary Review

### 新增薪資時

薪資以一般 `TransactionType.Income` 建立，符合目前單筆 Transaction Atomicity：

1. `TransactionService.CreateTransactionAsync` 在自有交易流程開始時呼叫 `IUnitOfWork.BeginTransactionAsync()`。
2. Service 載入並確認指定 Account，建立 Income Transaction。
3. 同一個 DbContext 中執行 `account.CurrentBalance += transaction.Amount`，並將 Transaction 加入 Repository。
4. `SaveChangesAsync()` 後才 `CommitAsync()`。
5. 例外時 catch 區塊呼叫 `RollbackAsync()`，避免只更新餘額或只建立交易。

[UnitOfWork.cs](../LifeDashboard.Infrastructure/Data/UnitOfWork.cs) 將 `BeginTransactionAsync`、`CommitAsync`、`RollbackAsync` 實作為資料庫交易。現有設計因此可重用，不需要薪資專用的餘額更新方式。

### 失敗情境

- 金額不大於 0、交易類型無效或分類不存在時，Service 不應提交交易。
- 指定帳戶不存在或不屬於目前使用者時，新增失敗，不應有餘額或交易的部分更新。
- `SaveChangesAsync` 或 Commit 前發生例外時，Unit of Work rollback。
- Account concurrency conflict 由 Controller 回傳 Conflict；前端應保留輸入並呈現錯誤，不能顯示成功。
- Income 本身不執行支出餘額不足檢查；它是增加帳戶餘額。

### 修改與刪除邊界

目前 Transaction API 有建立、查詢與刪除，沒有一般交易更新 endpoint；Transaction Entity 的 Amount、Type、TransactionDate 等主要欄位也不是可由一般呼叫端直接修改的 settable 欄位。因此「修改薪資」目前沒有直接的交易更新流程。

刪除 Income Transaction 會在 Unit of Work 交易中刪除 Transaction，並從帳戶餘額扣回該筆收入。若收入已花用，扣回可能觸發 `InsufficientFundsException`；因此「刪掉重建」不保證一定可用。任何薪資修正 UX 都必須避免假設刪除一定成功，也不能把交易和餘額分開修改。

### Atomicity 結論

**單筆薪資入帳**符合目前 Transaction Atomicity：一筆 Income Transaction 與一個 Account 餘額更新在同一個 UnitOfWork transaction 中保存或 rollback。

**一次薪資分配至多個帳戶**則需另外界定邊界。用多筆現有交易逐筆新增時，每筆各自 atomic，但多筆整體不是一個全有或全無的群組；若產品要求整份薪資分配必須共同成功，已超出目前單筆交易流程，需另外評估。

## 4. Product Decision Needed

以下規則不能由現有程式推定，實作前需產品確認：

| 決策 | 現況與需確認內容 |
|---|---|
| 薪資是否允許修改？ | 目前 Transaction 沒有更新 API。需決定 MVP 是否不支援修改、以刪除重建處理，或後續設計安全的修正流程。刪除收入可能因餘額不足而失敗。 |
| 一個薪資週期是否只能有一筆薪資？ | 現有 API 沒有薪資唯一性或防重複檢查；Budget Radar 會加總週期內所有收入交易。需決定允許多筆/補發/其他收入，或是否要檢查重複。 |
| 薪資日期使用發薪日還是交易日？ | Budget Radar 依 `TransactionDate` 及 Payday 切薪資週期。需確認此欄應記銀行實際入帳日、薪資單發薪日，或其他日期；不可用建立時間替代而不明示。 |
| 「本月」是自然月還是 Payday 薪資週期？ | 畫面用語為「本月」，Budget Radar 使用 Payday 週期，且週期可跨自然月。需決定欄位標題、顯示月份和交易歸屬以哪一種期間為準。 |
| 多帳戶薪資如何處理？ | 每筆既有 Income Transaction 只指定一個 Account。需決定 MVP 僅一筆入單一帳戶，或允許拆成多筆；拆分時需決定是否要求總額校驗及跨筆原子性。 |
| 分類是否必填？ | 一般收入分類目前可選填；Budget Radar 不按收入分類篩選。需決定薪資可否未分類、是否要求既有分類，或僅以 Description 辨識。 |
| 如何避免重複提交？ | 現有 API 沒有薪資週期唯一鍵或 idempotency contract。同一金額連續提交會成為多筆收入並增加餘額。需決定是否只由前端提交中禁用按鈕、提供二次確認，或另設可靠的後端防重複需求。 |
| 是否需專用薪資紀錄與報表？ | 現有模型只有通用 Income Transaction；薪資與其他收入會一起進入 Budget Radar。需決定是否要區分薪資，以及分類/備註能否滿足報表需求。 |

建議至少先確認單一帳戶、入帳日期語意、分類是否選填、重複提交處理及修改/刪除政策，才把 Responsibilities 欄位接成交易提交。

## 5. MVP Implementation Boundary

### Phase 3A — 單筆實際薪資入帳

**支援範圍**

- 將已實際收到的一筆薪資轉成一般 `Income` Transaction。
- 收集金額、實際交易日期與一個入帳帳戶；分類按產品決策選填或必填。
- 以現有 `CreateTransactionDto` / `POST /api/Transaction` / `TransactionService` 建立交易，不新增薪資 Entity、DTO、API、migration 或 Transaction Domain 欄位。
- 使用既有單筆 atomicity 更新 Account.CurrentBalance 與新增 Transaction。
- 成功後更新或重新載入 Dashboard Summary，讓該筆交易依日期和 Payday 規則進入 Budget Radar。
- 顯示 API/驗證錯誤；失敗時保留輸入，不呈現成功狀態。
- 與一般收入一致，不建立另外一筆「Budget Radar 專用收入」。

**不支援／不承諾**

- 不把薪資限制為每月唯一一筆；現有資料流會加總週期內所有 Income Transactions。
- 不支援多帳戶薪資分配的整體原子性；Phase 3A 一次一筆、一個帳戶。
- 不提供薪資專用的修改 API，也不假設刪除後重建一定成功。
- 不新增後端重複提交保護；如 MVP 只做前端提交中禁用，仍須接受重試或重複請求可能產生第二筆交易的風險。
- 不把未實際入帳的預估薪資當作 Income Transaction。

若 Responsibilities 頁繼續提供快捷輸入，它必須補足 Account 與 Date 等必要欄位並明確呈現「建立收入交易、帳戶餘額會增加」；若不增加這些欄位，最小做法是導向現有 Dashboard Income 交易表單，而不是只提交目前的金額 state。

### Phase 3B — 薪資交易管理與進階情境

依 Phase 3A 使用情況與產品決策，再評估：

- 薪資交易修改/更正流程及其餘額一致性。
- 多帳戶拆分、總額驗證，以及是否需要多筆整體 atomicity。
- 薪資週期重複偵測或 idempotency。
- 薪資與其他收入的分類、呈現、查詢與報表。
- 薪資週期歷史、月度摘要或薪資預估需求。
- 如需區分「實際收入」與「預估/計畫收入」，再設計獨立的資料來源與計算規則，避免將預估資料混入實際 Income Transaction。

Phase 3B 不預設一定新增 Entity 或 migration；應在確認需求、交易來源及歷史查詢規則後再評估。所有實際入帳仍須遵守 Transaction/Account atomicity。

## Implementation Boundary

- 本文件只定義既有收入交易能力如何承接薪資輸入，不授權或代表已實作任何變更。
- 不修改 Transaction Domain、既有 API 合約、資料庫 schema 或 migration。
- FixedCommitment 維持責任資料管理；不因建立薪資交易而自動建立固定支出交易。
- Frontend 不自行計算薪資週期或 Budget Radar；週期與預算結果沿用 Backend 現有規則。
- 唯有完成「Product Decision Needed」中的 MVP 決策後，才進入 Phase 3A 實作。
