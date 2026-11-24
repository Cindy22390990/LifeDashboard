# Dashboard V3 Backend Contract Specification

Version: 1.0  
Status: Draft  
Source of Truth: YES

---

## 1. Purpose

本文件定義 LifeDashboard Dashboard V3 後端唯讀 API 的正式資料契約（Backend Contract），作為前後端整合、DTO 實作與契約驗證的唯一規格依據（Source of Truth）。

本規格旨在：

* 確立 Dashboard V3 各區塊之 API 邊界、請求參數與回應結構。
* 明確劃分後端帳務聚合計算與前端展示格式化之責任。
* 確保所有契約嚴格對齊既有 Domain Entity（`Account`、`Transaction`），不提前發明不存在之領域模型。

---

## 2. Architecture Boundary

* **Cross-Aggregate Read Scenario**：Dashboard 屬於跨聚合唯讀查詢，由獨立的 Query Layer 提供 Read Model，不直接暴露 Domain Entity。
* **Domain 零修改原則**：不因 Dashboard UI 需求修改任何既有 Domain Entity，亦不新增 EF Core Migration。
* **分層資料流**：
  * Infrastructure 層透過 `AsNoTracking()` Projection 回傳 Application Read Model（Projection Model），不得直接回傳 Application DTO。
  * Application Query Handler 負責將 Projection Model 映射為本文件定義之 DTO。
  * WebApi Controller 僅負責路由、JWT 授權驗證（從 Claims 解析 `UserId`）與呼叫 Query Handler，不包含任何業務計算。
* **認證與隔離**：所有端點皆須通過 JWT Bearer 驗證，強制套用 `UserId` 資料隔離與 `!IsDeleted` 軟刪除過濾。

---

## 3. API Contract

### 3.1 `GET /api/dashboard/v3/summary`

* **Purpose**：提供 Dashboard V3 首屏核心摘要資料，包含總資產、當日支出、業務基準日、活躍帳戶清單與最新交易紀錄。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：無（不接受 `date` 或 `currentDate` 參數）。
* **Response**：
  * `200 OK`：`DashboardSummaryDto`
  * `401 Unauthorized`：未授權存取。
  * `500 Internal Server Error`：RFC 7807 `ProblemDetails`。
* **Status**：Implemented（Sprint 1）
* **Responsibility**：聚合活躍帳戶餘額總和、計算 `businessDate` 當日支出總額、投影活躍帳戶清單與最新 5 筆交易紀錄。

---

### 3.2 `GET /api/dashboard/v3/calendar`

* **Purpose**：提供指定年月的月曆每日收支聚合摘要，供月曆格子顯示活動標記。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：
    * `year`（必填，`int`，例如 `2026`）
    * `month`（必填，`int`，`1`–`12`）
* **Response**：
  * `200 OK`：`CalendarDto`
  * `400 Bad Request`：`year` 或 `month` 參數不合法。
  * `401 Unauthorized`：未授權存取。
  * `500 Internal Server Error`：RFC 7807 `ProblemDetails`。
* **Status**：Defined（待實作／驗證）
* **Responsibility**：依 `TransactionDate` 聚合該月份有交易紀錄之日期的每日收入總額、支出總額與轉帳標記。

---

### 3.3 `GET /api/dashboard/v3/calendar/day`

* **Purpose**：提供指定單一業務日期的收支總計與完整交易明細清單。
* **Request**：
  * Header：`Authorization: Bearer <JWT>`
  * Query Parameters：
    * `date`（必填，`string`，格式 `YYYY-MM-DD`）
* **Response**：
  * `200 OK`：`CalendarDayDto`
  * `400 Bad Request`：`date` 格式不合法。
  * `401 Unauthorized`：未授權存取。
  * `500 Internal Server Error`：RFC 7807 `ProblemDetails`。
* **Status**：Defined（待實作／驗證）
* **Responsibility**：計算指定日期的總收入與總支出（排除轉帳），並回傳該日所有未刪除交易明細。

---

### 3.4 Planned API：Budget

* **Purpose**：提供 Dashboard V3 Budget Radar 與 Hero Budget Status 所需之預算上限、已支出與剩餘額度資料。
* **Request**：TBD（待確立 Budget Domain 與 Cash Flow 業務邊界後定義）。
* **Response**：TBD
* **Status**：Planned（TBD）
* **Responsibility**：由後端依據正式 Budget 領域規則計算當期預算額度與已使用金額。

---

### 3.5 Planned API：Fixed Commitment

* **Purpose**：提供 Dashboard V3 Fixed Commitment Widget 所需之固定支出清單、當月應付總額、到期日與繳費狀態。
* **Request**：TBD（待確立 Fixed Commitment Read Model 後定義）。
* **Response**：TBD
* **Status**：Planned（TBD）
* **Responsibility**：由後端聚合當期固定承諾總額、已付與待付狀態。

---

### 3.6 Planned API：Goal

* **Purpose**：提供 Dashboard V3 Goal Pyramid 與 Hero Allocation Summary 所需之目標清單、目標金額與已配置資金進度。
* **Request**：TBD（待 `GoalFundingSummary` Read Model 完成後定義）。
* **Response**：TBD
* **Status**：Planned（TBD）
* **Responsibility**：由後端透過獨立 Funding Read Model 聚合各目標實際配置金額（嚴禁直接使用未驗證欄位替代）。

---

## 4. DTO Specification

### 4.1 `DashboardSummaryDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `totalAssets` | `decimal` | No | 所有非刪除且非系統帳戶之 `CurrentBalance` 總和。 |
| `todayExpense` | `decimal` | No | `businessDate` 當日 `TransactionType == Expense` 之支出總額。 |
| `businessDate` | `DateOnly` (`YYYY-MM-DD`) | No | 後端計算 `todayExpense` 所依據的業務基準日期。 |
| `accounts` | `AccountSummaryDto[]` | No | 使用者活躍帳戶清單；無資料時回傳空陣列 `[]`。 |
| `recentTransactions` | `RecentTransactionDto[]` | No | 最新 5 筆交易紀錄，依 `TransactionDate DESC, CreatedAtUtc DESC` 排序；無資料時回傳 `[]`。 |

### 4.2 `AccountSummaryDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 帳戶唯一識別碼。 |
| `name` | `string` | No | 帳戶名稱（使用者自訂原始字串）。 |
| `accountType` | `Enum`（序列化格式 TBD） | No | 帳戶類型列舉。 |
| `balance` | `decimal` | No | 帳戶目前餘額（純數值）。 |

### 4.3 `RecentTransactionDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | No | 交易唯一識別碼。 |
| `note` | `string` | Yes | 交易備註原始內容（不包含 `title` 欄位）。 |
| `categoryName` | `string` | No | 分類名稱。 |
| `accountName` | `string` | No | 所屬帳戶名稱。 |
| `amount` | `decimal` | No | 交易金額（純數值）。 |
| `transactionType` | `Enum`（`Expense` / `Income` / `Transfer`） | No | 交易類型列舉（須確認已納入實際回傳 Schema）。 |
| `transactionDate` | `DateOnly` (`YYYY-MM-DD`) | No | 交易業務日期。 |

### 4.4 `CalendarDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `year` | `int` | No | 查詢年份。 |
| `month` | `int` | No | 查詢月份（`1`–`12`）。 |
| `days` | `CalendarDaySummaryItemDto[]` | No | 該月有交易紀錄之日期摘要清單（無交易日期不回傳；整月無資料回傳 `[]`）。 |

*(註：`CalendarDaySummaryItemDto` 包含 `date` (`DateOnly`, Non-null)、`totalIncome` (`decimal`, Non-null)、`totalExpense` (`decimal`, Non-null)、`hasTransfer` (`bool`, Non-null)。)*

### 4.5 `CalendarDayDto`

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `date` | `DateOnly` (`YYYY-MM-DD`) | No | 查詢的單一業務日期。 |
| `totalIncome` | `decimal` | No | 該日 `TransactionType == Income` 且 `!IsDeleted` 之總和。 |
| `totalExpense` | `decimal` | No | 該日 `TransactionType == Expense` 且 `!IsDeleted` 之總和。 |
| `transactions` | `RecentTransactionDto[]` | No | 該日所有未刪除交易明細，依 `CreatedAtUtc DESC` 穩定排序；無資料回傳 `[]`。 |

---

## 5. Backend Calculation Responsibility

所有涉及帳務口徑、跨筆聚合與領域過濾的計算皆由 Backend 專責處理：

* **Total Assets**：過濾 `UserId == currentUserId && !IsDeleted && !IsSystemAccount`，於資料庫層加總 `CurrentBalance`。
* **Today's Expense**：過濾 `UserId == currentUserId && !IsDeleted && TransactionType == Expense && TransactionDate == businessDate`，於資料庫層加總金額。禁止以金額正負號推測收支。
* **Calendar Aggregation**：依 `TransactionDate` 分組計算每日 `totalIncome`（僅 `Income`）與 `totalExpense`（僅 `Expense`），嚴格排除 `Transfer` 不計入收支總額，但保留 `hasTransfer` 標記與明細清單。
* **Budget Calculation**：待 Budget Domain 確立後，由後端計算當期預算上限、已支出金額與剩餘額度（TBD）。
* **Goal Progress**：待 `GoalFundingSummary` Read Model 確立後，由後端計算各目標已配置資金總額（TBD）。

---

## 6. Frontend Responsibility

Frontend **嚴禁重新計算任何帳務結果**（例如不可自行加總 `accounts[]` 取代 `totalAssets`，亦不可自行加總 `transactions[]` 取代 `totalIncome` / `totalExpense`）。

Frontend 僅負責：

* **DTO → ViewModel 轉換**：透過純函數 Mapper 隔離後端 DTO，並由 Mapper 決定交易顯示標題（例如 `note` 為空時退回 `categoryName`）。
* **展示格式化**：使用 `Intl.NumberFormat` 處理單一基準幣別之千分位與符號，以及安全解析 `DateOnly` 字串（禁用 `new Date("YYYY-MM-DD")`）。
* **隱私遮罩（Privacy Display）**：依使用者 UI 偏好將敏感金額遮罩顯示。
* **UI 狀態管理**：處理 Loading Skeleton、Empty State、Pending State、Error State、Retry 與 `AbortController` 請求取消。
* **純視覺衍生比例**：僅允許計算純展示用之百分比（如進度條寬度比例）。

---

## 7. Contract Rules

* **Money format**：所有金額一律以未格式化之純數值 `decimal`（JSON `number`）傳輸，不含貨幣符號或千分位逗號。多幣別規則為 TBD。
* **DateOnly**：所有業務日期一律採用 `DateOnly` 並序列化為 ISO 8601 純日期字串 `YYYY-MM-DD`。`CreatedAtUtc` 僅供後端同日穩定排序使用，**不得**作為交易日期篩選依據，亦**不得**暴露於 DTO。
* **Enum**：後端 Enum 序列化格式（字串或整數）須與 Swagger Schema 保持一致（細節 TBD）；前端 Mapper 必須具備 `Unknown` 安全降級處理。
* **Nullable**：
  * 集合型別（陣列）一律不得為 `null`，無資料時固定回傳空陣列 `[]`。
  * 明確允許為 `null` 之欄位（如 `RecentTransactionDto.note`）須於 DTO 標示 `Nullable: Yes`。
  * 禁止為尚未實作的領域加入 nullable placeholder 欄位。
* **Error Response**：所有 `4xx` / `5xx` 錯誤回應一律遵循 RFC 7807 `ProblemDetails` 格式，不洩漏內部例外堆疊。
* **Pagination**：
  * `GET /api/dashboard/v3/summary` 之 `recentTransactions` 固定回傳最新最多 5 筆，不提供分頁參數。
  * `GET /api/dashboard/v3/calendar/day` 回傳單日完整交易清單，不提供分頁參數。
  * 其他 Planned API 分頁規則為 TBD。

---

## 8. API Evolution Rules

* **Additive-only 原則**：既定端點之演進以「向後相容的新增欄位」為原則，不得任意變更既有欄位名稱、型別或語意。
* **既有端點共存**：既有 `GET /api/dashboard/summary` 須維持原狀，不得因 V3 開發而刪除或破壞。
* **功能拆分原則（Summary + Feature APIs）**：`summary` 端點僅承載首屏輕量核心摘要；月曆、預算、固定承諾與目標等獨立領域或互動查詢，一律以獨立 Feature API 擴充，不得無限制塞入 `summary`。
* **破壞性變更**：若未來發生破壞性契約變更（如導入 Multi-Currency 改變金額結構），必須建立新版本端點並與舊版並存，遵循專案既有 Routing Pattern。

---

## 9. Dashboard Widget Mapping

| Widget | API |
| :--- | :--- |
| **Hero Section**（Total Assets, Today's Expense, Business Date） | `GET /api/dashboard/v3/summary` |
| **Hero Section**（Budget Status, Allocation Summary） | Planned API：Budget / Goal（TBD） |
| **Account Summary** | `GET /api/dashboard/v3/summary` |
| **Transaction Summary（Recent Transactions）** | `GET /api/dashboard/v3/summary` |
| **Calendar（Monthly Overview）** | `GET /api/dashboard/v3/calendar` |
| **Calendar（Day Detail）** | `GET /api/dashboard/v3/calendar/day` |
| **Budget Radar** | Planned API：Budget（TBD） |
| **Fixed Commitment** | Planned API：Fixed Commitment（TBD） |
| **Goal Pyramid** | Planned API：Goal（TBD） |