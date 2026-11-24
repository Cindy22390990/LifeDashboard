# Dashboard V3 API-only Migration Plan

> **Document Path**: `docs/DashboardV3ApiOnlyMigrationPlan.md`
>
> **Status**: Active (Sprint Execution Source of Truth)

---

## 1. Document Overview

### Purpose

本文件定義 LifeDashboard V3 從 Hybrid Data Source（Mock + API 混合模式）遷移至 **API-only 架構**的完整技術計畫、契約補齊清單與執行規範，作為後續遷移 Sprint 的唯一規格依據（Source of Truth）。

### Scope

* 補齊 Backend Dashboard Summary API 阻礙 API-only 切換的必要欄位。
* 清除 `dashboardV3ApiMapper` 中所有對 `mockDashboardData` 的依賴。
* 確立未實作領域（Budget、Goal）在前端的 Empty / Pending State 呈現規範。
* 透過 `AuthApp` 完成 `ApiDashboardDataSource` 正式切換。
* 規劃並接入獨立的 Calendar API，完成核心互動區塊的 API-only 遷移。

### Non-goals

為控制風險並聚焦於移除 Mock，以下項目明確排除於本遷移計畫之外：

* 不進行前端 Feature Slice 架構重構。
* 不導入新的 Server State 管理工具（如 TanStack Query、SWR）。
* 不設計 Widget Plugin 系統。
* 不實作 Offline Mode 或離線同步機制。
* 不修改既有的 Clean Architecture 分層與 UI 視覺設計。
* 不提前建立不存在的 Backend Domain（如 Budget Entity、Goal Funding Allocation）。

### Current Architecture

Frontend 目前已完成 DataSource 抽象邊界，但 `AuthApp` 尚未正式切換至 `ApiDashboardDataSource`，且 `dashboardV3ApiMapper` 仍直接依賴 `mockDashboardData` 補齊後端未提供的欄位。

```text
AuthApp
  ↓ (注入 MockDashboardDataSource)
DashboardV3Page
  ↓
useDashboardData
  ↓
IDashboardDataSource
  ↓
ApiDashboardDataSource / MockDashboardDataSource
  ↓
dashboardV3ApiMapper  ←── (仍依賴 mockDashboardData 作為 fallback)
  ↓
DashboardViewModel
  ↓
Dashboard Widgets
```

### Target Architecture

正式環境完全切斷對 `mockDashboardData` 的依賴，`ApiDashboardDataSource` 成為唯一正式資料來源。所有已顯示之數據皆源自 Backend Contract，未完成領域則由 Mapper 明確映射為 Empty / Pending State。

---

## 2. Migration Principle

1. **API-only 優先**：正式環境（Production）中，Dashboard V3 所有業務數據皆須來自後端 API，徹底移除生產程式碼對 Mock Fixture 的引用。
2. **不使用 Mock 補正式資料**：嚴禁以「真實總資產 + 假預算 / 假目標」的混合數據呈現給使用者，避免破壞財務系統的資料誠信。
3. **Backend Contract 優先**：若前端 Widget 所需資料在資料庫已存在但 API 缺少欄位（如 `TransactionType`、`AccountSummary`），必須優先補齊 Backend Contract，而非在前端進行數值猜測。
4. **ViewModel 與 DTO 分離**：維持 `Backend DTO` 僅限於 Infrastructure 層（DataSource / Mapper）使用；`DashboardViewModel` 專責服務 UI，兩者透過純函數 Mapper 隔離。
5. **Missing Data 使用 Empty / Pending State**：對於尚未建立後端領域模型的區塊（Budget Radar、Goal Progress），前端必須以明確的 Empty 或 Pending State 呈現，待未來領域完成後再行接入。

---

## 3. Current Data Flow

### Current: Mock + API Hybrid Flow

```text
[Backend API]
GET /api/dashboard/v3/summary (僅回傳部分 Summary)
        │
        ▼
ApiDashboardDataSource
        │
        ▼
dashboardV3ApiMapper ◄──── [mockDashboardData] (強耦合 Fallback)
        │
        ▼
DashboardViewModel (混合真實資料與假資料)
        │
        ▼
Dashboard Widgets
```

### Target: API-only Flow

```text
[Backend APIs]
  ├── GET /api/dashboard/v3/summary   (HeroStats, Accounts, RecentTransactions, BusinessDate)
  └── GET /api/dashboard/v3/calendar  (Monthly Summary & Day Detail)
        │
        ▼
ApiDashboardDataSource (唯一正式 DataSource，由 AuthApp 注入)
        │
        ▼
dashboardV3ApiMapper (Pure Function，零 Mock 依賴)
  ├── 已支援欄位 ──▶ 映射真實資料
  └── 未實作領域 ──▶ 映射為 Empty / Pending State
        │
        ▼
DashboardViewModel (100% 真實資料或明確 Pending 狀態)
        │
        ▼
Dashboard Widgets (具備 Empty / Pending 渲染防呆)
```

---

## 4. Dashboard Data Coverage Matrix

| Feature | ViewModel Data | Current Source | Backend Status | Migration Action |
| :--- | :--- | :--- | :--- | :--- |
| **Hero Stats** | `totalAssets`, `todayExpense` | Summary API | ✅ 已支援 | 維持由 Backend 聚合提供。 |
| **Hero Stats** | `remainingBudget` | Mock Fallback | ❌ 未支援 | 改為 `Pending / null`，待 Budget 領域定義完成後接入。 |
| **Hero Stats** | `assetChange`, `savingRate` | Mock Fallback | ❌ 未支援 | 從 `DashboardViewModel` 移除（屬於 Reports 範疇）。 |
| **Account Summary** | `accountList` (`id`, `name`, `balance`, `type`) | Mock Fallback | ❌ 缺口 | **Critical**：由 Backend Summary API 補齊。 |
| **Account Summary** | `totalBalance` | Summary API | ✅ 已支援 | 直接複用 `totalAssets`，不由前端重複加總。 |
| **Recent Transactions** | `id`, `note`, `categoryName`, `accountName`, `amount`, `transactionDate` | Summary API | ✅ 已支援 | 維持由 Backend 提供；顯示用 `title` 由 Mapper 決定。 |
| **Recent Transactions** | `transactionType` | Mock / 猜測 | ❌ 缺口 | **Critical**：由 Backend Summary API 補齊，禁止猜測正負號。 |
| **Budget Radar** | `budgetLimit`, `spentAmount`, `remainingAmount` | Mock Fallback | ❌ 未支援 | 刪除 Mock，改為 Empty / Pending State，待未來領域完成。 |
| **Goal Progress** | `goalAmount`, `currentAmount`, `deadline` | Mock Fallback | ❌ 未支援 | 刪除 Mock，改為 Empty / Pending State，待 Read Model 完成。 |
| **Goal Progress** | `contributionHistory` | Mock Fallback | ❌ 未支援 | 從 `DashboardViewModel` 移除（屬於 Goal Detail 範疇）。 |
| **Calendar** | `transactionDates`, `dailySpendingSummary`, `dayDetail` | Mock Fallback | ❌ 未支援 | **Important**：新增獨立 Calendar API 並於 Sprint 4 接入。 |

---

## 5. Mock Dependency Removal Plan

| Mock Dependency | Location | Action | Dependency |
| :--- | :--- | :--- | :--- |
| `assetChange`, `savingRate`, `contributionHistory` | `dashboardV3ApiMapper` / `DashboardViewModel` | **Remove Immediately** | 無（非 Dashboard V3 核心欄位，直接移除）。 |
| `mockBudgetRadar`（含 `remainingBudget`） | `dashboardV3ApiMapper` | **Replace with Empty State** | Widget 需支援 Pending / Empty State 渲染。 |
| `mockGoalProgress` | `dashboardV3ApiMapper` | **Replace with Empty State** | Widget 需支援 Pending / Empty State 渲染。 |
| `mockRecentTransactions`（收支類型補齊） | `dashboardV3ApiMapper` | **Waiting Backend Contract** | 需等待 Backend `RecentTransactionDto` 補齊 `TransactionType`。 |
| `mockAccountSummary` | `dashboardV3ApiMapper` | **Waiting Backend Contract** | 需等待 Backend `DashboardSummaryDto` 補齊 `Accounts` 清單。 |
| `mockCalendarData` | `dashboardV3ApiMapper` | **Waiting Backend Contract** | 需等待 Backend 實作獨立 Calendar API（Sprint 4）。 |

---

## 6. Backend Contract Requirements

### Critical（阻止 API-only 切換）

1. **`RecentTransactionDto.TransactionType`**：
   * 明確回傳現有 `Transaction` Entity 的 `TransactionType`（`Expense`、`Income`、`Transfer`）。
   * 解決前端無法區分收支方向與金額符號的問題。
2. **`DashboardSummaryDto.Accounts`（Account Summary）**：
   * 擴充現有 Summary API，回傳活躍帳戶清單（`Id`、`Name`、`AccountType`、`Balance`）。
   * 過濾條件與 `TotalAssets` 一致：`UserId == currentUserId && !IsDeleted && !IsSystemAccount`。
3. **`DashboardSummaryDto.BusinessDate`（ServerDate）**：
   * 回傳後端計算 `TodayExpense` 所依據的基準日期（`DateOnly`）。
   * 確保前端 Hero Stats 與日期顯示與後端計算口徑一致。

### Important（完整 Dashboard V3 體驗）

4. **Calendar API**：
   * `GET /api/dashboard/v3/calendar`：依 `year`、`month` 回傳該月有交易日期的每日收支總額與轉帳標記（依據 `TransactionDate` 聚合）。
   * `GET /api/dashboard/v3/calendar/day`：依 `date` 回傳單日交易明細清單，供 Day Detail Panel / Bottom Sheet 按需載入。

### Future（延後至對應領域完成後接入）

5. **Goal Read Model**：待 `GoalFundingAllocation` 與 `GoalFundingSummary` 完成後提供，嚴禁以 `Goal.CurrentAmount` 替代。
6. **Budget Read Model**：待釐清 Budget Management 與現有 Cash Flow（固定責任）的產品邊界後建立。

---

## 7. API Strategy

本遷移計畫採用 **Summary API + Feature APIs（Hybrid Strategy）**，不採用「全部塞進單一 Summary API」，亦不採用「每個 Widget 拆成獨立 API」。

### 決策理由

* **首頁載入效率**：將首屏靜態且高頻的摘要資料（`TotalAssets`、`TodayExpense`、`Accounts`、`RecentTransactions`、`BusinessDate`）集中於 `GET /api/dashboard/v3/summary`，確保首屏僅需單次請求即可渲染核心數據，避免行動網路下的多請求連線開銷。
* **Cache 邊界清晰**：月曆翻頁與點擊單日明細屬於按需互動查詢（Lazy Load），拆分為獨立的 Calendar API 可避免切換月份時重複查詢總資產與帳戶餘額。
* **維護成本可控**：維持 Summary API 聚焦於 `Account` 與 `Transaction` 的核心投影，避免單一 DTO 與 Reader 無限膨脹。
* **未來擴充彈性**：未來的 `Goal Progress` 與 `Budget Radar` 可依領域成熟度各自以獨立 Feature API 接入，不影響既有 Summary Contract 的穩定性。

---

## 8. Migration Sprint Plan

本計畫嚴格收斂於「Hybrid → API-only」遷移，**不引入 Feature Slice、TanStack Query、Widget Plugin 或 Offline 機制**。

### Sprint 1：Summary Contract 完善（Backend）

* 擴充 `RecentTransactionProjection` 與 `RecentTransactionDto`，補上 `TransactionType`。
* 擴充 `DashboardSummaryProjection` 與 `DashboardSummaryDto`，補上 `Accounts` 清單與 `BusinessDate`。
* 更新 `DashboardSummaryReader` 查詢邏輯與 `GetDashboardSummaryQueryHandler` Unit Tests。
* 驗證 `GET /api/dashboard/v3/summary` 回應格式與既有端點零破壞。

### Sprint 2：Frontend Mapper Clean-up（Frontend）

* 清理 `DashboardViewModel`，移除非 Dashboard V3 必要欄位（`assetChange`、`savingRate`、`contributionHistory`）。
* 重構 `dashboardV3ApiMapper`，將 Sprint 1 補齊的 `TransactionType`、`Accounts`、`BusinessDate` 完整映射至 ViewModel。
* 將 `BudgetRadar`、`GoalProgress` 與尚未接入 API 的 `Calendar` 映射邏輯改為明確的 Empty / Pending State。

### Sprint 3：Remove Mock Dependency（Frontend）

* 徹底移除 `dashboardV3ApiMapper` 中對 `mockDashboardData` 的所有 import 與 fallback 邏輯。
* 驗證所有 Dashboard Widgets 在 Empty / Pending State 下正常渲染，不發生 Runtime Error。
* 透過 `AuthApp` 將正式環境 `dataSource` 切換至 `ApiDashboardDataSource`。
* 確認 `MockDashboardDataSource` 僅保留於開發或測試用途，不再進入正式資料流。

### Sprint 4：Calendar API Integration（Backend + Frontend）

* Backend 建立獨立的 Calendar Query 與 Reader，實作 `GET /api/dashboard/v3/calendar` 與 `GET /api/dashboard/v3/calendar/day`。
* Frontend 於 `ApiDashboardDataSource` 接入 Calendar API，並更新 `dashboardV3ApiMapper` 完成月曆與單日明細映射。
* 完成 Dashboard V3 所有核心已實作區塊的 API-only 遷移。

---

## 9. Copilot Execution Rules

GitHub Copilot 在執行本遷移計畫的各 Sprint 任務時，必須嚴格遵守以下邊界：

### Allowed（允許執行）

* 修改指定的 Backend DTO、Projection Model 與 `DashboardSummaryReader` 查詢。
* 新增 Sprint 4 所需的 Calendar Query、Reader 與 Controller Endpoint。
* 修改 `ApiDashboardDataSource` 與 `dashboardV3ApiMapper`。
* 修正 `DashboardViewModel` 欄位清理與映射邏輯。
* 修正 Dashboard Widgets 的 Empty / Pending State 顯示與防呆。
* 補齊與更新對應的 Unit Tests。

### Forbidden（嚴格禁止）

* 禁止進行大規模架構重構（如 Feature Slice 拆分）。
* 禁止改變既有 Clean Architecture 分層與依賴方向。
* 禁止導入新的 State Management 或 Cache Library（如 TanStack Query、SWR、新 Zustand Store）。
* 禁止修改既有 UI 視覺設計、排版結構與 Design Tokens。
* 禁止建立 Widget Plugin System 或 Offline 同步機制。
* 禁止修改任何 Domain Entity 或新增 EF Core Migration。

---

## 10. Acceptance Criteria

滿足以下所有條件時，視為 Dashboard V3 API-only 遷移計畫完成：

* [ ] **零 Mock 依賴**：`dashboardV3ApiMapper` 與正式環境程式碼完全不 import `mockDashboardData`。
* [ ] **唯一正式來源**：`AuthApp` 已正式切換並注入 `ApiDashboardDataSource` 作為 Dashboard V3 唯一資料來源。
* [ ] **Contract 完整支援**：Backend Summary API 完整提供 `TotalAssets`、`TodayExpense`、`BusinessDate`、`Accounts` 與含 `TransactionType` 的 `RecentTransactions`；Calendar API 支援月聚合與單日明細。
* [ ] **狀態防呆完整**：`BudgetRadar` 與 `GoalProgress` 在無後端資料時正確呈現 Empty / Pending State，無任何假數據外洩，且不發生 Runtime Error。
* [ ] **建置與測試通過**：Backend 與 Frontend 皆通過完整 Build，所有 Unit Tests 維持全數綠燈通過。