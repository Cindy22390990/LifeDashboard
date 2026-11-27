# Dashboard V4 Migration Plan (V3 → V4)

> **Document Path**: `docs/DashboardV4MigrationPlan.md`
>
> **Version**: 1.0
>
> **Status**: Active — Migration Blueprint
>
> **Reference**: `docs/DashboardV4UXArchitecture.md` (v1.1), `docs/DashboardV4BackendContract.md`

---

## 1. Migration Goal & Anti-V3.5 Principle

本遷移計畫的核心目標是將 LifeDashboard 從 V3（財務控制中心）平滑演進至 V4（日常消費決策工具），同時確保**底層架構同步升級**。

### 嚴格禁止「V3.5 混血架構」
在遷移過程中，**嚴禁**出現以下依賴路徑：

```text
❌ Anti-Pattern (V3.5):
V4 UI (dashboard-v4/)
  ↓
V3 Store / V3 Mapper
  ↓
GET /api/dashboard/v3/summary 或 /api/dashboard/v3/calendar
```

所有 V4 頁面皆必須透過獨立的 V4 DataSource / API Client 消費專屬 V4 / RESTful 端點，讓 V3 成為可被整包安全移除的隔離區塊。

---

## 2. Four-Phase Migration Strategy

### Phase 1：Frontend Scaffold（建立隔離邊界，零動 V3）
* **目標**：建立 V4 獨立呈現層與路由開關，不修改任何 `dashboard-v3/` 檔案。
* **工作項目**：
  1. 建立 `src/presentation/components/dashboard-v4/`（`common/`、`home/`、`transactions/`、`plan/`、`me/`）與四個獨立頁面（`HomePage`、`TransactionsPage`、`PlanPage`、`MePage`）。
  2. 建立 V4 四分頁 `BottomNavigation` 外框與獨立路由（例如透過 Feature Flag 或 `/v4/*` 預覽路由與 V3 並存）。
  3. 建立 V4 獨立 API Client 與 DTO 型別目錄，禁止 import 任何 `dashboard-v3` 檔案。

---

### Phase 2：Backend Contract Completion（補齊 5 項 Review 補強與 V4 API）
* **目標**：先凍結 `DashboardV4BackendContract.md` v1.1，再實作後端專屬端點，不破壞現有 V3 API。
* **工作項目**：
  1. **實作 `GET /api/dashboard/v4/home`**：
     * `dailySpendingLimit`：由 `BudgetService` 透過**可替換預算策略（Strategy Pattern）**計算（Current Strategy：`AvailableMonthlyBudget / RemainingDays`），不將單一公式寫死於 Controller 或 DTO。
     * `monthlyProgress`：建立獨立 **`MonthlyFinancialProgress` Read Model**（包含 `savedAmount`、`investedAmount`、`source` 如 `"AccountTransfer"`），明確界定儲蓄與投資來源，避免 `AllocationService` 猜測。
  2. **實作 `GET /api/me/assets`**：
     * 僅回傳 `totalAssets` 與 `accounts[]`，徹底與 V3 Summary 解耦。
  3. **完善 `/api/transactions` RESTful 端點**：
     * `GET /api/transactions`：補齊分頁參數（`page`、`pageSize`）與分頁回應結構（`totalCount`、`page`、`pageSize`、`items[]`），並支援 `year`、`month`、`date`、`keyword`、`type` 篩選。
     * `POST /api/transactions/quick`：新增 Quick Create 端點（僅需 `amount`、`note`、`transactionType`），由後端自動帶入預設生活帳戶、今日日期與最近使用分類，支援 3 秒快速記帳。
  4. **標準化 Error Contract**：
     * 統一回傳 RFC 7807 `ProblemDetails`（含語意 `type` URI、`title`、`status`、`detail`），明確區分前端應顯示 **Toast** 或 **Field Error** 的錯誤類型。

---

### Phase 3：Feature Migration（依核心資料流順序漸進遷移）
嚴格遵守以下 **5 步驟順序**，確保最高頻的核心寫入流最先穩定：

1. **Step 1：`TransactionBottomSheet`（核心寫入流）**
   * 優先對接 `POST /api/transactions/quick`（快速模式）與 `POST /api/transactions`（完整展開模式）。
   * 確立 `close sheet → toast → onSuccess(transaction)` 非阻斷事件流與 `accountStore` 按需初始化。
2. **Step 2：`HomePage`（日常決策首屏）**
   * 串接 `GET /api/dashboard/v4/home`，實作 `DailyDecisionHero`、`MonthlyProgressCard` 與 `TodayTransactionList`。
   * 掛載 `+ 記一筆` 喚起 `TransactionBottomSheet`，送出成功後背景刷新 Home API。
3. **Step 3：`TransactionsPage`（交易查詢與補記）**
   * 串接分頁版 `GET /api/transactions`，實作 `SearchBar`、收合式月曆 Filter UI、無限捲動／分頁載入與右下角 `+` FAB。
4. **Step 4：`MePage`（長期安全感與管理）**
   * 串接 `GET /api/me/assets`，呈現 `AssetOverview` 與依 `Cashflow` / `Saving` / `Invest` 分組的 `AccountList`，並整合分類、設定與備份入口。
5. **Step 5：`PlanPage`（預算與目標）**
   * 採純前端組合模式（`BudgetView` + `GoalView`），分別串接獨立的 Budget Context API 與 Goal Context API（未就緒前維持明確 `Pending` State）。

---

### Phase 4：Remove V3（正式切換與架構清理）
* **切換條件**：Phase 3 全部頁面完成 E2E 驗收且正式環境穩定運行。
* **清理範圍**：
  1. 將預設根路由切換至 V4 `HomePage`，移除 Feature Flag。
  2. 刪除 `src/presentation/components/dashboard-v3/` 與 V3 專屬 Store / Mapper。
  3. 刪除 Legacy `Calendar` 元件與 Legacy 獨立 `TransactionPage`。
  4. 移除前端對 `GET /api/dashboard/v3/summary` 與 `/api/dashboard/v3/calendar` 的所有依賴；後端標記 V3 端點為 Deprecated 後擇期下線。