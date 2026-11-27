# Dashboard V4 Implementation Checklist

> **Document Path**: `docs/DashboardV4ImplementationChecklist.md`
>
> **Usage**: 作為交給 Copilot 或工程實作前的階段閘門（Gate Checklist）。**上一階段未打勾完成，禁止進入下一階段。**

---

## Gate 0：Contract Freeze（開工前必備）
- [ ] 更新 `docs/DashboardV4BackendContract.md` 至 v1.1，納入以下 5 項修正：
  - [ ] `dailySpendingLimit` 改為策略模式描述（Current Strategy: `AvailableMonthlyBudget / RemainingDays`）
  - [ ] 定義 `MonthlyFinancialProgress` Read Model（`savedAmount`、`investedAmount`、`source`）
  - [ ] `GET /api/transactions` 補齊 `page`、`pageSize`、`totalCount` 分頁契約
  - [ ] 新增 `POST /api/transactions/quick` 契約與後端預設值規則
  - [ ] 補齊 RFC 7807 `ProblemDetails` 錯誤範例與前端呈現對應表（Toast vs Field Error）

---

## Gate 1：Phase 1 — Frontend Scaffold
- [ ] 建立 `dashboard-v4/` 目錄與 `HomePage` / `TransactionsPage` / `PlanPage` / `MePage` 骨架
- [ ] 建立四分頁 `BottomNavigation` 與獨立預覽路由
- [ ] **Anti-V3.5 檢查**：確認 `dashboard-v4/` 內零 import `dashboard-v3/` 檔案
- [ ] `npm run build` 通過（0 Error）

---

## Gate 2：Phase 2 — Backend APIs
- [ ] 實作 `GET /api/dashboard/v4/home`（含 `BudgetService` 策略介面與 `MonthlyFinancialProgress` Read Model）
- [ ] 實作 `GET /api/me/assets`（僅查詢 `Account`，不載入交易或目標）
- [ ] 實作 `GET /api/transactions` 分頁與篩選（`year`, `month`, `date`, `keyword`, `type`, `page`, `pageSize`）
- [ ] 實作 `POST /api/transactions/quick`（自動帶入預設生活帳戶、今日日期、最近分類）
- [ ] 驗證 Swagger Schema 啟用 `SupportNonNullableReferenceTypes()` 且與契約 100% 一致
- [ ] `dotnet build` 與 Backend Integration Tests 全數通過

---

## Gate 3：Phase 3 — Feature Migration（嚴格依序）
- [ ] **1. `TransactionBottomSheet`**：支援快速記帳、完整欄位展開、`accountStore` 按需初始化、RFC 7807 錯誤分流（Toast / Field Error）與 `close → toast → onSuccess` 非阻斷流
- [ ] **2. `HomePage`**：串接 `/api/dashboard/v4/home`，前端零商業計算，支援 Privacy Masking、Empty / Error / Retry
- [ ] **3. `TransactionsPage`**：串接分頁 `/api/transactions`，月曆預設收合為 Filter UI，掛載右下角 `+` FAB
- [ ] **4. `MePage`**：串接 `/api/me/assets`，依 `Cashflow` / `Saving` / `Invest` 分組顯示帳戶與總資產
- [ ] **5. `PlanPage`**：`BudgetView` 與 `GoalView` 各自獨立串接（或安全顯示 `Pending`），禁止共用肥大 API
- [ ] 五語系（`zh-TW`, `en`, `pt-BR`, `ko`, `ja`）i18n 鍵值補齊
- [ ] `npm run build` 與 `npm test` 全數通過

---

## Gate 4：Phase 4 — Cutover & V3 Removal
- [ ] 預設路由正式切換至 V4
- [ ] 刪除 `dashboard-v3/`、Legacy Calendar 與 Legacy `TransactionPage`
- [ ] 全專案搜尋確認零殘留 `/api/dashboard/v3/summary` 與 `/api/dashboard/v3/calendar` 引用
- [ ] 完成 Mobile 真機／模擬器 E2E Smoke Test（10 秒記帳流、分頁捲動、隱私遮罩、401/400/500 錯誤處理）