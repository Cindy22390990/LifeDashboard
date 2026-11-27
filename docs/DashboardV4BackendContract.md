# Dashboard V4 Backend Contract

> **Document Path**: `docs/DashboardV4BackendContract.md`
>
> **Version**: 1.0
>
> **Status**: Draft — Pending Architecture Review
>
> **Source of Truth**: YES（Dashboard V4 前後端契約唯一依據）
>
> **Reference**: `docs/DashboardV4UXArchitecture.md` (v1.1)

---

## 1. Architecture & Boundary Principles

1. **Bounded Context 分離**：`Plan` 僅為前端 UI Tab，後端嚴格維持 `Budget` 與 `Goal` 兩個獨立領域，**禁止建立 `GET /api/plan`**。
2. **每頁最小資料集**：`Home` 與 `Me` 各自擁有輕量專屬端點，**禁止**將 `accounts[]`、`goals[]`、`charts[]` 塞入 Home API，亦**禁止** `Me` 複用 V3 Summary API。
3. **單一計算真理（Backend Calculation Owner）**：所有帳務聚合、每日可用額度與儲蓄進度皆由 Backend 計算並輸出具備明確業務語意的數值；Frontend **僅負責展示（Only Display）** 與格式化。
4. **認證與隔離**：所有端點皆須 JWT Bearer 驗證，強制依 Claims `UserId` 隔離資料並過濾 `!IsDeleted`。

---

## 2. Home API（今日消費決策）

### `GET /api/dashboard/v4/home`

* **Purpose**：提供 `HomePage` 首屏所需的今日消費額度、本月儲蓄／投資進度與今日交易清單。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：無
* **Response**：`200 OK` → `DashboardV4HomeDto`

#### Response DTO：`DashboardV4HomeDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `businessDate` | `DateOnly` (`YYYY-MM-DD`) | No | 後端計算今日數據所依據的業務基準日。 |
| `dailySpendingLimit` | `decimal` | No | 今日可花上限（基準額度）。 |
| `dailySpent` | `decimal` | No | 今日已支出總額（僅計 `Expense`）。 |
| `dailyRemaining` | `decimal` | No | 今日剩餘可花金額（可為負數，代表今日超支）。 |
| `monthlyProgress` | `MonthlyProgressDto` | No | 本月長期財務進度（儲蓄與投資）。 |
| `todayTransactions` | `HomeTransactionItemDto[]` | No | `businessDate` 當日交易紀錄（依 `CreatedAtUtc DESC` 排序；無資料回傳 `[]`）。 |

#### Child DTO：`MonthlyProgressDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `saved` | `decimal` | No | 本月累計儲蓄金額。 |
| `invested` | `decimal` | No | 本月累計投資金額。 |

#### Child DTO：`HomeTransactionItemDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 交易唯一識別碼。 |
| `note` | `string` | **Yes** | 交易備註（前端須處理 `note?.trim()` 空值退回預設標籤）。 |
| `amount` | `decimal` | No | 交易金額（純數值）。 |
| `transactionType` | `string` | No | Canonical String（`Expense` / `Income` / `Transfer`）。 |
| `transactionDate` | `DateOnly` (`YYYY-MM-DD`) | No | 交易業務日期。 |

#### Business Rules & Calculation Owner

| Metric | Calculation Owner | Formula / Business Rule | Frontend Responsibility |
| :--- | :--- | :--- | :--- |
| `dailySpendingLimit` | Backend `BudgetService` | `AvailableMonthlyBudget / RemainingDays`（若使用者未設定月預算，則為 `0`；無條件捨去或四捨五入至整數由後端統一）。 | **Only Display** |
| `dailySpent` | Backend `TransactionQuery` | `SUM(Amount)` where `TransactionDate == businessDate && TransactionType == Expense && !IsDeleted`。排除 `Transfer`。 | **Only Display** |
| `dailyRemaining` | Backend `BudgetService` | `dailySpendingLimit - dailySpent`。 | **Only Display**（負數顯示警示色） |
| `monthlyProgress.saved` | Backend `AllocationService` / Query | 本月流入 `AccountType == Saving` 帳戶或標記為儲蓄配置之金額總和（精確口徑 TBD）。 | **Only Display** |
| `monthlyProgress.invested` | Backend `AllocationService` / Query | 本月流入 `AccountType == Invest` 帳戶或標記為投資配置之金額總和（精確口徑 TBD）。 | **Only Display** |

---

## 3. Transactions API（交易導向 RESTful 契約）

> **邊界規則**：不複用或擴充 `/api/dashboard/v3/calendar`。V4 月曆僅作為前端 Filter UI，透過 `date` 參數查詢本組端點。

### 3.1 `GET /api/transactions`

* **Purpose**：提供 `TransactionsPage` 依月份、特定日期、關鍵字、帳戶、分類、交易類型、日期區間及排序查詢交易列表與區間收支摘要。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：
    * `year`（選填，`int`，例如 `2026`；未提供 `date` 時與 `month` 成對必填）
    * `month`（選填，`int`，`1`–`12`）
    * `date`（選填，`YYYY-MM-DD`，月曆 Filter 選取單日時使用，優先度高於 `year`/`month`）
    * `keyword`（選填，`string`，模糊比對 `Note`）
    * `type`（Legacy compatible，選填，`Expense` / `Income` / `Transfer`）
    * `transactionType`（選填，`Expense` / `Income` / `Transfer`）
    * `accountId`（選填，`Guid`，依交易帳戶精確篩選）
    * `categoryId`（選填，`Guid`，依交易分類精確篩選）
    * `startDate`（選填，`YYYY-MM-DD`，交易日期下限）
    * `endDate`（選填，`YYYY-MM-DD`，交易日期上限）
    * `sortBy`（選填，`transactionDate` / `amount` / `note`）
    * `sortDirection`（選填，`asc` / `desc`，預設 `desc`）
    * `page`（選填，`int`，預設 `1`；至少為 `1`）
    * `pageSize`（選填，`int`，預設 `20`；範圍 `1`–`100`）
  * **Compatibility & Validation**：
    * `date` 優先於 `year`/`month`。
    * `year` 與 `month` 必須成對提供；未提供 `date` 時不可只提供其中一個。
    * `startDate` 不可晚於 `endDate`。
    * `type` 與 `transactionType` 同時提供且值不同時，API 回傳 `400 ProblemDetails`；未衝突時視為同一交易類型篩選。
    * 不支援的 `transactionType`、`sortBy` 或 `sortDirection` 回傳 `400 ProblemDetails`。
* **Response**：`200 OK` → `TransactionListResponseDto`

#### Response DTO：`TransactionListResponseDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `totalIncome` | `decimal` | No | 符合篩選條件之總收入（排除 `Transfer`）。 |
| `totalExpense` | `decimal` | No | 符合篩選條件之總支出（排除 `Transfer`）。 |
| `items` | `TransactionItemDto[]` | No | 當前頁交易項目；無資料回傳 `[]`。 |

#### Item DTO：`TransactionItemDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 交易唯一識別碼。 |
| `note` | `string` | Yes | 交易備註；前端可退回預設標籤。 |
| `amount` | `decimal` | No | 交易金額。 |
| `transactionType` | `string` | No | Canonical String（`Expense` / `Income` / `Transfer`）。 |
| `transactionDate` | `DateOnly` (`YYYY-MM-DD`) | No | 交易業務日期。 |
| `accountId` | `Guid` | Yes | 交易帳戶識別碼。 |
| `accountName` | `string` | Yes | 交易帳戶名稱。 |
| `categoryId` | `Guid` | Yes | 交易分類識別碼。 |
| `categoryName` | `string` | Yes | 交易分類名稱。 |

#### Pagination & Sorting

* Backend 使用 `page` 與 `pageSize` 執行資料分頁。
* Response 不回傳 `totalCount`、`page` 或 `pageSize`；分頁 metadata 由前端依 request 與滿頁判斷維持。
* 預設排序：`TransactionDate DESC, CreatedAtUtc DESC, Id DESC`。
* `sortBy=transactionDate` 時維持交易日期／建立時間／ID 的穩定排序。
* `sortBy=amount` 或 `sortBy=note` 時，依指定欄位排序，再以交易日期、建立時間、ID 作為 tie-breaker。
* `totalIncome` 與 `totalExpense` 聚合所有符合篩選條件的交易，不限於當前頁；`Transfer` 不納入兩項總額。

* **Calculation Owner**：`totalIncome` 與 `totalExpense` 由 Backend Query Layer 於資料庫層聚合計算；Frontend **Only Display**，不可自行加總 `items[]`。

### 3.2 `GET /api/transactions/{id}` / `POST /api/transactions` / `DELETE /api/transactions/{id}`

* **Purpose**：單筆交易詳情查詢、新增交易（供 `TransactionBottomSheet` 呼叫）與軟刪除交易。
* **Request / Response**：沿用既有 `Transaction` CRUD 契約。`POST /api/transactions` 須保留 `categoryId` 為選填欄位以支援未來擴充，並支援完整 `TransactionType`（`1 Expense`, `2 Income`, `3 Transfer`）。
* **Business Rule**：`POST` 與 `DELETE` 須於同一 Transaction 內同步更新對應 `Account.CurrentBalance`。

### 3.3 `POST /api/transactions/quick`

* **Purpose**：建立快速交易；Request 僅包含金額、備註與交易類型，Account、Category 與業務日期由 Backend Application 層解析。
* **Default Account**：有效偏好帳戶（必須屬於目前使用者、未刪除、非系統且為 `Cashflow`）→ 最近 30 天 Expense 使用最多的有效 Cashflow 帳戶 → 最早建立的有效 Cashflow 帳戶。
* **Default Category**：有效 User Default Category → Recent Category → Most Used Category (30 days) → Uncategorized。
* **No Cashflow Account**：HTTP `422 Unprocessable Entity`，RFC 7807 ProblemDetails type 為 `https://api.lifedashboard/errors/no-cashflow-account`。
* **Transfer**：Quick Create 不接受 `Transfer` 類型。

---

## 4. Plan APIs（獨立 Bounded Contexts）

### 4.1 Budget Context：`GET /api/budgets/current`（Planned）

* **Purpose**：提供 `PlanPage → BudgetView` 本月剩餘預算、固定支出預留總額與各分類進度。
* **Request**：Header `Authorization: Bearer <JWT>`；Query `year` / `month`（選填，預設當月）。
* **Response**：`200 OK` → `MonthlyBudgetSummaryDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `totalBudget` | `decimal` | No | 本月設定總預算。 |
| `totalSpent` | `decimal` | No | 本月已支出總額。 |
| `remainingBudget` | `decimal` | No | 本月剩餘預算（`totalBudget - totalSpent`）。 |
| `fixedCommitmentReserved` | `decimal` | No | 本月固定支出已預留總額。 |
| `categories` | `CategoryBudgetItemDto[]` | No | 各分類預算進度清單（`categoryId`, `categoryName`, `limit`, `spent`, `remaining`）；無資料回傳 `[]`。 |

* **Calculation Owner**：Backend `BudgetService` 計算所有額度與剩餘金額；Frontend **Only Display**（僅允許計算純 UI 進度條百分比 `spent / limit * 100`）。

### 4.2 Goal Context：`GET /api/goals/summary`（Planned）

* **Purpose**：提供 `PlanPage → GoalView` 目標清單與達成進度。
* **Request**：Header `Authorization: Bearer <JWT>`。
* **Response**：`200 OK` → `GoalSummaryItemDto[]`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 目標識別碼。 |
| `name` | `string` | No | 目標名稱（如「日本旅遊」）。 |
| `targetAmount` | `decimal` | No | 目標金額。 |
| `currentAmount` | `decimal` | No | 已配置資金總額（來自 `GoalFundingSummary` Read Model）。 |
| `progressPercent` | `decimal` | No | 達成百分比（`0`–`100`）。 |

* **Calculation Owner**：Backend `GoalFundingSummary` Read Model 計算 `currentAmount` 與 `progressPercent`；Frontend **Only Display**。

----

## 5. Accounts API（Accounts Read Model v1）

### `GET /api/accounts`

* **Purpose**：提供 Accounts Page 所需的使用者帳戶清單及總資產摘要。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：無
  * Request Body：無
* **Response**：`200 OK` → `AccountsReadDto`

#### Response DTO：`AccountsReadDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `totalAssets` | `decimal` | No | 所有使用者可用帳戶的 `CurrentBalance` 總和；排除 soft-deleted 與 system accounts。 |
| `items` | `AccountReadItemDto[]` | No | 可用帳戶清單；無資料回傳 `[]`。 |

#### Child DTO：`AccountReadItemDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 帳戶唯一識別碼。 |
| `name` | `string` | No | 帳戶名稱。 |
| `accountType` | `string` | No | Canonical String（`Cashflow` / `Saving` / `Invest` / `Unknown`）。 |
| `balance` | `decimal` | No | 帳戶目前餘額。 |

#### Business Rules

* Only active, non-system accounts are included: `UserId == currentUserId && !IsDeleted && !IsSystemAccount`.
* Backend calculates `totalAssets`; frontend does not sum `items[]`.
* No pagination, type filter, include-inactive flag, keyword filter or Account CRUD is included in v1.
* Domain `Investment` enum is canonically represented as `Invest` in this V4 read DTO.
* Liability handling and net-worth semantics are deferred; `totalAssets` is not a net-worth claim.

---

## 6. Me API（資產與帳戶總覽）

### `GET /api/me/assets`

* **Purpose**：提供 `MePage` 頂部「資產總覽」與依類型分組之帳戶清單，不載入任何交易或目標資料。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：無
* **Response**：`200 OK` → `MeAssetsDto`

#### Response DTO：`MeAssetsDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `totalAssets` | `decimal` | No | 所有非刪除且非系統帳戶之 `CurrentBalance` 總和。 |
| `accounts` | `MeAccountItemDto[]` | No | 活躍帳戶清單；無資料回傳 `[]`。 |

#### Child DTO：`MeAccountItemDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 帳戶識別碼。 |
| `name` | `string` | No | 帳戶名稱（使用者自訂，不翻譯）。 |
| `accountType` | `string` | No | Canonical String（`Cashflow` / `Saving` / `Invest`）。 |
| `balance` | `decimal` | No | 帳戶目前餘額（純數值）。 |

#### Business Rules & Calculation Owner
* **Calculation Owner**：Backend `AccountQuery` 過濾 `UserId == currentUserId && !IsDeleted && !IsSystemAccount` 後加總 `totalAssets`。
* **Frontend Responsibility**：**Only Display**（依 `accountType` 分組渲染，並套用 `privacyStore` 遮罩；禁止自行加總 `accounts[]`）。

---

## 7. Categories API（Category Read Model v1）

### `GET /api/categories`

* **Purpose**：提供 Categories Page 與現有交易分類選擇器可讀取的使用者分類清單。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：無
  * Request Body：無
* **Response**：`200 OK` → `CategoryReadDto[]`

#### Response DTO：`CategoryReadDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 分類唯一識別碼。 |
| `name` | `string` | No | 分類名稱。 |
| `icon` | `string` | No | 分類圖示文字或既有選定值。 |
| `bucketType` | `BucketType` (`1`–`5`) | No | 既有 `BucketType` 枚舉值。 |
| `createdAt` | `DateTime` | No | 分類建立時間。 |

#### BucketType Values

| Value | Name |
|---:|---|
| `1` | Living |
| `2` | Entertainment |
| `3` | Savings |
| `4` | Investment |
| `5` | Responsibility |

#### Business Rules

* Categories are user-scoped: `UserId == currentUserId`.
* Empty result is `[]`.
* Read results are ordered by `Name`.
* No Expense/Income category type is inferred.
* No category status, display order, default marker or current-month usage count is added.
* Category CRUD, budget category semantics and allocation/reporting rules are out of scope.

---

## 8. Global Contract Rules

* **Money Format**：所有金額一律以未格式化純數值 `decimal`（JSON `number`）傳輸；Frontend 透過 `Intl.NumberFormat` 與 `privacyStore` 格式化。
* **DateOnly**：所有業務日期固定為 ISO 8601 純日期字串 `YYYY-MM-DD`；Frontend 嚴禁使用 `new Date("YYYY-MM-DD")`。
* **Canonical Enum Strings**：
  * `accountType`：`Cashflow` / `Saving` / `Invest`（前端須保留 `Unknown` 安全降級）。
  * `transactionType`：`Expense` / `Income` / `Transfer`（前端須保留 `Unknown` 安全降級）。
* **Nullable Policy**：所有陣列欄位永不回傳 `null`（空值一律為 `[]`）；`note` 明確標示為 `string | null`。
* **Error Response**：所有 `4xx` / `5xx` 一律回傳 RFC 7807 `ProblemDetails`，不洩漏內部例外堆疊。