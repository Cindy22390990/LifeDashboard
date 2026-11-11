# Phase 3C Budget Radar Application Query Contract Specification

Status: Proposed Application/query contract. Derived from [Phase 3A Budget Radar Specification](phase3-budget-radar-spec.md) and [Phase 3B Read Model Specification](phase3-budget-radar-read-model-spec.md). This document defines internal boundaries only; it does not change code or authorize API, UI, or database-schema work.

## 1. Scope and Non-Goals

This contract defines:

- The Budget Radar Application service boundary.
- Query interfaces and result semantics.
- Repository versus Application/Domain responsibilities.
- The Goal Purchase aggregate extension point.
- The FixedCommitment detail projection boundary.

It does not define an HTTP controller, endpoint, API response expansion, frontend/UI, EF schema, or migration. The existing `RadarStateDto` and its HTTP mapping remain unchanged.

## 2. Application Service Boundary

### Existing public Application boundary

Keep the current Application use case as the controller-facing boundary:

```csharp
public interface IBudgetRadarService
{
    Task<RadarStateDto> GetRadarStateAsync(Guid userId, DateOnly currentDate);
}
```

The contract remains stable. `userId` is obtained by the existing caller from authenticated claims; clients must not supply a user id as a query input. `currentDate` is the existing date input.

`BudgetRadarService` is the sole orchestrator for this query. It:

1. Reads the user's SystemSetting and payday.
2. Derives inclusive payroll-period dates and `PayrollPeriodMonth`.
3. Calls user-scoped repository queries for aggregate facts and active commitments.
4. Creates `BudgetSnapshot` through its calculation factory.
5. Optionally builds an internal `BudgetRadarReadModel` for Application consumers that need details.
6. Maps the Snapshot to the existing `RadarStateDto` without adding fields.

Do not add a second public service interface solely to wrap the current method. If an internal query result is needed, keep its composition in Application and keep the existing service/API-facing return type intact. `BudgetSnapshot` performs arithmetic and invariants only; it must not resolve repositories, settings, or dates itself.

### Internal read result

When an Application consumer needs component details, use an internal, immutable result with the following conceptual shape:

```csharp
public sealed record BudgetRadarReadModel(
    BudgetSnapshot Snapshot,
    IReadOnlyList<FixedCommitmentDetailReadModel> ActiveCommitments);
```

This is not an API DTO. The summary totals and daily figures come only from `Snapshot`; consumers must not recompute or sum them again. If no consumer needs commitment details, do not introduce a wrapper/result type just to duplicate the current `RadarStateDto` path.

## 3. Query Contracts

Repository interfaces live in the Application layer and expose domain-specific facts, not EF Core queryables or tracked database objects.

### Existing Transaction query contracts

Retain the current contracts for:

```csharp
Task<decimal> GetTotalAmountAsync(
    Guid userId,
    DateOnly startDate,
    DateOnly endDate,
    TransactionType type,
    BucketType? bucketType = null);

Task<decimal> GetUncategorizedExpenseTotalAsync(
    Guid userId,
    DateOnly startDate,
    DateOnly endDate);
```

Usage:

- `Income` over the inclusive payroll-period dates, with no bucket filter.
- `Expense` plus `BucketType.Living` over the inclusive payroll-period dates.
- `Expense` plus `BucketType.Living` for `currentDate` through `currentDate`.
- Uncategorized `Expense` over the inclusive payroll-period dates.

These methods return aggregate facts. They do not calculate payroll periods, commitment totals, remaining budget, or daily limits.

### Goal Purchase aggregate extension point

Add one narrow Transaction repository query contract when implementing the Goal Purchase extension:

```csharp
Task<GoalPurchaseAggregate> GetGoalPurchaseAggregateAsync(
    Guid userId,
    DateOnly startDate,
    DateOnly endDate);
```

`GoalPurchaseAggregate` is an immutable Application query-result record with:

```text
ExpenseTotal: decimal
TransactionCount: int
```

Contract semantics:

- Select Expense Transactions within the inclusive period.
- Require `Transaction.UserId = userId`.
- Require a linked Goal with `Goal.UserId = userId` and `Goal.GoalType = Purchase`.
- Return only sum and transaction-row count; do not materialize matching entities.
- A successful query with no rows returns `ExpenseTotal = 0` and `TransactionCount = 0`.
- The query result type represents a completed query. “Not queried/unavailable” is represented by the absence of this result at the Application/Snapshot boundary, not by zero values.
- Count matching transaction rows, not distinct Goal ids.

The Application layer passes both aggregate values together to `BudgetSnapshot`; it must not infer purchase statistics from notes, account balances, categories, or `Goal.CurrentAmount`.

The repository query joins the Goal relationship in its query implementation. Do not fetch Goal lists and Transaction lists separately and perform an in-memory join. Apply both user constraints to the joined data even if the relationship normally guarantees matching ownership.

### FixedCommitment query contract

Reuse the existing contract:

```csharp
Task<IEnumerable<FixedCommitment>> GetActiveForPayrollPeriodAsync(
    Guid userId,
    DateOnly payrollPeriodMonth);
```

Input `payrollPeriodMonth` is the first day of the payroll-period start month. The repository applies:

```text
UserId = userId
StartMonth <= payrollPeriodMonth
EndMonth is null OR EndMonth >= payrollPeriodMonth
```

The returned set is the complete active set for that period. It is the only input to `FixedCommitmentTotal`; do not query all commitments and filter them in the Application layer.

## 4. Repository Responsibilities

### TransactionRepository

- Execute user/date/type/category predicates and sums/counts in the database.
- Implement the narrow Goal Purchase aggregate as a filtered Transaction-to-Goal join.
- Return zero totals/counts for successful empty aggregate queries.
- Do not calculate payroll periods, budget formulas, or Goal state transitions.
- Do not widen `GetAllAsync` or use it for Budget Radar aggregation.

### FixedCommitmentRepository

- Select active commitments using user id and payroll-period month.
- Return the active records required for calculation and optional Application projection.
- Do not subtract commitment totals from income; that formula belongs to the calculation boundary.

### GoalRepository

- Remains responsible for Goal CRUD and user-scoped Goal retrieval.
- Is not used to load all Goals for Goal Purchase aggregation. The Transaction aggregate query joins only matching Goals.

### Setting query/service

- Resolve the authenticated user's payday as today.
- AllocationRatio remains excluded from the Phase 3 budget formula.

## 5. FixedCommitment Detail Projection Boundary

`FixedCommitmentDetailReadModel` belongs to the Application read/query layer, not Domain `BudgetSnapshot` and not Infrastructure. It is an immutable projection of the active commitments returned by the existing repository query.

Recommended fields:

```text
Id
Name
CommitmentType
MonthlyAmount
StartMonth
EndMonth
```

No `IsActiveForPayrollPeriod` field is needed when the collection is already filtered to active commitments. Do not include Transaction payment status or attempt to match payments to commitments.

The Application orchestration derives `FixedCommitmentTotal` from the exact active collection used for the projection. `BudgetSnapshot.FixedCommitmentTotal` is authoritative; a read-model consumer must not recalculate it. If itemized details are not requested by an Application consumer, omit the projection rather than add another repository query.

## 6. BudgetSnapshot Input Contract

The Application calculation path supplies these facts to `BudgetSnapshot.Calculate`:

- `asOfDate`, `periodStartDate`, `periodEndDate`
- `periodIncome`
- `fixedCommitmentTotal`
- `periodLivingExpense`
- `uncategorizedExpenseTotal`
- `todayLivingExpense`
- Optional Goal Purchase aggregate amount/count, supplied together only after the narrow aggregate query completes

The Domain calculation remains authoritative for:

```text
PayrollPeriodMonth = first day of PeriodStartDate month
TotalLivingBudget = PeriodIncome - FixedCommitmentTotal
RemainingLivingBudget = TotalLivingBudget - PeriodLivingExpense
RemainingDays = inclusive days from AsOfDate through PeriodEndDate
DailySafeLimit = round(RemainingLivingBudget / RemainingDays, 2, AwayFromZero)
TodayRemaining = DailySafeLimit - TodayLivingExpense
```

Uncategorized Expense and Goal Purchase values remain independent metrics. Neither is implicitly subtracted from Living budget. Negative totals remain negative. `AllocationRatio` is not supplied to this calculation.

## 7. Query Orchestration and Mapping

The intended Application sequence is:

```text
IBudgetRadarService.GetRadarStateAsync(userId, currentDate)
  -> resolve SystemSetting.Payday
  -> derive payroll period
  -> query Income, period Living Expense, today's Living Expense,
     Uncategorized Expense, and active FixedCommitments
  -> query GoalPurchaseAggregate when that extension is implemented
  -> calculate immutable BudgetSnapshot
  -> optionally project active FixedCommitment details
  -> map only existing fields to RadarStateDto
```

Until the Goal Purchase aggregate query is implemented, leave both Goal Purchase Snapshot values unavailable. Do not report zero as though the query ran and found no matches.

Mapping remains limited to the existing API fields: `DailySafeLimit`, `TodaySpent`, `TodayRemaining`, `RemainingDays`, `TotalLivingBudget`, and `UncategorizedExpenseAmount`. The internal query contract may be richer without changing the HTTP response.

## 8. Failure and Data Semantics

- Missing SystemSetting retains the current application behavior.
- Repository query predicates remain explicitly user-scoped; no Global Query Filter is introduced by this contract.
- Date windows are inclusive and use `DateOnly` values.
- FixedCommitments reserve the full monthly amount once for the payroll period, whether or not payment occurred.
- Responsibility Expenses stay excluded from Living expenses under the current classification policy.
- Goal Purchase Expenses are counted in their separate aggregate and excluded from Living expense totals.
- Transfers, including Goal Saving deposits, are excluded from all income/expense aggregates.
- No account-balance read is used to calculate BudgetSnapshot.
- No raw transaction list is returned from a Budget Radar query contract.

## 9. Contract Acceptance Criteria

1. `IBudgetRadarService` remains the single current Application use-case boundary and returns the existing `RadarStateDto`.
2. Repository methods accept explicit `userId` and period dates/month; no repository returns `IQueryable` to Application consumers.
3. Goal Purchase amount/count are sourced only by the narrow Transaction-to-Goal aggregate query and remain unavailable until queried.
4. A successful empty Goal Purchase aggregate is `(0, 0)`; it is not confused with an unexecuted query.
5. FixedCommitment selection is performed in the repository; detail projection is performed in Application.
6. `BudgetSnapshot` is created from aggregate facts and owns budget arithmetic; consumers do not duplicate formulas.
7. Existing API response fields and behavior are preserved; no Controller or UI contract is introduced here.
8. No migration or database-schema change is required by these contracts.

## 10. Open Product Decisions

- Should the Goal Purchase count mean transaction rows or distinct Goals? Recommended: transaction rows.
- If a Goal's type changes after linked Transactions exist, should prior rows remain Purchase-classified? The query contract currently uses the Goal's current type.
- Does an internal consumer require active FixedCommitment details, or is the aggregate total sufficient? Recommended: add the projection only when needed.
- Should any of these internal read models be exposed through an API in a later phase? This requires a separate API contract decision.
