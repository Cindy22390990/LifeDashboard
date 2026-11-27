# Dashboard V4 Frontend Integration Readiness Review

> **Document Path**: `docs/DashboardV4FrontendIntegrationReadinessReview.md`
>
> **Status**: Accepted — Phase 3 Readiness Gate
>
> **Date**: 2026-11-26
>
> **Purpose**: 作為 Dashboard V4 Phase 3（Frontend Integration）啟動前的唯一整合檢查文件（Readiness Gate），確認前後端契約、錯誤處理、資料映射與狀態管理皆已就緒，且不引入 V3.5 混血依賴。

---

## 1. Dashboard API DTO 是否完整

**結論：核心模組（Home / Transactions / Me）已完整，僅需核對 1 處欄位命名。**

| 頁面 / API | DTO 覆蓋狀態 | 需核對細節 |
| :--- | :---: | :--- |
| **Home** (`GET /api/dashboard/v4/home`) | ✅ 完整 | 涵蓋 `businessDate`、`dailySpendingLimit`、`dailySpent`、`dailyRemaining`、`monthlyProgress`、`todayTransactions[]`。<br>**核對點**：確認實碼中 `monthlyProgress` 的子欄位名稱為 `saved` / `invested` 或 Read Model 版的 `savedAmount` / `investedAmount`，避免 Mapper 讀到 `undefined`。 |
| **Transactions** (`GET /api/transactions`) | ✅ 完整（Read API 已 Freeze） | Response 為 `{ totalIncome, totalExpense, items[] }`；`page`、`pageSize` 為 Request only，不回傳 `totalCount`、`page` 或 `pageSize`。已支援 `keyword`、`accountId`、`categoryId`、`transactionType`、日期區間及排序。 |
| **Accounts** (`GET /api/accounts`) | ✅ 完整（Read Model v1 已 Freeze） | Response 為 `{ totalAssets, items[] }`；帳戶欄位為 `id`、`name`、`accountType`、`balance`。無 pagination/filter/query parameter。 |
| **Categories** (`GET /api/categories`) | ✅ 完整（Read Model v1 已 Freeze） | Response 為 `CategoryReadDto[]`；欄位為 `id`、`name`、`icon`、`bucketType`、`createdAt`；無 filter/pagination/CRUD。 |
| **Me** (`GET /api/me/assets`) | ✅ 完整 | 獨立回傳 `totalAssets` 與 `accounts[]`（含 `accountType`），與 V3 Summary 徹底解耦。 |
| **Plan** (`Budget` / `Goal`) | ⏳ 按序推進 | 維持獨立 Bounded Context，Phase 3 排在 Step 5，未就緒前前端安全顯示 `Pending` State。 |

---

## 2. Quick Create API Contract 是否足夠

**結論：足夠支撐「3 秒快速記帳」MVP。**

* **Request 足夠**：前端僅需送出 `{ amount, note, transactionType }`，其餘預設值（`Cashflow` 預設帳戶、Preference 優先之預設分類、後端 `businessDate`）已由 `TransactionDefaultResolver` 完整接管。
* **Response 核對**：確認 `POST /api/transactions/quick` 成功時回傳新建的 `Transaction` DTO（至少含 `id`、`amount`、`transactionType`），供前端顯示成功 Toast 與觸發 `queryClient.invalidateQueries()`。
* **邊界提醒**：若使用者在 `TransactionBottomSheet` 展開進階選項（手動切換日期或帳戶），前端需切回呼叫完整版 `POST /api/transactions`，兩條路徑共用同一個 `useMutation` 錯誤與刷新流。

---

## 3. 422 ProblemDetails 前端是否有處理位置

**結論：架構已有明確歸屬，Phase 3 Step 1 需落實雙層接線。**

依 `DashboardV4ErrorHandlingSpec.md` 與 Phase 2 Decision 2（無生活帳戶改回 `422`），前端須在以下兩個位置承接：

1. **API Client / Error Parser 層**：統一解析 RFC 7807 `ProblemDetails`（`status`、`type`、`title`、`detail`、`errors`），轉換為標準化 `ApiError`，**禁止 Component 直接拆解 `axios` / `fetch` 原始錯誤**。
2. **`TransactionBottomSheet`（`useMutation` `onError`）**：
   * 收到 `422`（Business Rule Error，如 `no-cashflow-account`、`insufficient-balance`）時，**強制保留 Sheet 開啟與表單輸入內容**，於表單內 `InlineErrorBanner` 顯示訊息。
   * 若 `type` 對應「無生活帳戶」，可在 Banner 內提供直接導向 `MePage` 建立帳戶的復原按鈕。

---

## 4. Mock Data 與 Backend Response 差異

Phase 1 Scaffold 階段的靜態假資料與實際後端回應最容易出現以下 4 種落差，交由 `mappers/` 統一收斂：

| 差異項目 | 常見 Mock 寫法 | 實際 Backend Contract | 前端處理方式 |
| :--- | :--- | :--- | :--- |
| **日期欄位** | `date: "2026-11-26"` | `transactionDate: "2026-11-26"` | Mapper 統一讀 `transactionDate`，並用安全 `DateOnly` 工具解析（禁用 `new Date()`）。 |
| **空值與空陣列** | `note: ""` | `note: string \| null`；陣列空時為 `[]` | Mapper 強制執行 `note?.trim() \|\| fallbackLabel` 與 `items ?? []`。 |
| **帳戶類型** | `type: 0` 或 `"Bank"` | `accountType: "Cashflow" \| "Saving" \| "Invest"` | 消費 Canonical String，未知值一律降級為 `'Unknown'`。 |
| **交易列表結構** | 分頁物件 `{ totalCount, page, pageSize, items }`（歷史假設） | `{ totalIncome, totalExpense, items }`；`page` / `pageSize` 為 request 參數，response 不回傳 metadata | `TransactionsPage` 透過 TanStack Query Infinite/Load More 消費 `items`；以滿頁判斷是否有下一頁，不依賴 `totalCount`。 |

---

## 5. Zustand Store 是否需要調整

**結論：需要嚴格收斂，避免 V3 Store 滲透進 V4。**

依據 **ADR-006（Server State Management Strategy）**：

* **保留（跨頁純 UI State）**：`privacyStore`、`languageStore` 可直接於 V4 沿用。
* **新增（V4 專屬純 UI State）**：建立輕量 `bottomSheetStore`（僅存 `isOpen`、`mode`、`transactionId`、`defaultDate`），讓 `HomePage` 與 `TransactionsPage` 共用喚起邏輯。
* **禁止在 V4 使用（關鍵防呆）**：
  * ❌ 禁止引用 `dashboardV3Store`。
  * ❌ **禁止在 V4 的 `TransactionBottomSheet` 沿用 V3 的 `accountStore` 或 `transactionStore` 來存後端資料**。在 V4 中，帳戶清單應透過 TanStack Query（`dashboardV4Keys.me.assets()`）取得，新增交易應透過 `useMutation` 並在成功後呼叫 `invalidateQueries`（失效 `home`、`transactions`、`me.assets`）。

---

# Additional Integration Recommendations

## 1. Frontend Mapper Strategy

為確保 Backend Contract 變動不會直接震動 UI 元件，嚴格執行以下單向資料映射邊界：

```text
Backend DTO → Mapper → ViewModel → Component
```

* **Component 禁止直接存取 Backend DTO**：所有呈現層元件僅消費經轉換後的 ViewModel。
* **集中管理**：所有欄位轉換、Enum 防禦降級、空值處理與日期安全格式化，一律集中於 `mappers/` 目錄下的純函數實作。
* **後續文件追蹤**：建議後續建立 **`ADR-009: Frontend Mapper Strategy`**，正式固化跨模組的 DTO 映射規範。

---

## 2. Query Invalidation Matrix

依據 ADR-006 與 `DashboardV4QueryKeyConvention.md`，所有 V4 Mutation 成功後必須依下表精準執行 `queryClient.invalidateQueries()`：

| Mutation | Invalidate Queries |
| :--- | :--- |
| **Quick Create** | `home`、`transactions`、`me.assets` |
| **Full Transaction** | `home`、`transactions`、`me.assets` |
| **Edit Transaction** | `home`、`transactions`、`me.assets` |
| **Delete Transaction** | `home`、`transactions`、`me.assets` |
| **Budget Update** | `budget`、`home` |
| **Goal Deposit** | `goal`、`home`、`me.assets` |

> **維護規範**：後續於 V4 新增任何 Mutation 時，**必須同步更新此 Matrix**，確保跨頁資料一致性。

---

## 3. BottomSheet Mode Definition

為避免未來隨著功能擴充產生大量散落的 Boolean Flag（如 `isQuickMode`、`isEditMode`、`isTransferOpen`），`bottomSheetStore` 僅允許保存以下 4 個最小 UI 狀態欄位：

* `isOpen` (`boolean`)
* `mode` (`'quick' | 'full' | 'edit' | 'transfer'`)
* `transactionId` (`string | null`，僅 `edit` 模式使用)
* `defaultDate` (`string | null`，格式 `YYYY-MM-DD`)

### Mode 定義
* **`quick`**：3 秒快速記帳模式（對應 `POST /api/transactions/quick`）。
* **`full`**：完整欄位記帳模式（支援手動切換帳戶、分類與日期，對應 `POST /api/transactions`）。
* **`edit`**：編輯既有交易模式（依 `transactionId` 載入並更新）。
* **`transfer`**：跨帳戶轉帳模式（預留未來階段擴充，Phase 3 MVP 暫不開放 UI）。

---

## 4. Optimistic Update Strategy

* **MVP 策略**：目前 Quick Create 採標準非同步刷新流：
  ```text
  POST → invalidateQueries → refetch
  ```
* **Phase 3 邊界**：**Optimistic Update 不納入 Phase 3**，避免過早引入複雜的樂觀回滾（Rollback）與暫態 ID 同步風險。
* **後續演進**：若未來因體感延遲需求決定導入 Optimistic Update，須另外建立專屬 ADR 評估 Rollback 與衝突處理規則。

---

## 5. API Response Verification

* **串接前強制核對**：正式開始 Phase 3 Step 1 串接前，須再次確認本機實作的 **`POST /api/transactions/quick`** 成功 Response 結構是否與 `DashboardV4BackendContract.md` 完全一致。
* **衝突處理原則**：若實作與文件存在落差，**以 Backend Contract 為準並同步修正文件**，嚴禁前端於 Mapper 內私自寫臨時 workaround 掩蓋契約不一致。

---

# Final Readiness Assessment

| Assessment Item | Status |
| :--- | :--- |
| **Backend Readiness** | **Ready** |
| **Frontend Integration Readiness** | **Ready** |
| **Integration Blocking Issue** | **None** |
| **Recommended Next Phase** | **Phase 3 - Dashboard V4 Frontend Integration** |