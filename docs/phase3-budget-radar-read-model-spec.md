# Phase 3B Budget Radar Read Model Specification

Status: Proposed internal read-model design following Phase 3A. This specification does not authorize implementation changes to the API contract, Dashboard UI, or database schema.

## 1. Scope

Phase 3B defines the backend read-model boundaries needed to supply budget facts and breakdowns to current or future consumers.

In scope:

- Define the Budget Radar summary and supporting internal read models.
- Decide whether `BudgetSnapshot` needs to grow.
- Define how Goal Purchase aggregates enter the calculation.
- Define whether active FixedCommitment details need a separate model.
- Define the scope of a narrow Transaction repository query.

Out of scope:

- UI layout, visual presentation, or frontend calculations.
- New or changed HTTP endpoints, query parameters, or response DTO fields.
- Database migrations or persisted snapshots.
- Reconciliation between a FixedCommitment and an actual payment Transaction.

## 2. Current State After Phase 3A

Phase 3A introduced an immutable Domain `BudgetSnapshot`. It owns payroll-period component totals and derived calculations. Goal Purchase amount/count are nullable; they are null because no Goal Purchase aggregate query is currently available.

`BudgetRadarService` currently obtains period income, Living expenses, today's Living expenses, uncategorized Expense total, and active FixedCommitments, then creates a Snapshot and maps it to the existing `RadarStateDto`.

The existing Dashboard page separately requests Radar state, account data, and category data. The current Radar response contains daily safe limit, today's spent/remaining, remaining days, total living budget, and uncategorized expense amount. The Account and Category requests currently support Quick Add; they are not inputs to `BudgetSnapshot`.

`ITransactionRepository` has user/date/type/category aggregate queries and an uncategorized-expense query. Its `GetAllAsync(userId)` returns transaction entities and is not an acceptable substitute for a period Goal Purchase aggregate. `IFixedCommitmentRepository.GetActiveForPayrollPeriodAsync` already returns the active commitments needed for both the total and optional details.

## 3. Read Model Decisions

### 3.1 BudgetSnapshot

**Decision: Keep the Domain value object focused on budget facts and calculations. Do not put persistence entities, transaction rows, or display-oriented data in it.**

Its existing values are sufficient for the calculation:

- period boundaries and payroll month
- period income and fixed commitment total
- total/remaining Living budget and period Living expenses
- uncategorized expense total
- Goal Purchase amount/count, when that aggregate is available
- remaining days, daily safe limit, today's Living expense, and today's remaining amount

Do not add Category names/icons, Account details, FixedCommitment names, transaction descriptions, or UI status labels to `BudgetSnapshot`.

Goal Purchase metrics remain unavailable together until a repository aggregate has been requested. After a successful aggregate query with no matching rows, amount/count are both zero. Do not use zero to mean “not queried.” Keep the existing invariant that amount and count are either both present or both unavailable.

### 3.2 BudgetRadarReadModel

**Decision: Add an Application-level internal composition model only when a consumer needs component breakdowns; it is not an API DTO.**

Recommended shape:

```text
BudgetRadarReadModel
  Snapshot: BudgetSnapshot
  ActiveCommitments: IReadOnlyList<FixedCommitmentDetailReadModel>
```

The summary values in `Snapshot` remain the single source for totals and daily calculations. Commitment details explain the commitment component; they must not be summed again by callers. Additional transaction breakdowns should be separately named aggregate facts rather than raw transactions.

If Phase 3B has no consumer for commitment details, keep the current service return path and do not add a wrapper model speculatively. The existing `RadarStateDto` mapping remains unchanged in either case.

### 3.3 FixedCommitmentDetailReadModel

**Decision: Use a separate immutable Application read model if active commitments need to be explained individually. Do not extend `BudgetSnapshot` with FixedCommitment entities.**

Recommended fields:

- `Id`
- `Name`
- `CommitmentType`
- `MonthlyAmount`
- `StartMonth`
- `EndMonth`
- `IsActiveForPayrollPeriod` (optional if the model only contains already-filtered active rows)

For the current Budget Radar calculation, only the active rows and their total are required. The existing repository returns full commitment entities, so this projection needs no additional repository query. Never include inactive rows in `ActiveCommitments` or in `FixedCommitmentTotal`.

The detail model is explanatory; the sum is calculated once in the Application calculation path and represented by `BudgetSnapshot.FixedCommitmentTotal`.

## 4. GoalPurchase Aggregate Integration

**Decision: Add one narrow, period-scoped aggregate query to the Transaction repository contract when Phase 3B is allowed to modify the Infrastructure query implementation. Do not call `GetAllAsync` and filter in memory.**

Recommended aggregate:

```text
GoalPurchaseAggregate
  ExpenseTotal: decimal
  TransactionCount: int
```

Recommended repository contract semantics:

```text
GetGoalPurchaseAggregateAsync(userId, startDate, endDate)
```

The aggregate filters:

- `Transaction.UserId = userId`
- `Transaction.Type = Expense`
- `Transaction.TransactionDate >= startDate`
- `Transaction.TransactionDate <= endDate`
- Transaction has an associated Goal
- associated Goal belongs to the same user
- associated Goal has `GoalType = Purchase`

The query returns only the amount sum and transaction-row count; it must not materialize all matching Transaction or Goal entities. A completed query with no matches returns `(0, 0)`. The calculation service passes both values to `BudgetSnapshot`; before the query is available or executed, both Snapshot Goal Purchase fields remain unavailable/null.

### Classification and double-counting

- Goal Purchase remains a real Expense and affects Account balance through the existing transaction flow.
- It is counted separately as `GoalPurchaseAggregate`.
- It does not reduce `PeriodLivingExpense` or `TodayLivingExpense`, even if historical data has an unexpected Living category. Goal association takes precedence for the separate purchase statistic and excludes that row from Living budget measures.
- It is not income, uncategorized Expense, or a Transfer.
- Goal Saving deposits are Transfers and are never included in the Goal Purchase aggregate.
- `Goal.CurrentAmount` is not used to compute Purchase spend.

The count is the number of matching Expense Transaction rows, not the number of distinct Goals. A Purchase Goal may have multiple linked Expense rows; each row contributes once to the total and count.

## 5. Narrow Transaction Repository Strategy

The existing aggregate methods remain the preferred source for period income, Living expense, today's Living expense, and uncategorized expense. Add a narrow Goal Purchase aggregate only for the missing amount/count fact.

The read path should be:

```text
BudgetRadarService
  -> existing income / Living / uncategorized aggregates
  -> existing active FixedCommitment query
  -> narrow GoalPurchase aggregate query
  -> BudgetSnapshot.Calculate(...)
  -> existing RadarStateDto mapping
```

All queries must include the authenticated `userId` and explicit period boundaries. The Goal join must be scoped on both the Transaction user and Goal user to prevent cross-user data from entering the aggregate. Keep sums/counts database-side. Do not widen `GetAllAsync`, add eager loading to the general transaction list, or add Goal Purchase-specific behavior to the general list query.

The repository aggregate result belongs to the Application query contract (for example, an immutable aggregate record); it must not expose EF Core query types or tracked entities. Repository code performs selection and aggregation only; payroll period, budget formula, classification precedence, and daily calculations remain in Application/Domain.

## 6. Existing API Contract Boundary

This read-model specification does not change `RadarStateDto` or add an API endpoint. The existing controller continues returning the current response shape.

If a later phase authorizes exposing the expanded summary or commitment details, that work must separately define:

- API versioning/compatibility expectations
- nullable versus zero semantics
- date and amount serialization
- user-scoped access behavior

Until then, `BudgetRadarReadModel`, `FixedCommitmentDetailReadModel`, and `GoalPurchaseAggregate` are internal backend query/application types only.

## 7. Data Semantics Preserved

- `TotalLivingBudget = PeriodIncome - FixedCommitmentTotal`.
- `RemainingLivingBudget = TotalLivingBudget - PeriodLivingExpense`.
- `AllocationRatio` is not applied.
- Active FixedCommitments reserve the full monthly amount once for the payroll period, whether or not payment occurred.
- Responsibility Expenses remain outside Living expense totals to avoid double deduction under the documented classification policy.
- Uncategorized Expenses are reported separately and do not reduce Living budget.
- Transfers and Goal Saving deposits do not count as income or expense.
- Negative totals and daily amounts remain negative; do not clamp.
- Account balances remain a separate cash-position read model and are not an input to BudgetSnapshot.

## 8. Acceptance Criteria

1. Existing Dashboard/Budget Radar consumers and the current API response shape remain unchanged by this specification.
2. BudgetSnapshot remains immutable and contains only budget-relevant facts and derived values.
3. Goal Purchase aggregate is nullable/unavailable before query execution and `(0, 0)` after a successful query with no matches.
4. Goal Purchase aggregation is period- and user-scoped, counts transaction rows, and executes in the database.
5. Goal Purchase amounts/count do not change Living budget, daily limit, or Goal.CurrentAmount.
6. FixedCommitment details, when included, are already filtered to the snapshot payroll period and do not cause the total to be recomputed by consumers.
7. No query loads all transactions for in-memory Goal Purchase aggregation.
8. No API, UI, database schema, or migration changes are required to implement the internal read-model boundary.

## 9. Product Decisions Required

| Decision | Recommended default | Product question |
| --- | --- | --- |
| Commitment breakdown | Keep details internal and include only if a backend consumer needs to explain the total. | Does a consumer require per-commitment name/type/amount in the same read operation, or is the total sufficient? |
| Goal Purchase count | Count matching Expense transaction rows, not distinct Goals. | Should the metric represent purchase transactions or completed Purchase Goals? |
| Goal type changes | Query using the associated Goal's current `GoalType = Purchase`, matching the Phase 3A classification rule. | If a Goal's type can be changed after linked transactions exist, should prior transactions retain their original classification? |
| API exposure | Keep all new read models internal; preserve the current `RadarStateDto`. | Is a future API contract revision planned to expose period/component/Goal Purchase details? |
| FixedCommitment payment reconciliation | Keep it out of Phase 3B; continue the Responsibility category policy. | Is paid/unpaid state or direct Transaction-to-commitment reconciliation required in a later product phase? |

## 10. Recommended Phase 3B Work Sequence

1. Add the immutable Goal Purchase aggregate result and narrow repository query contract.
2. Implement its database-side user/date/type/Goal filters in Infrastructure when that scope is approved.
3. Feed the aggregate to BudgetSnapshot while preserving unavailable/null semantics when not queried.
4. Add `FixedCommitmentDetailReadModel` only if a backend consumer needs itemized explanation; otherwise keep the total-only calculation path.
5. Preserve current API mapping and verify user isolation, payroll boundaries, aggregate classification, and formula invariants.
