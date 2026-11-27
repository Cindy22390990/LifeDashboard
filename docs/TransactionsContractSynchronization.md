# Transactions Contract Synchronization

## 1. Final Contract Status

Status:

**Frozen — Transactions Read API scope**

`GET /api/transactions` 已依 Phase 4.1 實作與驗證，可視為 Transactions Read Contract Freeze。此凍結範圍不包含新增、編輯、刪除、匯入、匯出或 Quick Create 的重新契約設計。

---

## 2. Final API Contract

### Endpoint

`GET /api/transactions`

Authentication:

- `Authorization: Bearer <JWT>`
- 使用 Claims `UserId` 隔離資料
- 排除 soft-deleted transactions

### Query Parameters

| Parameter | Status | Description |
|---|---|---|
| `page` | Supported | 頁碼，預設 `1`，最小值 `1`。 |
| `pageSize` | Supported | 每頁筆數，預設 `20`，範圍 `1`–`100`。 |
| `keyword` | Supported | 模糊比對交易 `Note`，不含 category-name 搜尋。 |
| `accountId` | Supported | `Guid` 精確帳戶篩選。 |
| `categoryId` | Supported | `Guid` 精確分類篩選。 |
| `transactionType` | Supported | `Expense` / `Income` / `Transfer`。 |
| `startDate` | Supported | `YYYY-MM-DD` inclusive lower bound。 |
| `endDate` | Supported | `YYYY-MM-DD` inclusive upper bound。 |
| `sortBy` | Supported | allowlist：`transactionDate` / `amount` / `note`。 |
| `sortDirection` | Supported | `asc` / `desc`，預設 `desc`。 |
| `year` | Legacy Compatible | 與 `month` 成對使用；未提供 `date` 時使用月份範圍。 |
| `month` | Legacy Compatible | `1`–`12`；與 `year` 成對使用。 |
| `date` | Legacy Compatible | `YYYY-MM-DD`；優先於 `year` / `month`。 |
| `type` | Legacy Compatible | 與 `transactionType` 相同交易類型語意；兩者衝突時回傳 `400`。 |
| `categoryName` | Not Defined | 無獨立 category-name 搜尋參數。 |
| `search` | Not Defined | 無通用搜尋參數別名。 |
| `offset` / `cursor` | Not Defined | 不使用 offset/cursor pagination。 |

Additional validation:

- 未提供 `date` 時，`year` 與 `month` 必須成對提供。
- `startDate` 不可晚於 `endDate`。
- 不支援的 `transactionType`、`sortBy` 或 `sortDirection` 回傳 RFC 7807 `400 ProblemDetails`。
- `page` 或 `pageSize` 超出限制時回傳 `400 ProblemDetails`。

---

## 3. Response Contract

### Response Fields

| Field | Type | Status | Meaning |
|---|---|---|---|
| `totalIncome` | `decimal` | Confirmed | 所有符合篩選條件的交易收入總額，排除 `Transfer`。 |
| `totalExpense` | `decimal` | Confirmed | 所有符合篩選條件的交易支出總額，排除 `Transfer`。 |
| `items` | `TransactionItemDto[]` | Confirmed | 當前頁交易項目；空資料為 `[]`。 |
| `items[].id` | `Guid` | Confirmed | 交易識別碼。 |
| `items[].note` | `string \| null` | Confirmed | 交易備註。 |
| `items[].amount` | `decimal` | Confirmed | 交易金額。 |
| `items[].transactionType` | `Expense` / `Income` / `Transfer` | Confirmed | Canonical transaction type。 |
| `items[].transactionDate` | `DateOnly` (`YYYY-MM-DD`) | Confirmed | 交易業務日期。 |
| `items[].accountId` | `Guid \| null` | Confirmed | 交易帳戶識別碼。 |
| `items[].accountName` | `string \| null` | Confirmed | 交易帳戶名稱。 |
| `items[].categoryId` | `Guid \| null` | Confirmed | 交易分類識別碼。 |
| `items[].categoryName` | `string \| null` | Confirmed | 交易分類名稱。 |
| `totalCount` | `int` | Removed | 不再包含於實作 Response。 |
| `page` | `int` | Removed | 不再包含於實作 Response；僅為 Request。 |
| `pageSize` | `int` | Removed | 不再包含於實作 Response；僅為 Request。 |

---

## 4. Pagination Decision

- 不回傳 `totalCount`。
- 不回傳 `page`。
- 不回傳 `pageSize`。
- Backend 使用 `page` 與 `pageSize` 執行 SQL 分頁。
- `totalIncome` 與 `totalExpense` 彙整整個篩選結果，不只彙整目前頁。
- 前端使用 TanStack Query Infinite/Load More 模式。
- 每次載入以滿頁判斷是否有下一頁：若回傳筆數等於 `pageSize`，嘗試下一頁。
- 此模式不提供確切最後一頁；若資料筆數剛好是 `pageSize` 的倍數，最後可能多載一次空頁。
- 目前未實作 Infinite Scroll，自動捲動或瀏覽器 scrollbar 觸發；目前使用明確的 `Load More` 操作。

Conflict Source:

1. `DashboardV4FrontendIntegrationReadinessReview.md`
2. `DashboardV4ApiContractPlan.md`
3. 原先的 Frontend `TransactionListResponseDto`

Conflict Detail:

- 歷史文件曾將 `totalCount`、`page`、`pageSize` 描述為 Response fields。
- 實作 Response 不包含這三個欄位。
- 原實作的分頁回應與 Readiness Review 先前假設不一致。

Recommended Update:

- 以本文件及已更新的 `DashboardV4BackendContract.md` 作為 Transactions Read Contract。
- Frontend 共用 DTO 若未同步，仍不得視為實作契約；後續只需在允許修改 Frontend 時同步型別，不在本次同步範圍內修改程式。

---

## 5. Document Synchronization Plan

| File | Current | Need Update |
|---|---|---|
| `DashboardV4BackendContract.md` | 已更新 Transactions GET query、response、pagination 與 sorting。 | 無；Transaction 區段已與實作同步。 |
| `DashboardV4FrontendIntegrationReadinessReview.md` | 已修正 Transactions response 不含 pagination metadata。 | 無；歷史分頁假設已標示為已移除。 |
| `DashboardV4ApiContractPlan.md` | 已將 Transactions GET parameters、response及 Open Questions 更新為實作結果。 | 無；POST/CRUD/Quick Create 仍不在本次凍結範圍。 |
| `TransactionsApiImplementationReport.md` | 已記錄實作、差異及驗證。 | 保留作 Phase 4.1 實作證據，不改歷史記錄。 |
| `DashboardV4ProductSpecification.md` | 仍保留搜尋、篩選、Load More 等產品意圖與待確認項目。 | 不在本次同步範圍；後續產品決策需另行更新。 |
| `DashboardV4DomainModel.md` | Transaction 為 Partial/文域層整理。 | 不在本次同步範圍；已確認與實作衝突需以 API report 註記時另行處理。 |

---

## 6. Final Decision

**Transactions Read API Contract: Frozen**

凍結內容：

- endpoint
- query parameter names and status
- response fields
- backend pagination behavior
- totals semantics
- filter behavior
- sort allowlist and direction

Remaining Open Questions:

- `keyword` 是否應一併比對 category name。
- UI 的「全部」是否包含 `Transfer`。
- 是否需要精確的下一頁結束訊號，而不是滿頁判斷。
- Load More 最終狀態是否要顯示明確的「已載入全部」。
