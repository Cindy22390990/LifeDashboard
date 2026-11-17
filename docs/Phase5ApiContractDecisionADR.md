Phase 5 API Contract Decision ADR

1. RadarStateDto 是否允許新增 RemainingLivingBudget？

決策：允許 (Allowed)

原因：RadarStateDto 屬於 Application Layer 的 Read Model (唯讀視圖模型)。在 Clean Architecture 中，DTO 的存在目的就是為了貼合前端 UI 的顯示需求。新增 RemainingLivingBudget 可以將計算邏輯集中在後端 (Single Source of Truth)，避免前端自行運算，這完全符合 BFF (Backend for Frontend) 的設計精神。

限制：Phase 5 僅將既有 `BudgetSnapshot.RemainingLivingBudget` expose 至 DTO，不重新計算 Budget，不新增 `TotalBudget` 或 `TotalSpent` 欄位，也不可影響底層 Domain Entity。

Phase 5 Budget Radar response addition is limited to `RemainingLivingBudget`, exposed from the existing `BudgetSnapshot`. It does not authorize adding `TotalBudget` or `TotalSpent`. `UsagePercentage` and `Status` remain future/not implemented and are not Phase 5 response requirements.

2. Dashboard response 是否允許新增欄位？

決策：允許 (Allowed)

原因：Phase 5 的核心目標是透過既有 Dashboard API 提供專屬的 Aggregation Read Model。Dashboard API 的 Response DTO 應能支援儀表板所需的資訊聚合。

限制：

只能在 DTO 層級新增欄位。

新增的欄位資料必須能從「現有」的資料庫結構與 Repository 取得。

不允許為了滿足新欄位而要求修改 Database Schema 或新增假 Entity。

Phase 5 Dashboard response additions are limited to `TotalAssets`, `MonthlyIncome`, `MonthlyExpense`, and `RecentTransactions` (maximum 5). The existing `Goals` response is retained; no separate `GoalsProgress` collection is added.

3. Existing tests 是否視為 frozen contract？

決策：是 (Yes, Strictly Frozen)

原因：Phase 1 至 Phase 4 建立的所有單元測試與整合測試（包含 Transaction Atomicity, Soft Delete Exclusion, InsufficientFunds Rollback 等）是系統財務一致性的絕對防線。

規範：

除 `Phase5TestCompatibilityDecisionADR.md` 明確允許的 DTO shape assertion 更新外，現有測試均視為 Frozen Contract。此例外僅涵蓋核准新增欄位的 shape assertions；不可修改、註解或刪除任何 business behavior assertions 或其他既有測試。

若 Phase 5 實作導致 frozen business behavior tests 失敗，必須修正 Phase 5 程式碼或停止並進行 ADR review，不得修改測試期待值。

4. Phase 5 API compatibility rule

決策：Phase 5 API 相容性與邊界規則如下

純唯讀聚合原則 (Pure Read-Only Aggregation)：

Dashboard API (`GET /api/dashboard/summary`, `GET /api/budget/radar`) 必須是純粹的 GET 請求。

執行這些 API 絕對不允許產生任何副作用 (Side Effects) 或改變資料庫狀態。

向下相容不破壞原則 (Non-Breaking Existing APIs)：

Phase 5 的實作不得更改既有的 API (如 POST /api/transaction, GET /api/account) 的 Request / Response 結構。

既有 API 的行為必須保持 100% 向下相容。

前端零業務邏輯原則 (Zero Frontend Business Logic)：

後端 API Response 必須提供「可直接渲染 (Ready-to-Render)」的最終結果（例如已計算好的剩餘額度、已過濾軟刪除的明細）。

禁止後端丟出原始資料並要求 Frontend / Zustand 進行預算加減計算。