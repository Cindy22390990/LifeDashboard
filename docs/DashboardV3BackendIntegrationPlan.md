# Dashboard V3 Backend Integration Plan (V2.2)

> **Document Authority**：本文件（`docs/DashboardV3BackendIntegrationPlan.md`）為 **Sprint 1 Source of Truth（唯一規格依據）**。
>
> **Spec Sync Requirement**：`DashboardV3BackendSprint1ImplementationSpec.md` 需同步依據本文件 V2.2 進行更新，避免規格衝突。

---

## 1. Purpose

本文件定義 LifeDashboard V3 前端與 .NET Clean Architecture 後端之間的唯讀資料整合規格與架構邊界。

V2.2 版本依據 Copilot Architecture Validation 結果完成 **Domain Alignment**，將所有查詢邏輯與 DTO 欄位嚴格對齊目前專案真實存在的 Entity 結構（`Account`、`Transaction`），不重新設計架構，亦不假設不存在的欄位或業務模型。

核心原則：

* Dashboard 屬於 Cross-Aggregate Read Scenario，由獨立的 Query Layer 提供 Read Model。
* 不修改任何 Domain Entity，不新增 EF Core Migration。
* 不因 Dashboard UI 需求反推或汙染核心 Domain。
* UI 不直接依賴 Backend DTO，前端一律透過 Mapper 將 DTO 轉換為 ViewModel。

---

## 2. Architecture Boundary

### 資料流向（Data Flow）

```text
[Backend]
Domain Entities (Account, Transaction)
        ↓ (EF Core AsNoTracking Projection)
Infrastructure: Dashboard Summary Read Component
        ↓ (returns Application Read Model / Projection Model)
Application: Features/Dashboard/Queries/GetDashboardSummaryQuery (Handler)
        ↓ (maps Projection Model → DTO)
Application: DashboardSummaryDto
        ↓
WebApi: DashboardController (GET Dashboard Summary Endpoint)

════════════════ HTTP / JSON Boundary ════════════════

[Frontend]
Infrastructure: ApiDashboardDataSource
        ↓
Infrastructure: DashboardMapper (DTO → ViewModel，並決定顯示 Title)
        ↓ (implements IDashboardDataSource)
Application: useDashboardData()
        ↓
Presentation: DashboardV3Page → Widgets (ViewModel only)
```

### 分層鐵律

1. **Domain 層**：完全唯讀、零修改。不加入任何 Dashboard 專屬屬性、方法或查詢邏輯。
2. **Application 層**：透過單一職責的讀取介面取得 Projection Model，並由 Query Handler 負責將 Projection Model 轉換為 `DashboardSummaryDto`，不依賴 EF Core。
   * **Projection Model 邊界**：Projection Model 屬於 Application Read Model，不得包含 EF Core Entity 或 `IQueryable` 等 Infrastructure 相依型別。
3. **Infrastructure 層**：負責實作 Dashboard Summary Read Component，在資料庫層完成 `AsNoTracking()` Projection 與加總運算，**不直接回傳 Application DTO**，僅回傳 Projection Model。
4. **WebApi 層**：僅負責路由、授權驗證（從 JWT 解析 `UserId`）與呼叫 Query，不包含任何業務運算。
5. **DTO 與 Frontend 邊界**：Backend DTO 為 API Contract，可由 Application 建立並經 WebApi 回傳；Frontend 僅允許 Infrastructure（DataSource / Mapper）直接依賴 Backend DTO，Presentation 不得直接使用 DTO。

---

## 3. Sprint 1 Scope

### Included（本階段唯一實作範圍）

Sprint 1 **只實作 Dashboard Summary Read Model**，提供單一唯讀 Dashboard Summary API。

* **資料來源**：僅限現有 `Account` 與 `Transaction`。
* **支援項目**：
  * **Total Assets**：所有非刪除（`!IsDeleted`）且非系統帳戶（`!IsSystemAccount`）的 `CurrentBalance` 總和。
  * **Today Expense**：當日業務日期中 `TransactionType == Expense`（且 `!IsDeleted`）的交易金額總和。
  * **Recent Transactions**：最新 N 筆（預設 5 筆）未刪除交易的原始摘要清單。

### Excluded（本階段明確排除）

以下功能因尚未完成對應的 Business Contract 或領域模型，**完全排除於 Sprint 1 之外**，亦不在 DTO 中保留任何 nullable placeholder：

* Budget Radar（含 `RemainingBudget`）
* Goal Progress
* Monthly Calendar
* Day Detail Transactions
* Client 指定查詢日期

---

## 4. DTO Contract

禁止直接回傳 `Account`、`Transaction`、`Goal` Entity，亦不得暴露 `UserId`、`IsDeleted`、`IsSystemAccount`、`CreatedAtUtc`、`UpdatedAtUtc` 等內部欄位。

### DashboardSummaryDto

| 欄位名稱 | 型別 | 說明 |
| :--- | :--- | :--- |
| `TotalAssets` | `decimal` | 所有非刪除（`!IsDeleted`）且非系統帳戶（`!IsSystemAccount`）之 `CurrentBalance` 總和（純數值，不含貨幣符號）。 |
| `TodayExpense` | `decimal` | 當日 `TransactionType == Expense` 之支出總額（純數值，不含貨幣符號）。 |
| `RecentTransactions` | `List<RecentTransactionDto>` | 最新交易摘要清單（預設最多 5 筆）。排序規則：`TransactionDate DESC`，同日期以 `CreatedAtUtc DESC` 作為穩定排序。 |

### RecentTransactionDto

現有 `Transaction` Entity 不包含 `Title` 欄位（僅存在 `Note`、`Category`、`CategoryEntity`）。Backend 僅提供原始資料，**由 Frontend Mapper 決定 UI 顯示的 `Title`**：

| 欄位名稱 | 型別 | 說明 |
| :--- | :--- | :--- |
| `Id` | `Guid` | 交易唯一識別碼。 |
| `Note` | `string?` | 交易備註原始內容（選填）。 |
| `CategoryName` | `string` | 分類名稱（由現有 `Category` / `CategoryEntity` 投影）。 |
| `AccountName` | `string` | 所屬帳戶名稱。 |
| `Amount` | `decimal` | 交易金額（純數值）。 |
| `TransactionDate` | `DateOnly` | 交易業務日期（`YYYY-MM-DD`）。 |

### Domain Alignment Rules

* **TotalAssets 計算規則**：
  * 既有 `Account` Entity 不存在 `IsActive` 欄位，實際存在 `IsDeleted`、`IsSystemAccount`、`CurrentBalance`。
  * 查詢條件固定為：屬於當前使用者、`!IsDeleted` 且 `!IsSystemAccount`，加總 `CurrentBalance`。
* **TodayExpense 計算規則**：
  * 既有 `TransactionType` 列舉已確認包含 `Expense`、`Income`、`Transfer`。
  * 查詢條件固定為：屬於當前使用者、`!IsDeleted`、符合當日 `TransactionDate`，且 `TransactionType == Expense`。
  * 禁止以 `Amount` 正負號推測收支類型。

---

## 5. Query Layer Design

為避免建立過於龐大的共用 Read Repository 演變為 God Repository，Sprint 1 採用單一職責的 Dashboard Summary Read Component，並嚴格區分 Projection Model 與 Application DTO。

### 元件與命名慣例

* **Application**
  * `Features/Dashboard/Dtos/`：`DashboardSummaryDto`、`RecentTransactionDto`
  * `Features/Dashboard/Models/`（或 `Projections/`）：`DashboardSummaryProjection`、`RecentTransactionProjection`
    * **邊界限制**：Projection Model 屬於 Application Read Model，不得包含 EF Core Entity 或 `IQueryable` 等 Infrastructure 相依型別。
  * `Features/Dashboard/Interfaces/`：Dashboard Summary Read Component 介面（回傳 Projection Model）
  * `Features/Dashboard/Queries/`：`GetDashboardSummaryQuery`（含對應 Handler）
* **Infrastructure**
  * **Dashboard Summary Read Component**：名稱可依現有 Repository / Query 命名慣例調整，例如：
    * `DashboardSummaryReader`
    * `DashboardSummaryQueryRepository`
    * 以專案一致性為優先，實際資料夾位置亦需遵循目前 `Infrastructure` 專案既有 Repository / Query 實作慣例，不強制新增新的目錄結構。
* **WebApi**
  * `DashboardController`：遵循既有專案 `Controllers/` 目錄與 Routing Pattern，不為 Dashboard 單獨建立特殊目錄。

### 責任邊界

* **Dashboard Summary Read Component（Infrastructure）**：專責 Dashboard Summary 的 EF Core Projection 與聚合查詢，強制套用 `AsNoTracking()`、使用者隔離與軟刪除過濾，回傳 Projection Model（不直接回傳 Application DTO）。
* **`GetDashboardSummaryQuery` / Handler（Application）**：僅接收 `UserId`（不接受客戶端日期參數），呼叫 Read Component 介面取得 Projection Model 後，負責映射並組裝為 `DashboardSummaryDto`。

### Transaction Date Semantics

* **`TransactionDate` (`DateOnly`)**：代表交易的**業務日期**，為 Dashboard 所有日期篩選與主要排序（`TransactionDate DESC`）的唯一依據。
* **`CreatedAtUtc`**：僅代表資料寫入資料庫的系統建立時間，**不代表交易發生時間**，嚴禁用於交易日期篩選；在 Sprint 1 中僅允許作為 `RecentTransactions` 同日期的次要穩定排序依據（`CreatedAtUtc DESC`）。
  * 依目前 Entity 定義使用 `CreatedAtUtc`，不得修改其型別。
* **Sprint 1 邊界**：不處理 Client 指定日期、不處理 Timezone conversion、不使用 `DateTimeOffset`、不進行 UTC 時間區間換算查詢。

---

## 6. API Contract

### Dashboard Summary Endpoint

* **Method**：`GET`
* **認證方式**：必須通過 JWT Bearer 驗證，`UserId` 一律由 Token Claims 解析，不接受由 Query String 傳入。
* **Query Parameters**：無（Sprint 1 不處理 client 指定日期）。
* **Success Response**：
  * `200 OK`
  * Body：`DashboardSummaryDto`（JSON camelCase）
* **Error Response**（遵循 RFC 7807 `ProblemDetails`）：
  * `401 Unauthorized`：未提供有效驗證資訊。
  * `500 Internal Server Error`：非預期系統錯誤（不洩漏內部堆疊資訊）。

### API Versioning & Migration Decision

* **API Versioning 規則**：
  * 實作前先檢查現有 Controller Routing Pattern。
  * **不得自行新增 API Versioning Framework**。
  * 若專案未使用 Versioning，則沿用既有 Controller Routing。
  * 若已存在 Versioning，則遵循既有版本策略。
* **既有端點共存原則**：
  1. 實作前先確認 V3 Frontend 是否已使用舊版 `api/dashboard/summary`。
  2. **禁止刪除或破壞既有 `api/dashboard/summary`**，以確保既有功能與向後相容性不受影響。

---

## 7. Deferred Features

以下功能與擴充點延後至後續 Sprint，待對應領域與需求確認後再行擴充：

* **Monthly Calendar & Day Detail（Future Extension）**：
  * 目前已確認 `TransactionDate` 可作為 Business Date，且 `TransactionType` 包含 `Expense`、`Income`、`Transfer`。
  * 實作前需先確認：UI 顯示需求（週曆/月曆切換與標記規則）與日期聚合規則。
* **Budget Radar**：待正式定義 Budget Domain 或確認與既有 Cash Flow（`BudgetRadarService`）的產品邊界後，再另建獨立 Query Contract。
* **Goal Progress**：待 `GoalFundingAllocation` 與 `GoalFundingSummary` Read Model 實作完成後再行接入。
* **Multi-Currency**：待核心領域完成多幣別設計後再行擴充。
* **API Integration Tests**：延後至後續階段補齊（Sprint 1 僅實作 Query Handler Unit Tests）。

---

## 8. Open Questions

以下問題在進入後續 Sprint 前仍需與產品確認，**暫不升級為正式 ADR**：

1. **Budget Radar 的產品定位**：未來是要呈現 Cash Flow（收入減固定承諾）還是 Budget（月預算上限減支出）？
2. **Calendar 聚合規格**：日曆格子需要顯示哪些交易類型、是否需要顯示筆數或僅顯示總額？

---

## 9. Copilot Implementation Instruction

請以本文件（`docs/DashboardV3BackendIntegrationPlan.md` V2.2）作為 Sprint 1 的唯一規格依據執行實作。

### 嚴格限制（違反即失敗）

* **第一階段只能建立 Dashboard Summary Query**。
* **Domain 對齊**：
  * `TotalAssets` 查詢條件為 `!IsDeleted && !IsSystemAccount`，加總 `CurrentBalance`（不可假設 `IsActive`）。
  * `TodayExpense` 查詢條件為 `!IsDeleted && TransactionType == Expense`（不可用金額正負號推測）。
  * `RecentTransactionDto` 僅回傳 `Id`、`Note`、`CategoryName`、`AccountName`、`Amount`、`TransactionDate`（不可假設 `Transaction.Title`）。
* **分層與 Projection 限制**：
  * Dashboard Summary Read Component 只能回傳 Projection Model，**禁止**直接回傳 `DashboardSummaryDto`；DTO 轉換必須由 Application Query Handler 負責。
  * Projection Model 屬於 Application Read Model，不得包含 EF Core Entity 或 `IQueryable` 等 Infrastructure 相依型別。
  * Backend DTO 為 API Contract，可由 Application 建立並經 WebApi 回傳；Frontend 僅允許 Infrastructure（DataSource / Mapper）直接依賴 Backend DTO，Presentation 不得直接使用 DTO。
* **命名與目錄慣例**：
  * 禁止因命名不同而重構整個專案；以現有專案命名與目錄慣例為優先，只要責任一致即可。
  * 不得自行新增 API Versioning Framework；沿用既有 Controller Routing Pattern 或既有版本策略。
* **API 共存與參數限制**：不接受 `date` query parameter，**禁止刪除或破壞既有 `api/dashboard/summary`**。
* **禁止實作**：Calendar Query、Budget Query、Goal Query、API Integration Tests。
* **禁止新增或修改**：任何 Domain Entity（含 `CreatedAtUtc` 型別）、EF Core Migration、任何 Frontend 程式碼。
* **日期與排序規則**：日期篩選只使用 `TransactionDate` (`DateOnly`)；`RecentTransactions` 排序為 `TransactionDate DESC` 搭配同日期 `CreatedAtUtc DESC` 穩定排序（依目前 Entity 定義使用 `CreatedAtUtc`，不得修改其型別）。禁止實作時區換算。

### 執行順序（每完成一步皆須執行 Build + Test 確認通過）

0. **Step 0：規格與現況核對（實作前必做）**
   * 閱讀 `DashboardV3BackendIntegrationPlan.md` (V2.2) 與 `DashboardV3BackendSprint1ImplementationSpec.md`。
   * 若 `DashboardV3BackendSprint1ImplementationSpec.md` 尚未同步，**先以 `DashboardV3BackendIntegrationPlan.md` (V2.2) 為唯一規格依據**，並列出需要同步之差異，**不得因此停止 Sprint 1 實作**。
   * 檢查現有專案的 `Entity`、`DbContext`、`Repository`（命名與目錄慣例）以及 `Controller`（Routing Pattern 與既有版本策略）。
   * **禁止因命名不同而重構整個專案**：若規格中的 Reader、Repository、Component 名稱與現有命名不同，以現有專案命名慣例為優先，只要責任一致即可。
   * 僅當**現有程式碼（Entity / Schema）**與本規格 V2.2 存在實質衝突（例如缺少必要欄位）時，才停止修改並列出差異。
1. **Step 1：建立 DTO 與 Projection Model**
   * 建立 `DashboardSummaryDto` 與 `RecentTransactionDto`（僅含 `Id`、`Note`、`CategoryName`、`AccountName`、`Amount`、`TransactionDate`）。
   * 建立對應的 Application Projection Model（不得含 EF Core / `IQueryable` 相依型別）。
   * 執行 `Build + Test`。
2. **Step 2：建立 Query Interface**
   * 依專案命名慣例建立 Dashboard Summary Read Component 介面（回傳 Projection Model）。
   * 執行 `Build + Test`。
3. **Step 3：建立 Query Handler**
   * 建立 `GetDashboardSummaryQuery` 與對應 Handler（負責呼叫 Read Component 介面並將 Projection Model 轉換為 `DashboardSummaryDto`）。
   * 執行 `Build + Test`。
4. **Step 4：建立 Infrastructure Projection**
   * 遵循既有 `Infrastructure` 專案慣例實作 Dashboard Summary Read Component，使用 `AsNoTracking()` 與 EF Projection 實作查詢（`TotalAssets` 過濾 `!IsDeleted && !IsSystemAccount` 加總 `CurrentBalance`；`TodayExpense` 過濾 `TransactionType == Expense`；`RecentTransactions` 以 `TransactionDate DESC, CreatedAtUtc DESC` 穩定排序），回傳 Projection Model，並完成 DI 註冊。
   * 執行 `Build + Test`。
5. **Step 5：建立 API Endpoint**
   * 依專案既有 Controller Routing Pattern（不新增 API Versioning Framework）實作唯讀 Dashboard Summary Endpoint（無 Query Parameter，保留既有舊端點不刪除）。
   * 執行 `Build + Test`。
6. **Step 6：建立 Tests**
   * **Sprint 1**：補齊 `GetDashboardSummaryQuery` Handler 的 **Unit Tests**（驗證空狀態、Projection 映射至 DTO 正確性等）。
   * **Future（本階段不實作）**：API Integration Tests。
   * 執行 `Build + Test`。