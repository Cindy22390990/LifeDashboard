# Phase 3 Budget Radar Domain Design

Status: Architecture proposal based on the current repository and `CoreTransactionBudgetRadarSpec.md` v1.4. This document does not change application behavior or API contracts.

## 1. Current Domain Map

| Concept | Current source of truth | Budget Radar meaning |
| --- | --- | --- |
| Income / expense / transfer | `Transaction.Type`, `Amount`, `TransactionDate`, `CategoryId`, `AccountId`, optional `ToAccountId` | Period income and classified expenses are derived from transaction rows, not account balances. |
| Account position | `Account.InitialBalance`, `CurrentBalance`, `AllowNegativeBalance`, soft-delete state | Represents account-level balance/liquidity; it is not the Radar budget input. |
| Category bucket | `Category.BucketType` | Classifies an Expense as Living, Responsibility, Entertainment, Savings, or Investment. |
| Fixed commitment | `FixedCommitment.MonthlyAmount`, `StartMonth`, optional `EndMonth` | A recurring planned deduction applied by payroll period; it has no generated Transaction. |
| Goal progress | `Goal.CurrentAmount`, `Status`, `GoalType`; related transactions may carry `GoalId` | A separate savings/purchase progress concept, not itself a Living Expense. |
| Payroll preferences | `SystemSetting.Payday` and `AllocationRatio` | Payday determines the cycle; allocation ratios are stored but are currently not used by `BudgetRadarService`. |

Relevant implementations: [BudgetRadarService](../LifeDashboard.Application/Services/BudgetRadarService.cs), [TransactionRepository](../LifeDashboard.Infrastructure/Repositories/TransactionRepository.cs), [Transaction](../LifeDashboard.Domain/Entities/Transaction.cs), [Account](../LifeDashboard.Domain/Entities/Account.cs), [FixedCommitment](../LifeDashboard.Domain/Entities/FixedCommitment.cs), [Goal](../LifeDashboard.Domain/Entities/Goal.cs), and [SystemSetting](../LifeDashboard.Domain/Entities/SystemSetting.cs).

All relevant data access is scoped by the authenticated `userId`; the design should preserve explicit user scoping in repository/query contracts.

## 2. Budget Calculation Source

The current `BudgetRadarService` uses the requested `currentDate` and the user's `Payday` to derive an inclusive payroll period:

- The period starts on the most recent payday, including today when today is payday.
- It ends the day before the next payday.
- If a payday does not exist in a month, it is clamped to that month's last day.
- `PayrollPeriodMonth` is the first day of the period start month.

Current calculation:

```text
PeriodIncome = sum(Transaction.Amount)
  where Type = Income and TransactionDate is within the payroll period

PeriodLivingExpense = sum(Transaction.Amount)
  where Type = Expense
    and Category.BucketType = Living
    and TransactionDate is within the payroll period

ActiveCommitmentTotal = sum(MonthlyAmount)
  for commitments active in PayrollPeriodMonth

TotalLivingBudget = PeriodIncome - ActiveCommitmentTotal
RemainingLivingBudget = TotalLivingBudget - PeriodLivingExpense
RemainingDays = inclusive days from currentDate through period end
DailySafeLimit = round(RemainingLivingBudget / RemainingDays, 2, AwayFromZero)
TodaySpent = sum of today's Living Expenses
TodayRemaining = DailySafeLimit - TodaySpent
```

Uncategorized expenses are summed separately when `Type = Expense` and `CategoryId` is null. They are reported for classification follow-up and do not reduce `RemainingLivingBudget`.

The current service does not query `Account.CurrentBalance` when calculating Radar. It also does not clamp negative budget or daily values to zero. This preserves the distinction between *budget remaining* and *cash currently held in accounts*.

The current endpoint is authenticated `GET /api/budget/radar?currentDate=yyyy-MM-dd`; it requires a date, derives `userId` from JWT claims, and returns 404 if the user has no SystemSetting. The existing response is [RadarStateDto](../LifeDashboard.Application/Models/DTOs/RadarStateDto.cs).

## 3. Transaction Classification Rules

The current transaction model has three transaction types: `Expense`, `Income`, and `Transfer`. Radar behavior is based on both type and category bucket:

| Transaction | Account effect | Radar treatment |
| --- | --- | --- |
| Income | Adds to the account balance through TransactionService | Added to payroll-period income, irrespective of category bucket. |
| Expense / Living | Debits the account | Reduces period Living budget and contributes to `TodaySpent` when dated today. |
| Expense / Responsibility | Debits the account | Excluded from Living expenses; the active FixedCommitment amount is already deducted separately. |
| Expense / Entertainment, Savings, Investment | Debits the account | Excluded from Living budget. |
| Expense / uncategorized | Debits the account | Excluded from Living budget and exposed as `UncategorizedExpenseAmount`. |
| Transfer | Debits source and credits destination | Not income or expense; excluded from Radar income and expense calculations. |

Current enforcement is not uniform:

- The general transaction service accepts a nullable category, and does not enforce a category bucket for an Expense. The current Dashboard Quick Add UI requires a category for Expense, but that is not a backend invariant.
- Goal Purchase requires a category and rejects the Living bucket; it creates an Expense associated with the Goal.
- FixedCommitment payment classification as Responsibility is a documented rule/convention, but the general transaction endpoint does not require that bucket for a commitment payment because there is no transaction-to-commitment relation.
- Income may be categorized or uncategorized; the current income query does not filter by category.

Relevant paths: [TransactionService](../LifeDashboard.Application/Services/TransactionService.cs), [GoalPurchaseService](../LifeDashboard.Application/Services/GoalPurchaseService.cs), and [DashboardPage](../LifeDashboard.Web/src/presentation/pages/DashboardPage.tsx).

## 4. Fixed Commitment Semantics

FixedCommitment is a planning input rather than a payment ledger entry:

- A commitment is active for a payroll period when `StartMonth <= PayrollPeriodMonth` and `EndMonth` is null or `EndMonth >= PayrollPeriodMonth`.
- The full `MonthlyAmount` is deducted once from period income when active, regardless of whether a corresponding payment Transaction exists or its actual payment date.
- FixedCommitment does not automatically create a Transaction or link to one.
- To avoid double deduction, an actual commitment payment should be categorized as an Expense in the Responsibility bucket. Responsibility Expenses are excluded from Living expenses.

This means the Radar is a *planned availability* calculation, not a reconciliation of bank cash or actual bill-payment state. If a commitment is active but unpaid, it is still deducted. If a payment is misclassified as Living, the amount can be deducted both as a FixedCommitment and as a Living Expense. Since backend enforcement is absent, Phase 3 must decide whether to retain this as a user classification rule or introduce an explicit commitment payment association in a later scope.

Relevant paths: [FixedCommitmentRepository](../LifeDashboard.Infrastructure/Repositories/FixedCommitmentRepository.cs) and [FixedCommitmentService](../LifeDashboard.Application/Services/FixedCommitmentService.cs).

## 5. Goal Saving and Purchase Semantics

Goal Saving deposit is a Transfer, not an Expense:

- TransactionService/TransferCore debits the source account and credits the destination account.
- The transfer is associated with the Goal and `Goal.CurrentAmount` increases in the same database transaction.
- Radar filters for Income or Expense, so Goal Saving deposits do not reduce Living budget or count as income.

Goal Purchase is a distinct event:

- It creates an Expense, debits the selected account, and completes a Purchase Goal atomically.
- The selected category must not be in the Living bucket; therefore the purchase Expense does not reduce Living budget under the current Radar rule.
- Goal progress/status is not an expense measure. `CurrentAmount` is progress and remains separate from account cash and Radar spending totals.

A future Goal summary may display progress and completion alongside Radar, but should not add `Goal.CurrentAmount` or Saving transfers to expenses. If a future product decision wants Goal deposits to consume a budget envelope, that must be a new, explicit budgeting rule rather than treating a Transfer as an Expense implicitly.

## 6. Proposed Dashboard Read Models

The current [DashboardPage](../LifeDashboard.Web/src/presentation/pages/DashboardPage.tsx) requests Radar state, account list, and category list separately. It uses account/category data for Quick Add. Current [RadarStateDto](../LifeDashboard.Application/Models/DTOs/RadarStateDto.cs) exposes daily limits, today's spending, remaining days, total living budget, and uncategorized expense amount, but not period context or component breakdowns.

For a future Dashboard, prefer explicit read models composed at the Application/query boundary. Do not make the browser recompute budget rules or merge unrelated money concepts into one balance.

### BudgetRadarSummary

Recommended fields:

- `asOfDate`, `periodStartDate`, `periodEndDate`, `payrollPeriodMonth`
- `periodIncome`, `activeCommitmentTotal`, optionally commitment line items
- `totalLivingBudget`, `periodLivingExpense`, `remainingLivingBudget`
- `dailySafeLimit`, `todaySpent`, `todayRemaining`, `remainingDays`
- `uncategorizedExpenseAmount`, optionally an uncategorized transaction count

This gives the UI context for why the limit has its current value and avoids inferring hidden dates or totals from the existing six-field response.

### CashPositionSummary

Recommended as a separate account read model:

- active account balances and optionally account type/name
- optionally a total current balance, clearly labeled as cash position, not budget remaining

Account soft-deletion behavior should be decided for this read model. The current AccountRepository list is user-scoped but does not filter `IsDeleted`.

### GoalProgressSummary

Recommended as a separate Goal read model:

- active Saving and Purchase goal counts
- target/current progress and completion state, or per-goal summaries if the UI needs them

Do not sum Goal progress into income, expense, or account balance; a Saving deposit is already represented by a Transfer and a Purchase is represented by its Expense.

### Optional SpendingBreakdown

If Dashboard needs charts or comparisons, expose a period/category aggregate with category bucket, amount, and period boundaries. Keep Responsibility separate from Living so a commitment payment is not visually conflated with discretionary Living spending. Do not return all transactions merely to calculate these aggregates in the frontend.

A future Dashboard overview endpoint may compose these read models, but the budgeting, account-position, and Goal-progress semantics should remain independently named and testable.

## 7. Phase 3 Decisions and Acceptance Criteria

Resolve these decisions before expanding the current Radar contract:

1. **Allocation ratio:** `SystemSetting` validates and stores a 100% allocation split, including `Living`, but current `BudgetRadarService` uses all period income minus commitments and does not multiply by the Living allocation. `CoreTransactionBudgetRadarSpec.md` v1.4 also states `income - fixed commitments`. Confirm whether Phase 3 preserves that formula or revises the domain definition; do not silently apply the ratio.
2. **Commitment payment classification:** decide whether Responsibility bucket remains a user convention or whether the product needs a direct association between an actual Expense and a FixedCommitment. The current model cannot prove a particular commitment was paid.
3. **Account balance semantics:** decide whether account creation should initialize `CurrentBalance` from `InitialBalance` before presenting a cash-position summary. The current Account constructor stores `InitialBalance` but does not initialize `CurrentBalance` from it.
4. **Read model scope:** decide whether the first Dashboard read model is only an expanded BudgetRadarSummary or a composed overview containing separate budget, account, and Goal sections.
5. **Uncategorized visibility:** the current Radar returns only an amount. A count or itemized list requires an explicit query/API contract and should not be inferred from that amount.

Acceptance tests for the budget domain should cover payday boundary and short-month clamping; inclusive date filtering; user isolation; FixedCommitment start/end month inclusion; Living versus Responsibility/other bucket treatment; uncategorized Expenses reported but not deducted; negative remaining budget; and the invariant that transfers/Goal Saving deposits do not count as expenses. Read-model tests should assert that period/component fields agree with the same calculation used for daily limits.
