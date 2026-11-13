# Phase 3A Readiness Review

## Current Status

### 結論

**技術上可沿用既有 Transaction 流程完成 Phase 3A 的單筆薪資入帳；不需要新增 API、DTO、Entity、Transaction Domain 欄位或 migration。** 但 Responsibilities 頁目前只有薪資金額的本地 state，尚未具備建立交易所需的帳戶及日期輸入，也沒有提交 handler。

因此目前狀態為：

- **Backend contract / 單筆 atomicity：就成功路徑而言已具備。**
- **Frontend：尚未就緒；需把薪資表單接上既有交易 store，並補足必要欄位與提交 UX。**
- **產品規則：可先用本文件列出的 MVP 假設開始；若需嚴格防重複或薪資跨帳戶整批原子性，則不符合目前範圍，需先另行決策。**

本 review 依據 [monthly-income-analysis.md](./monthly-income-analysis.md)、[monthly-income-transaction-design.md](./monthly-income-transaction-design.md) 與 [CoreTransactionBudgetRadarSpec.md](./CoreTransactionBudgetRadarSpec.md)，並核對目前前後端程式。Core 規格記載 Phase 3 尚在進行；目前程式已存在 BudgetRadarService/API 與收入交易查詢路徑，但該規格也列出 integration test、FixedCommitment migration 及 production validation 尚未完成。薪資交易本身使用既有 Transaction schema，不需要為此新增 migration。

### FixedCommitmentPage 現況

[FixedCommitmentPage.tsx](../LifeDashboard.Web/src/presentation/pages/FixedCommitmentPage.tsx):

- 有 `monthlyIncome` React state，並以 number input 的 `onChange` 更新字串值。
- 沒有薪資 submit handler；現有 `handleSubmit` 僅處理 FixedCommitment CRUD。
- 沒有 Account 選擇或 Account store 載入。
- 沒有薪資入帳日期欄位。
- 沒有 Category 選擇欄位或 Description 輸入欄位。
- 沒有呼叫 `transactionStore`。
- 金額雖已可輸入，但目前只是頁面 state，沒有轉成交易 payload。

### transactionStore 現況

[transactionStore.ts](../LifeDashboard.Web/src/application/stores/transactionStore.ts) 已提供 `addTransaction(payload)`，由 [TransactionRepository.ts](../LifeDashboard.Web/src/infrastructure/api/TransactionRepository.ts) 呼叫既有建立交易 API，成功後重新抓取交易清單。建立時使用 [Transaction.ts](../LifeDashboard.Web/src/domain/Transaction.ts) 的 `ICreateTransactionPayload`：

```text
accountId        必要
amount           必要
type             必要
transactionDate  必要
description      必要的 string 欄位（可用空字串）
categoryId       選填
```

使用薪資流程時，`type` 應為 `TransactionType.Income`（值為 2）。Store 不必新增薪資專用 action。

## Reusable Components

| 需求 | 現有能力 | Phase 3A 可重用方式 |
|---|---|---|
| `Amount` | Responsibilities 頁已有 `monthlyIncome` 輸入與 state | 重用輸入，但 submit 時需 parse/驗證正數並映射至 `amount`；目前尚未與 payload 相連 |
| `AccountId` | [accountStore.ts](../LifeDashboard.Web/src/application/stores/accountStore.ts) 有 `fetchAccounts` 與 `accounts`；[AccountPage.tsx](../LifeDashboard.Web/src/presentation/pages/AccountPage.tsx) 和 [TransactionPage.tsx](../LifeDashboard.Web/src/presentation/pages/TransactionPage.tsx) 已使用 | 在薪資表單載入帳戶並讓使用者明確選取，映射至 `accountId`。每筆既有 Transaction 只接受一個 AccountId |
| `TransactionDate` | Dashboard 與 TransactionPage 已使用日期字串及 `type="date"`；Dashboard 有本地日期格式 helper | 在薪資表單新增日期輸入，映射至 `transactionDate`。預設值與「實際入帳日」語意應先按 MVP 決策確認 |
| `CategoryId` | [categoryStore.ts](../LifeDashboard.Web/src/application/stores/categoryStore.ts) 有 `fetchCategories`；既有一般收入表單允許「未分類（選填）」 | 若需分類選擇，可重用分類 store 和既有 select 模式；映射至 `categoryId` 或 null。不是讓 Budget Radar 計入收入的必要條件 |
| `Description` | [ICreateTransactionPayload](../LifeDashboard.Web/src/domain/Transaction.ts) 要求 `description: string`；現有表單以空字串起始 | 可重用同一 payload 欄位；MVP 若不提供備註輸入，送空字串即可。若要固定顯示「薪資」等文案，需將其列為明確 UX 決定 |
| `Type` | [TransactionType](../LifeDashboard.Web/src/domain/Transaction.ts) 已定義 `Income` | 表單提交時固定設為 `TransactionType.Income`，不需要新增交易類型 |
| 建立與錯誤傳遞 | `transactionStore.addTransaction`、TransactionRepository 與一般交易 API 已存在 | 直接呼叫 `addTransaction`，依既有錯誤行為呈現失敗，不另建薪資 service |
| 帳戶餘額更新 | Backend TransactionService 會處理 | 不在 Frontend 修改餘額；若頁面需更新帳戶狀態，可在成功後重用 `accountStore.fetchAccounts()` |
| Budget Radar 刷新 | Dashboard 的 [dashboardSummaryStore.ts](../LifeDashboard.Web/src/application/stores/dashboardSummaryStore.ts) 有 `fetchDashboardSummary`；Dashboard 在新增交易成功後會刷新摘要 | 可讓使用者回 Dashboard 取得新摘要，或重用既有 summary store 刷新；不必新增薪資 API。Responsibilities 頁目前只有 placeholder，並未顯示真實 Radar 輸出 |

### 欄位盤點

- **Amount**：有 UI 與 local state；缺少 submit/payload 接線及 submit 時驗證。
- **AccountId**：Responsibilities 頁缺少選擇 UI；可直接重用 `accountStore`。
- **TransactionDate**：Responsibilities 頁缺少日期 UI；可重用現有 date input 模式。
- **CategoryId**：Responsibilities 頁缺少分類 UI，但 API/前端 payload 可選；可重用 `categoryStore`，是否必填待產品決定。
- **Description**：Responsibilities 頁缺少輸入 UI；payload 型別要求字串，可先傳空字串，或新增選填欄位。

## Missing Requirements

### Frontend

薪資區塊至少需要：

1. 將現有金額輸入納入薪資表單，檢查有效正數。
2. 顯示並選擇入帳帳戶；載入中、無帳戶及帳戶載入失敗須有明確 UI 狀態。
3. 提供 `TransactionDate` 日期值，不可只送金額。
4. 確認分類是否要呈現；若呈現，使用既有分類 store，Income 可不選分類。
5. 使用 `transactionStore.addTransaction` 建立 `type: Income` payload。
6. 防止同一請求期間重複按下提交按鈕，並在失敗時保留輸入及顯示錯誤。
7. 成功後顯示明確結果；如需立即反映帳戶餘額，呼叫 `accountStore.fetchAccounts()`。Budget Radar 可於使用者進入 Dashboard 時依既有流程重新讀取；若要同頁顯示最新計算結果，需使用既有 summary store，而不是在 Frontend 計算。

### Backend Contract

既有 [CreateTransactionDto.cs](../LifeDashboard.Application/Models/DTOs/CreateTransactionDto.cs) 欄位與目標 payload 對應如下：

| API payload | DTO | Contract 狀態 |
|---|---|---|
| `amount` | `Amount` | 已存在 |
| `accountId` | `AccountId` | 已存在 |
| `transactionDate` | `TransactionDate` | 已存在 |
| `categoryId?` | `CategoryId` | 已存在且 nullable |
| `description?` | `Description` | 已存在，Backend 預設空字串；前端 payload 型別仍需提供 string |
| `type: Income` | `Type` | 已存在；Income enum 值為 2 |

[TransactionController.cs](../LifeDashboard.Api/Controllers/TransactionController.cs) 已提供授權的 `POST /api/Transaction`，從 JWT 取得 UserId，呼叫 `ITransactionService.CreateTransactionAsync`，成功回傳建立的 Transaction response。

**Phase 3A 成功路徑不需要新增或修改 API、CreateTransactionDto 或 TransactionService。** 原因是現有 endpoint 已接受所有必要欄位，Service 已能建立 Income Transaction、更新 Account，並使 BudgetRadar 後續查詢到該收入。

### Controller 錯誤回應風險

TransactionService 在找不到/無權存取 Account 或 Category 時會丟 `KeyNotFoundException`。目前 `TransactionController.CreateTransaction` 明確處理 `InsufficientFundsException`、`DbUpdateConcurrencyException` 與 `ArgumentException`，沒有在該 action 捕捉 `KeyNotFoundException`。

正常 UI 先載入使用者帳戶/分類後可降低此情形，但遇到 stale ID 或跨使用者/已刪除 ID 時，錯誤可能落入未處理例外路徑，而非明確 4xx response。這是現有 contract 的錯誤回應風險，不妨礙有效 payload 的 Phase 3A 成功路徑；若要求對無效 Account/Category 回傳穩定 404/400，需另行決定是否允許修改 Controller。該錯誤映射不是新增薪資 API 的理由。

## Blocking Decisions

狀態定義：

- **已有答案**：目前 API、前端或計算規則已確定。
- **需要產品決定**：現有程式沒有定義該業務政策。
- **可以 MVP 暫時假設**：未見明確產品規則，但採用有界且符合現有行為的暫定方案即可進入實作；需在 UI/測試中明示。

| 決策 | 狀態 | 目前證據與建議的 Phase 3A 暫定方案 |
|---|---|---|
| 薪資是否只能單一帳戶？ | **已有答案**（每筆交易一個帳戶）；薪資是否可拆多帳戶仍**需要產品決定** | `CreateTransactionDto.AccountId` 是單一 Guid；MVP 可暫假設一次提交一筆薪資入一個帳戶。多帳戶需多筆交易，不是同一筆原子操作 |
| `TransactionDate` 是否代表銀行入帳日？ | **需要產品決定** | BudgetRadar 明確以 `TransactionDate` 歸入 Payday 週期，但現有規格未定義它等同銀行入帳日。MVP 可暫假設填實際入帳日，並將 label/help text 說清楚 |
| `CategoryId` 是否必填？ | **已有答案**（現有 Income contract 可為 null）；薪資產品分類政策仍**需要產品決定** | Dashboard Income 表單允許未分類，Radar 不以 Category 篩選收入；Phase 3A 可暫定選填，未選時送 null |
| 同一薪資週期是否允許多筆 Income？ | **已有答案**（系統目前允許並加總）；是否符合薪資產品規則仍**需要產品決定** | 沒有週期唯一性檢查，Radar 加總週期所有 Income；Phase 3A 可暫時維持此既有行為，不新增一個月限一筆的規則 |
| 是否需要防止重複提交？ | **需要產品決定** | API 沒有 idempotency key 或薪資唯一性檢查。同一 payload 再送一次會建立第二筆並再增加餘額；MVP 至少可在 request 進行中禁用按鈕，但不能視為伺服器端防重複保證 |
| 「本月」是自然月或 Payday 週期？ | **需要產品決定**；可以採既有 Radar 規則作 MVP 假設 | BudgetRadar 按 Payday 分週期，可跨自然月。MVP 可將新增交易依 `TransactionDate` 自動歸入既有 Payday 週期，並避免把頁面輸入月份誤述為 Radar 的自然月收入 |
| 薪資交易是否可修改？ | **需要產品決定** | 目前沒有 Transaction update API；Phase 3A 可暫不支援編輯，錯誤更正沿用既有刪除規則或人工新增調整交易，但刪除收入可能因餘額不足而失敗，不能保證可刪 |
| `Description` 是否需要使用者輸入？ | **可以 MVP 暫時假設** | DTO 預設空字串，前端型別要求 string；MVP 可不增加備註 UI 並送空字串。若要用固定標籤辨識薪資，需產品決定其文字/規則 |

### 是否阻塞 Phase 3A

若產品接受表中暫定假設，**不必等待後端架構或新增資料模型，可開始 Phase 3A 前端實作**。若產品要求以下任一項，則須先釐清並評估擴大範圍：

- 一個薪資週期只能有一筆，且必須由 Backend 強制。
- 請求重試也不可重複建立交易。
- 多帳戶薪資必須作為一個整體同成同敗。
- 薪資必須可安全修改，即使款項已部分花用。

這些要求不由目前的單筆 Create Transaction contract 保證，不能只靠前端表單完成。

## Proposed Implementation Scope

### Frontend

**建議最小直接接線範圍**

1. [FixedCommitmentPage.tsx](../LifeDashboard.Web/src/presentation/pages/FixedCommitmentPage.tsx)：將薪資區塊擴為實際入帳表單；重用 `monthlyIncome` state，新增日期/帳戶欄位、submit state/error/success feedback；組出既有 `ICreateTransactionPayload`（`type = Income`）並呼叫 `useTransactionStore().addTransaction`。
2. 使用既有 [accountStore.ts](../LifeDashboard.Web/src/application/stores/accountStore.ts) 載入帳戶，並使用既有 [categoryStore.ts](../LifeDashboard.Web/src/application/stores/categoryStore.ts)（僅在決定提供分類選擇時）。
3. [FixedCommitmentPage.css](../LifeDashboard.Web/src/presentation/pages/FixedCommitmentPage.css)：只有在新增表單欄位/提交狀態需要版面樣式時調整。
4. 重用 [transactionStore.ts](../LifeDashboard.Web/src/application/stores/transactionStore.ts)、`TransactionRepository` 與 [Transaction.ts](../LifeDashboard.Web/src/domain/Transaction.ts)；預期不需修改它們。
5. 若提交後同頁必須更新帳戶餘額顯示，呼叫既有 `accountStore.fetchAccounts()`。若只需在 Dashboard 顯示新 Radar，既有 Dashboard Summary 載入流程可重用；不需新增 Summary API。

**較小的替代方案**：不在 Responsibilities 頁提交交易，改提供導向現有 Dashboard Income 表單的入口。這樣可避免複製帳戶、日期、分類及錯誤處理 UI，但使用者輸入的 `monthlyIncome` 不會自動帶入；若要求帶入金額，需有既有導頁資料傳遞方式或另作前端設計。

### Backend

**Phase 3A 正常有效資料路徑建議不修改 Backend 檔案：**

- `CreateTransactionDto` 已符合 payload。
- `TransactionController` 已有 `POST /api/Transaction`。
- `TransactionService` 已處理 Income Transaction 與 Account balance 更新。
- `BudgetRadarService` 已加總薪資週期內 Income Transactions。

`TransactionController.cs` 的 KeyNotFound 錯誤映射可作獨立的 API 錯誤契約改善評估；非建立薪資功能的必要新增 endpoint，也不應在未核准範圍內順便修改。

### Database

**不需要 migration。** Phase 3A 使用既有 `Transactions` 欄位（AccountId、Amount、Type、CategoryId、TransactionDate、Note/Description）及既有 Account balance 欄位。

### 範圍確認

**Phase 3A 可以完全建立在既有 Transaction 流程上**，前提是定義為「新增一筆實際入帳的普通 Income Transaction」。不需要 MonthlyIncome Entity 或薪資專用 API。若需求轉為薪資週期唯一性、多帳戶整批原子性、後端防重複或未入帳計畫收入，則不再是目前既有單筆交易流程可單獨滿足的 Phase 3A 範圍。

## Risks

1. **重複請求會重複加帳**：沒有後端 idempotency 或薪資週期唯一性；前端 disabled 僅降低連按，不涵蓋網路重試或重複 request。
2. **日期語意直接影響預算週期**：BudgetRadar 依 TransactionDate 與 Payday 分期；誤用今天或薪資單日期可能使收入進入不同週期。
3. **Account / Category stale ID 的錯誤 response**：Service 的 `KeyNotFoundException` 未由 Create action 明確映射；UI 載入清單不能完全排除 stale record。
4. **交易刪除會扣回收入**：若帳戶餘額已不足以扣回，刪除會失敗；MVP 不應宣稱薪資可任意修改或撤銷。
5. **薪資與一般收入無獨立辨識**：BudgetRadar 加總所有收入，沒有薪資專用分類篩選或去重；分類目前只供交易記錄辨識。
6. **多帳戶非整體 atomic**：多筆分配是多次單筆交易，部分成功後再失敗會留下部分入帳。
7. **同頁占位預覽不會自動成為真實計算**：Responsibilities 頁的可分配金額及分配預覽仍是 placeholder。薪資交易建立後，BudgetRadar 只會在既有 Summary/Radar 查詢時反映；本 Phase 3A 不應在 Frontend 自行補算。

本次只新增 readiness 分析文件，沒有修改程式碼、API contract、資料庫或 migration。
