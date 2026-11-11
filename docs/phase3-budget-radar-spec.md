# Phase 3 Budget Radar Implementation Specification

Status: Proposed backend implementation specification. Based on [phase3-budget-radar-design.md](phase3-budget-radar-design.md) and [CoreTransactionBudgetRadarSpec.md](CoreTransactionBudgetRadarSpec.md) v1.4.

## 1. Scope and Constraints

Phase 3 defines the backend Budget Radar domain calculation and its internal read model.

In scope:

- Define an immutable `BudgetSnapshot` domain value object.
- Define how period income, Living expenses, Goal Purchases, uncategorized expenses, and active FixedCommitments contribute to the snapshot.
- Keep all calculations in backend code and all queries user-scoped.
- Map the result to the existing `RadarStateDto` and preserve the current HTTP/API contract.

Out of scope:

- New endpoints or changes to existing endpoint/request/response contracts.
- Frontend or Dashboard UI work.
- Persisting snapshots, adding tables, or creating migrations.
- Account balance changes or transaction creation behavior.
- Actual payment reconciliation between a FixedCommitment and a Transaction.

## 2. Domain Model Decision: BudgetSnapshot

`BudgetSnapshot` is an immutable, non-persisted Domain value object representing one user's calculated budget for one `AsOfDate`. It is not an Entity: it has no database identity or lifecycle and must not be stored in EF Core.

The Application layer gathers user-scoped facts and the payroll period, then creates the snapshot through one domain calculation/factory path. Derived values must not be independently assigned by callers.

### Required facts and derived values

| Property | Meaning |
| --- | --- |
| `AsOfDate` | Date requested for the calculation. |
| `PeriodStartDate`, `PeriodEndDate` | Inclusive current payroll-period boundaries. |
| `PayrollPeriodMonth` | First day of the period start month; used for FixedCommitment eligibility. |
| `PeriodIncome` | Sum of Income Transactions in the period. |
| `FixedCommitmentTotal` | Sum of active monthly commitments for `PayrollPeriodMonth`. |
| `TotalLivingBudget` | `PeriodIncome - FixedCommitmentTotal`. |
| `PeriodLivingExpense` | Sum of qualifying Living Expenses in the period. |
| `RemainingLivingBudget` | `TotalLivingBudget - PeriodLivingExpense`. |
| `UncategorizedExpenseTotal` | Period Expense total with no Category; reported separately and not subtracted. |
| `GoalPurchaseExpenseTotal`, `GoalPurchaseCount` | Separate period metrics for Goal-linked Purchase Expenses; not subtracted from Living budget. |
| `RemainingDays` | Inclusive calendar days from `AsOfDate` through `PeriodEndDate`. |
| `DailySafeLimit` | `RemainingLivingBudget / RemainingDays`, rounded to two decimals away from zero. |
| `TodayLivingExpense` | Today's qualifying Living Expense total. |
| `TodayRemaining` | `DailySafeLimit - TodayLivingExpense`. |

Amounts use `decimal`. Preserve negative `TotalLivingBudget`, `RemainingLivingBudget`, `DailySafeLimit`, and `TodayRemaining`; do not clamp to zero. Apply the specified two-decimal rounding to `DailySafeLimit`; do not round source totals before aggregation.

`BudgetSnapshot` may carry the Goal Purchase metrics as independent domain measures. The existing API does not expose them; mapping to the existing `RadarStateDto` must not add response fields in this phase.

## 3. Payroll Period and Data Selection

`AsOfDate` is supplied by the existing `currentDate` query. Use `SystemSetting.Payday` to derive the period:

- The most recent payday is the inclusive start date.
- The day before the next month's payday is the inclusive end date.
- A payday beyond a month's length is clamped to that month's last day.
- `PayrollPeriodMonth` is `DateOnly(periodStartDate.Year, periodStartDate.Month, 1)`.
- `RemainingDays = PeriodEndDate.DayNumber - AsOfDate.DayNumber + 1`.

All fact queries must be scoped by the authenticated `userId` and use inclusive transaction date boundaries. Do not query account balances to derive budget values, and do not load all transactions into memory to aggregate them.

## 4. Transaction Classification Strategy

Classification is determined by `Transaction.Type`, Goal association where present, and Category bucket. Apply the following precedence:

| Transaction condition | Snapshot treatment |
| --- | --- |
| `Type = Transfer` | Exclude from income, expenses, Goal Purchase totals, and uncategorized Expense totals. Transfers only move account funds. |
| `Type = Income` | Add to `PeriodIncome`, regardless of whether Category is null or which bucket it uses. |
| `Type = Expense`, linked to a Purchase Goal | Add to `GoalPurchaseExpenseTotal` and `GoalPurchaseCount`; do not add to `PeriodLivingExpense` or `TodayLivingExpense`. |
| `Type = Expense`, no Category | Add to `UncategorizedExpenseTotal`; do not subtract from the Living budget. |
| `Type = Expense`, `Category.BucketType = Living` | Add to period Living expense and, when dated `AsOfDate`, to `TodayLivingExpense`. |
| `Type = Expense`, `BucketType = Responsibility` | Exclude from Living expenses; the active FixedCommitment has already been reserved from period income. |
| `Type = Expense`, other bucket (`Entertainment`, `Savings`, `Investment`) | Exclude from Living expenses. |

A Goal Purchase remains a real Expense and reduces its selected Account balance through the existing transaction flow. Its separate BudgetSnapshot metric is informational; it must not also reduce the Living allowance. Goal Saving deposits are Transfers and therefore are neither income nor expenses. `Goal.CurrentAmount` is progress, not a budget transaction total.

For consistency, a Goal Purchase is identified using its associated Goal (`GoalId` and `GoalType = Purchase`), not inferred from its note or category name. If historical data has a missing Goal association, classify by the normal Expense/category rules and do not guess that it was a Goal Purchase.

## 5. FixedCommitment Policy

A FixedCommitment is a planned payroll-period reserve, not a payment transaction.

It is active for a snapshot when:

```text
StartMonth <= PayrollPeriodMonth
and (EndMonth is null or EndMonth >= PayrollPeriodMonth)
```

Deduct its full `MonthlyAmount` once from `PeriodIncome` for that payroll period. Do not multiply or prorate it by the number of days in the period. Do not check whether a payment Transaction exists; an active commitment is reserved even if unpaid.

To prevent a normal recorded payment from being deducted a second time as Living spending, record it as an Expense in the `Responsibility` bucket. Responsibility Expenses do not reduce `PeriodLivingExpense`.

Phase 3 does not associate a Transaction with a specific FixedCommitment and does not infer a payment from matching amount, date, name, or account. Therefore this policy depends on correct classification. A payment incorrectly categorized as Living will be counted as Living and the active planned commitment will also be deducted; this is a known limitation, not an amount-matching heuristic to add in Phase 3.

## 6. AllocationRatio Decision

`AllocationRatio` does **not** participate in Phase 3 BudgetSnapshot calculations.

Use:

```text
TotalLivingBudget = PeriodIncome - FixedCommitmentTotal
```

Do not apply `AllocationRatio.Living` to income, and do not subtract the other allocation buckets. This preserves the explicit v1.4 Budget Radar formula and current service behavior. AllocationRatio remains a separate preference/configuration model until a product decision defines how percentages interact with fixed commitments, transfers, and actual savings/investment transactions.

If the product later decides that only the Living percentage is available to spend, that is a formula change requiring a revised specification and new acceptance examples; it must not be introduced implicitly while implementing this phase.

## 7. Application and Query Responsibilities

- **BudgetRadarService / Application use case:** resolve the user's SystemSetting, derive the payroll period, request aggregates, create `BudgetSnapshot`, and map it to the existing `RadarStateDto`.
- **Domain:** own immutable snapshot invariants and arithmetic/rounding rules; have no dependency on EF Core, repositories, HTTP, or UI DTOs.
- **Repositories:** perform user-scoped aggregate queries for period income, Living Expenses, uncategorized Expenses, Goal Purchase Expenses, and active commitments. Return aggregate facts; do not calculate daily limits or remaining budget.
- **Controller/API:** remain unchanged in this phase. Continue requiring `currentDate`, obtaining `userId` from JWT claims, and returning the current response shape.

Use aggregate queries in the database. Goal Purchase aggregation may require a filtered join to Goal; it must remain user-scoped and period-scoped. The sum/count should describe Expense Transactions associated with Purchase Goals only.

## 8. Existing API Compatibility

Do not add an endpoint or fields to `RadarStateDto` in Phase 3. Preserve the current endpoint behavior and existing fields:

- `DailySafeLimit`
- `TodaySpent`
- `TodayRemaining`
- `RemainingDays`
- `TotalLivingBudget`
- `UncategorizedExpenseAmount`

The internal `BudgetSnapshot` can contain period and component values not exposed in this response. Any later decision to expose these values requires a separate API contract review.

## 9. Acceptance Criteria

1. Payroll payday boundary is inclusive; invalid month days clamp to month end; the preceding period ends the day before the next payday.
2. All transaction aggregates filter by authenticated user, type, and inclusive date range.
3. Income includes categorized and uncategorized Income Transactions.
4. Only Living-bucket Expenses reduce the Living budget and contribute to `TodayLivingExpense`.
5. Uncategorized Expenses are reported separately and do not reduce the Living budget.
6. Responsibility Expenses do not reduce Living budget; active commitments are deducted exactly once by payroll-period eligibility.
7. Transfer and Goal Saving deposits do not count as income or expense.
8. Goal Purchase Expense amount/count are available in `BudgetSnapshot` independently and do not reduce Living budget or alter `Goal.CurrentAmount`.
9. Negative budget and daily values are preserved; daily limit rounding uses two decimal places, away from zero.
10. Account.CurrentBalance is not used as a substitute for period income, expense, or budget remaining.
11. Existing API response contract remains unchanged.

## 10. Product Decisions Required

The implementation defaults below are recommendations; product approval is needed before changing them:

| Decision | Recommended default | Product question |
| --- | --- | --- |
| AllocationRatio | Do not apply it in Phase 3; preserve `income - commitments`. | Is Living percentage intended to restrict the budget, or are allocation percentages advisory only? If applied later, should fixed commitments be deducted before or after the percentage? |
| FixedCommitment payment tracking | Keep commitments as planned reserves; actual payments use Responsibility category and are not linked to a commitment. | Does the product need paid/unpaid status or reconciliation to a specific transaction in a later phase? |
| Goal Purchase statistics | Track amount/count separately in the internal snapshot, with no effect on Living budget. | Should Goal Purchase totals be exposed or displayed in a later Dashboard/API phase? |
| Uncategorized Expenses | Continue reporting separately without subtracting from available Living budget. | Should unclassified expenses remain outside the budget, or should the product reserve them conservatively until categorized? |

Until these decisions are approved otherwise, implement the recommended defaults and preserve the current API contract.
