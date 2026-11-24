# Dashboard V3 Contract Alignment Specification

> **Status**: Draft for Freeze (Backend DTO → Frontend DTO → ViewModel)
>
> **Scope**: 確立 `GET /api/dashboard/v3/summary` 從後端傳輸層到前端視圖層的正式契約、列舉防禦映射、`SectionState` 狀態模型，以及 API-only 遷移順序。
>
> **Note**: 若本機 Step 0 Audit Report 發現 `accountType` / `transactionType` 序列化格式（字串 vs 整數）與本表不同，請以實際後端回傳值鎖定映射表左側欄位。

---

## 1. DashboardSummaryResponse 最終型別

嚴格維持三層邊界：**Backend DTO（HTTP JSON） → Frontend Infrastructure DTO → Frontend ViewModel**。`Frontend Infrastructure DTO` 僅限 `ApiDashboardDataSource` 與 `dashboardV3ApiMapper` 使用，嚴禁外洩至 Presentation 層。

### 1.1 Root：`DashboardSummaryResponse`

| JSON Key (Backend / Frontend DTO) | JSON 型別 | Nullable | 對應 ViewModel 路徑 | 備註與邊界規則 |
| :--- | :--- | :---: | :--- | :--- |
| `totalAssets` | `number` | 否 | `heroStats.totalAssets` / `accountSummary.totalBalance` | 純數值，不含貨幣符號；由後端聚合 `!IsDeleted && !IsSystemAccount`。 |
| `todayExpense` | `number` | 否 | `heroStats.spentToday` | 純數值，僅計算 `businessDate` 當日 `TransactionType == Expense`。 |
| `businessDate` | `string` | 否 | `meta.businessDate` | 格式固定 `YYYY-MM-DD`（`DateOnly`）；禁止使用 `new Date(str)` 直接解析。 |
| `accounts` | `AccountSummaryDto[]` | 否 | `accountSummary.items` | 無資料時為 `[]`；Mapper 需做 Null 防呆轉為 `[]`。 |
| `recentTransactions` | `RecentTransactionDto[]` | 否 | `recentTransactions.items` | 最多 5 筆，後端已依 `TransactionDate DESC, CreatedAtUtc DESC` 排序。 |

### 1.2 Child：`AccountSummaryDto`（`accounts[]`）

| JSON Key | JSON 型別 | Nullable | 對應 ViewModel 路徑 | 備註與邊界規則 |
| :--- | :--- | :---: | :--- | :--- |
| `id` | `string` (UUID) | 否 | `accountSummary.items[].id` | 唯一識別碼。 |
| `name` | `string` | 否 | `accountSummary.items[].name` | 使用者自訂名稱，不可經由 i18n 翻譯。 |
| `accountType` | `string` 或 `number` | 否 | `accountSummary.items[].type` | 經由 AccountType Mapper 轉為前端語意列舉。 |
| `balance` | `number` | 否 | `accountSummary.items[].balance` | 純數值，受 Privacy Mode 強制遮罩。 |

### 1.3 Child：`RecentTransactionDto`（`recentTransactions[]`）

| JSON Key | JSON 型別 | Nullable | 對應 ViewModel 路徑 | 備註與邊界規則 |
| :--- | :--- | :---: | :--- | :--- |
| `id` | `string` (UUID) | 否 | `recentTransactions.items[].id` | 唯一識別碼。 |
| `note` | `string` | **是** | `recentTransactions.items[].title` / `subtitle` | Mapper 決定顯示標題：`note` 去除空白後有值則用之，否則退回 `categoryName`。 |
| `categoryName` | `string` | 否 | `recentTransactions.items[].categoryName` | 分類名稱。 |
| `accountName` | `string` | 否 | `recentTransactions.items[].accountName` | 所屬帳戶名稱，不可翻譯。 |
| `amount` | `number` | 否 | `recentTransactions.items[].amount` | 純數值，禁止以正負號推測收支方向。 |
| `transactionType` | `string` 或 `number` | 否 | `recentTransactions.items[].type` | 經由 TransactionType Mapper 轉為前端語意列舉。 |
| `transactionDate` | `string` | 否 | `recentTransactions.items[].date` | 格式固定 `YYYY-MM-DD`（`DateOnly`）。 |

---

## 2. AccountType Mapping

Mapper 必須對後端原始值執行正規化（支援大小寫不敏感字串或對應整數列舉），並強制提供 `Unknown` 降級路徑，確保後端未來新增帳戶類型時前端不崩潰。

| Backend Raw Value（預期） | Frontend ViewModel Enum | i18n Translation Key | UI 顯示策略 |
| :--- | :--- | :--- | :--- |
| `"Bank"` / 對應 Enum 值 | `Bank` | `dashboard.accountType.bank` | 銀行圖示 |
| `"Cash"` / 對應 Enum 值 | `Cash` | `dashboard.accountType.cash` | 現金圖示 |
| `"CreditCard"` / 對應 Enum 值 | `CreditCard` | `dashboard.accountType.creditCard` | 信用卡圖示 |
| `"Investment"` / 對應 Enum 值 | `Investment` | `dashboard.accountType.investment` | 投資圖示 |
| 任何未預期值 / `null` / `undefined` | `Unknown` | `dashboard.accountType.unknown` | 通用預設帳戶圖示（不拋出例外） |

---

## 3. TransactionType Mapping

嚴格依據後端 `transactionType` 決定 UI 金額符號與視覺語意，**禁止**透過 `amount` 正負值反向猜測交易類型。

| Backend Raw Value（預期） | Frontend ViewModel Enum | 金額前置符號 | 視覺語意（色票） | 備註 |
| :--- | :--- | :---: | :--- | :--- |
| `"Income"` / 對應 Enum 值 | `Income` | `+` | 正向 / 收入色票（如綠色） | 計入收入顯示 |
| `"Expense"` / 對應 Enum 值 | `Expense` | `-` | 支出色票（如深灰/預設） | 計入支出顯示 |
| `"Transfer"` / 對應 Enum 值 | `Transfer` | 無（不加正負號） | 中性色票（如淺灰） | 內部轉帳，不計入收支 |
| 任何未預期值 / `null` / `undefined` | `Unknown` | 無 | 中性色票 | 安全降級，不拋出例外 |

---

## 4. SectionState 設計

為徹底消除「以假資料填補未實作功能」的風險，Dashboard V3 各區塊統一採用語意化的 `SectionState` 狀態模型，明確區分「有真實資料」、「新用戶無資料」與「後端尚未實作」。

### 狀態定義

| 狀態名稱 | 語意說明 | 攜帶資料 | UI 呈現行為 |
| :--- | :--- | :--- | :--- |
| `ready` | 後端已支援且具備有效資料 | `data`（非空陣列或有效物件） | 正常渲染 Widget 內容。 |
| `empty` | 後端已支援，但該使用者目前無資料 | 無（或空陣列） | 顯示 Empty State 引導文案與行動按鈕（如「新增第一個帳戶」）。 |
| `pending` | 後端尚未建立對應 Domain / API | 無 | 顯示 Pending State 占位卡片（如「功能準備中」），絕不顯示假數字。 |

### 各區塊在 Sprint 1 的狀態分配

| Dashboard 區塊 | Sprint 1 允許狀態 | 判定規則 |
| :--- | :--- | :--- |
| **Hero Stats (`totalAssets`, `spentToday`)** | `ready` | 永遠為 `ready`（新用戶顯示 `0`）。 |
| **Hero Stats (`remainingBudget`)** | `pending` | 固定輸出 `null` / `pending`，UI 顯示 `—`。 |
| **Account Summary** | `ready` \| `empty` | `accounts.length > 0` 為 `ready`，否則為 `empty`。 |
| **Recent Transactions** | `ready` \| `empty` | `recentTransactions.length > 0` 為 `ready`，否則為 `empty`。 |
| **Budget Radar** | `pending` | Sprint 1 固定為 `pending`。 |
| **Goal Progress** | `pending` | Sprint 1 固定為 `pending`。 |
| **Calendar Section** | `pending` | Sprint 1 固定為 `pending`（待 Sprint 2 Calendar API）。 |

---

## 5. Mock 移除策略

核心鐵律：**生產環境程式碼（Production Bundle）零 `mockDashboardData` 引用**。

| 處置類別 | 目標項目 | 具體執行策略 |
| :--- | :--- | :--- |
| **立即刪除（Remove）** | `assetChange`、`savingRate`、`contributionHistory` | 從 `DashboardViewModel`、Mapper 與元件中直接移除，不保留欄位。 |
| **立即刪除（Remove）** | `dashboardV3SummaryStore` | 確認全專案零引用後直接刪除檔案，消除與 `useDashboardData` 的雙重狀態來源。 |
| **改為 Pending（Replace）** | `mockBudgetRadar`、`mockGoalProgress`、`mockCalendarData`、`mockRemainingBudget` | 移除 `dashboardV3ApiMapper` 對上述 Mock 的所有 fallback，統一改為輸出 `pending` 狀態。 |
| **替換為真實 API（Migrate）** | `mockHeroStats`、`mockAccountSummary`、`mockRecentTransactions` | 全面由 `GET /api/dashboard/v3/summary` 真實資料映射取代。 |
| **隔離保留（Isolate）** | `MockDashboardDataSource` | 僅允許存在於測試目錄或本地開發切換工具中，**禁止**被 `dashboardV3ApiMapper` 或正式環境 `AuthApp` import。 |

---

## 6. API-only Migration Order

以下為低風險、每一步皆可獨立驗證 Build 與 Test 的遷移順序（不引入 TanStack Query 或 Feature Slice 重構）：

1. **Step 1：凍結 DTO 與 Enum 型別**
   * 建立 `DashboardSummaryResponseDto`、`AccountSummaryDto`、`RecentTransactionDto`、`AccountType`、`TransactionType`、`ApiError` 與 `DashboardFetchOptions`（含 `AbortSignal`）。
2. **Step 2：重整 ViewModel 與 `SectionState`**
   * 清理 `DashboardViewModel` 冗餘欄位，將 `budgetRadar`、`goalProgress`、`calendar`、`accountSummary`、`recentTransactions` 套用 `SectionState` 結構。
3. **Step 3：補齊共用 Formatter**
   * 確立 Currency Formatter、Privacy Formatter（整合 `isAmountHidden`）與安全 `DateOnly` Formatter（字串拆解，禁用 `new Date("YYYY-MM-DD")`）。
4. **Step 4：淨化 `dashboardV3ApiMapper`**
   * 移除所有 `mockDashboardData` import 與 fallback；實作 `AccountType` / `TransactionType` 防禦性映射、`note` 標題退回邏輯、陣列 Null 防呆與 `pending` 狀態輸出；補齊 Mapper Unit Tests。
5. **Step 5：強化 DataSource、Hook 與清理 Store**
   * 在 `IDashboardDataSource` 與 `ApiDashboardDataSource` 補上 `AbortSignal` 與 `ApiError` 標準化轉換。
   * 在 `useDashboardData` 補齊 Unmount 自動取消、`error` 狀態與 `retry()` 方法；刪除 `dashboardV3SummaryStore`。
6. **Step 6：補齊 i18n 五語系鍵值**
   * 於 `zh-TW`、`en`、`pt-BR`、`ko`、`ja` 補齊 `AccountType`、`Pending`、`Empty`、`Error` 與 `Retry` 翻譯鍵值。
7. **Step 7：更新 Widgets 並切換 `AuthApp`**
   * 更新 `DashboardV3Page` 與各 Widgets 支援 Loading Skeleton、Empty、Pending、Error 與 Privacy Masking。
   * 於 `AuthApp` 正式將注入的 `dataSource` 切換為 `ApiDashboardDataSource`，執行完整 Build 與 Test 驗收。