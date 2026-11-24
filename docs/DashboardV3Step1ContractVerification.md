# Dashboard V3 Step 1.5 Contract Verification

> **Document Path**: `docs/DashboardV3Step1ContractVerification.md`
>
> **Stage**: Post-Step 1 (DTO & Enum Types) / Pre-Step 2 (ViewModel & SectionState)
>
> **Mode**: Read-Only Architecture Verification

---

## 1. `DashboardSummaryResponse` DTO vs. Backend Contract 檢查

核對前端 Infrastructure 層 DTO 與後端 `GET /api/dashboard/v3/summary` 凍結合約的一致性：

| 層級 / 欄位 | Backend Contract 預期 | Frontend DTO 驗收基準 | Step 1.5 核對重點 |
| :--- | :--- | :--- | :--- |
| **`DashboardSummaryResponse`** | | | |
| `totalAssets` | `decimal` (non-null) | `number` | 必須為純數值，不含貨幣符號。 |
| `todayExpense` | `decimal` (non-null) | `number` | 必須為純數值。 |
| `businessDate` | `DateOnly` (`YYYY-MM-DD`) | `string` | 嚴禁定義為 `Date` 物件。 |
| `accounts` | `AccountSummaryDto[]` | `AccountSummaryDto[]` | 不可為 `any[]`，需有獨立子 DTO 型別。 |
| `recentTransactions` | `RecentTransactionDto[]` | `RecentTransactionDto[]` | 不可為 `any[]`，需有獨立子 DTO 型別。 |
| **`AccountSummaryDto`** | | | |
| `id` | `Guid` | `string` | UUID 字串。 |
| `name` | `string` | `string` | 使用者自訂名稱。 |
| `accountType` | `string` 或 `number` | 對應後端實際序列化型別 | **關鍵核對**：確認與實際 API JSON 輸出一致（字串 vs 整數）。 |
| `balance` | `decimal` | `number` | 純數值。 |
| **`RecentTransactionDto`** | | | |
| `id` | `Guid` | `string` | UUID 字串。 |
| `note` | `string?` | `string \| null` | **關鍵核對**：必須允許 `null`，不可寫死為 non-null `string`。 |
| `categoryName` | `string` | `string` | 分類名稱。 |
| `accountName` | `string` | `string` | 帳戶名稱。 |
| `amount` | `decimal` | `number` | 純數值。 |
| `transactionType` | `string` 或 `number` | 對應後端實際序列化型別 | **關鍵核對**：確認後端 DTO 已實際暴露此欄位，避免阻塞 Mapper。 |
| `transactionDate` | `DateOnly` (`YYYY-MM-DD`) | `string` | 嚴禁定義為 `Date` 物件。 |

### 禁制項目檢查（Negative Check）
* DTO 中**不得**包含 `title`、`userId`、`isDeleted`、`isSystemAccount`、`createdAtUtc`。
* DTO 中**不得**提前塞入 `budgetRadar`、`goalProgress`、`remainingBudget` 等未實作欄位。
* DTO 檔案**不得**被任何 Presentation 層（`DashboardV3Page`、Widgets）直接 import。

---

## 2. `dashboardV3ApiMapper` 對 `mockDashboardData` 的依賴檢查

依照既定遷移順序，Step 1 僅建立 DTO 與 Enum 型別，Mapper 重構排定於 Step 4。因此在 Step 1.5 時間點，預期狀態如下：

* **預期現況**：`dashboardV3ApiMapper` **目前仍依賴 `mockDashboardData`** 作為缺失欄位的 fallback。
* **本階段核對目標**：
  1. 確認 Step 1 **沒有**在新的 DTO 或 Enum 檔案中反向引入 `mockDashboardData`。
  2. 盤點 `dashboardV3ApiMapper` 中所有引用 `mockDashboardData` 的欄位（預期為 `budgetRadar`、`goalProgress`、`calendar`、`remainingBudget`，以及舊版 `accounts` / `recentTransactions` fallback），列為 Step 4 必須一次性清除的清單。
  3. 確認 `dashboardV3ApiMapper` 尚未具備完整的 `AccountType` / `TransactionType` 防禦性 `Unknown` 映射，待 Step 4 補齊。

---

## 3. API DTO → ViewModel 欄位流向

確認資料流嚴格遵守單向防腐層邊界（Infrastructure → Mapper → ViewModel → Presentation）：

```text
[Backend JSON]
GET /api/dashboard/v3/summary
  │
  ▼
[Frontend Infrastructure DTO]
DashboardSummaryResponseDto
  ├── totalAssets        ──────────────────────┬──▶ heroStats.totalAssets
  │                                            └──▶ accountSummary.totalBalance
  ├── todayExpense       ─────────────────────────▶ heroStats.spentToday
  ├── businessDate       ─────────────────────────▶ meta.businessDate
  ├── accounts[]         ──(AccountType Map)──────▶ accountSummary.items[]
  └── recentTransactions[]
        ├── id           ─────────────────────────▶ recentTransactions.items[].id
        ├── note + categoryName ──(Title Rule)────▶ recentTransactions.items[].title / subtitle
        ├── accountName  ─────────────────────────▶ recentTransactions.items[].accountName
        ├── amount       ─────────────────────────▶ recentTransactions.items[].amount
        ├── transactionType ──(TxType Map)────────▶ recentTransactions.items[].type
        └── transactionDate ──────────────────────▶ recentTransactions.items[].date
```

---

## 4. 目前沒有 Backend Source 的 ViewModel 欄位

將無後端來源的欄位明確區分為「**轉為 Pending**」與「**直接刪除**」，防止在 Step 2 繼續殘留無效結構：

| 分類 | ViewModel 欄位 | 原因 | 處置方式（Step 2 / Step 4） |
| :--- | :--- | :--- | :--- |
| **轉為 Pending** | `heroStats.remainingBudget` | 依賴未定義的 Budget / Cash Flow 合約 | 改為 `number \| null`，Sprint 1 固定輸出 `null`（UI 顯示 `—`）。 |
| **轉為 Pending** | `budgetRadar` | 尚無 Backend Budget Read Model | 改以 `SectionState` 包裝，Sprint 1 固定輸出 `pending`。 |
| **轉為 Pending** | `goalProgress` | 尚無 `GoalFundingSummary` Read Model | 改以 `SectionState` 包裝，Sprint 1 固定輸出 `pending`。 |
| **轉為 Pending** | `calendar` | 待 Sprint 2 實作獨立 Calendar API | 改以 `SectionState` 包裝，Sprint 1 固定輸出 `pending`。 |
| **直接刪除** | `heroStats.assetChange` | 非 Dashboard V3 核心指標（屬 Reports） | Step 2 直接從 ViewModel 移除。 |
| **直接刪除** | `heroStats.savingRate` | 非 Dashboard V3 核心指標（屬 Reports） | Step 2 直接從 ViewModel 移除。 |
| **直接刪除** | `goalProgress.contributionHistory` | 屬 Goal Detail 頁面，非首頁摘要 | Step 2 直接從 ViewModel 移除。 |

---

## 5. `SectionState` 導入前需要調整的 Model（Step 2 準備）

在導入 `SectionState<T>`（`ready` | `empty` | `pending`）前，以下 ViewModel 結構必須先行調整，以消除「假設資料永遠存在」的隱性耦合：

1. **`DashboardViewModel`（頂層模型）**
   * 新增 `meta: { businessDate: string }`。
   * 將 `accountSummary`、`recentTransactions`、`budgetRadar`、`goalProgress`、`calendar` 從直接持有資料陣列/物件，改為由 `SectionState<T>` 包裝。
2. **`HeroStatsViewModel`**
   * 移除 `assetChange`、`savingRate`。
   * 將 `remainingBudget` 改為 `number | null`。
3. **`AccountSummaryViewModel`**
   * 確保 `items[].type` 使用 Step 1 建立的前端 `AccountType` Enum（含 `'Unknown'`），而非原始字串或數字。
4. **`RecentTransactionViewModel`**
   * 確保 `type` 使用 Step 1 建立的前端 `TransactionType` Enum（含 `'Unknown'`）。
   * 明確區分 `title`（由 Mapper 依 `note?.trim() || categoryName` 產生）與 `categoryName`。
5. **`BudgetRadarViewModel` / `GoalProgressViewModel` / `CalendarViewModel`**
   * 移除 `contributionHistory` 等冗餘欄位；保留既有核心型別作為 `SectionState<T>` 的泛型參數 `T`，以便未來從 `pending` 切換回 `ready` 時無需大改型別。

---

## 6. `dashboardV3SummaryStore` 存廢評估

* **結論**：**無存在必要，建議於 Step 5 / Step 6 直接刪除。**
* **架構依據**：
  1. Dashboard V3 的資料流已由 `useDashboardData → IDashboardDataSource` 完整封裝，屬於單純的唯讀查詢生命週期。
  2. `dashboardV3SummaryStore` 目前疑似未被任何 Dashboard V3 元件引用；若繼續保留，未來極易演變為「部分元件讀 Hook、部分元件讀 Zustand Store」的 **Split-Brain State（雙重狀態來源）**。
  3. 依據長期架構決策，Zustand 應專責 UI State（如 `privacyStore`），遠端 Server State 未來應交由專屬工具管理，而非手寫分散的 Store。
* **刪除前必要檢查**：執行全專案引用搜尋，確認無任何頁面、Hook 或測試依賴 `dashboardV3SummaryStore` 後再行移除。

---

## Step 1.5 驗收門檻（進入 Step 2 前必檢）

* [ ] `DashboardSummaryResponseDto`、`AccountSummaryDto`、`RecentTransactionDto` 與後端實際 JSON 欄位、型別、Nullability 完全一致。
* [ ] `AccountType` 與 `TransactionType` 皆已包含 `'Unknown'` 降級值。
* [ ] `tsc --noEmit` 編譯零錯誤，且無任何 Presentation 元件直接 import DTO。
* [ ] 已確認進入 Step 2 時要從 `DashboardViewModel` 刪除的冗餘欄位清單。