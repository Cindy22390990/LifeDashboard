## Phase 5 Implementation Decisions

### Dashboard

- 保留既有 Dashboard API
- 新增 Response 欄位：`TotalAssets`、`MonthlyIncome`、`MonthlyExpense`、`RecentTransactions`（最多 5 筆）
- Goals 維持使用既有 `Goals` response；不新增重複的 `GoalsProgress`
- Recent Transaction 暫採 Service Layer Take
- 不新增 Repository Query

### Budget Radar

- 使用現有 BudgetSnapshot
- Phase 5 僅新增 expose `RemainingLivingBudget`
- 不新增 `TotalBudget` 或 `TotalSpent` response 欄位
- `UsagePercentage` 與 `Status` 為未實作的 future contract，不納入本階段
- 不修改 FixedCommitment Domain
- 不建立 Payment Tracking

### Category

- Budget Radar 不負責 Category Analysis
- Category Breakdown 維持 Report 責任

### Test Compatibility

- 僅允許依 `Phase5TestCompatibilityDecisionADR.md` 更新已核准欄位的 DTO shape assertions。
- Business behavior tests 維持 frozen。

### Forbidden

- 不新增 Entity
- 不新增 Migration
- 不修改 Transaction Flow
- 不修改 Balance Logic
