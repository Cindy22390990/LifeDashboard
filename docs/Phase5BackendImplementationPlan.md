# Phase 5 Backend Implementation Plan

Status: **READY FOR IMPLEMENTATION**

The Phase 5 documentation is approved. Implementation must remain within the boundaries in the approved ADRs and API contract. The `TotalAssets` system-account scope is resolved by [Phase5TotalAssetsScopeADR.md](./Phase5TotalAssetsScopeADR.md).

This plan is based on:

- [Phase5ScopeFreezeADR.md](./Phase5ScopeFreezeADR.md)
- [Phase5ImplementationDecision.md](./Phase5ImplementationDecision.md)
- [Phase5ApiContractDecisionADR.md](./Phase5ApiContractDecisionADR.md)
- [Phase5TestCompatibilityDecisionADR.md](./Phase5TestCompatibilityDecisionADR.md)
- [Phase5TotalAssetsScopeADR.md](./Phase5TotalAssetsScopeADR.md)
- [Phase5DashboardApiContract.md](./Phase5DashboardApiContract.md)

The canonical API contract is [Phase5DashboardApiContract.md](./Phase5DashboardApiContract.md). The lowercase duplicate `phase5-dashboard-api-contract.md` is empty and is not the canonical contract; do not delete it as part of this plan.

## Implementation Constraints

- No Domain or Entity changes.
- No database schema changes or migrations.
- No Transaction create/delete or other Transaction write-flow changes.
- No Account balance logic changes.
- No FixedCommitment domain changes, formula changes, or payment tracking.
- No new API routes, Repository Interfaces, or repository/query methods.
- Existing business behavior tests remain frozen.
- DTO shape assertions may be updated only as narrowly permitted by `Phase5TestCompatibilityDecisionADR.md`.

---

## 1. Architecture Mapping

### Dashboard

| Component | Current state |
|---|---|
| [DashboardController](../LifeDashboard.Api/Controllers/DashboardController.cs) | Existing authorized `GET /api/dashboard/summary`. Requires `currentDate` in `yyyy-MM-dd` format. Calls `IDashboardService.GetSummaryAsync` and maps the read model to the response DTO. |
| [DashboardService](../LifeDashboard.Application/Services/DashboardService.cs) | Existing aggregation service. Currently composes `IBudgetRadarService.GetRadarStateAsync`, `IAccountService.GetAllAccountsAsync`, and `IGoalService.GetAllAsync`. It does not currently request monthly totals or recent transactions. |
| [DashboardSummaryDto](../LifeDashboard.Application/Models/DTOs/DashboardSummaryDto.cs) | Existing response DTO has exactly `Budget`, `Accounts`, and `Goals`. |
| [DashboardSummaryReadModel](../LifeDashboard.Application/Models/ReadModels/DashboardSummaryReadModel.cs) | Existing application read model with `RadarStateDto Budget`, `IReadOnlyList<AccountDto> Accounts`, and `IReadOnlyList<GoalDto> Goals`. |

### Budget Radar

| Component | Current state |
|---|---|
| [BudgetRadarController](../LifeDashboard.Api/Controllers/BudgetRadarController.cs) | Existing authorized `GET /api/budget/radar`. Requires `currentDate`, calls `IBudgetRadarService.GetRadarStateAsync`, and returns `RadarStateDto`. No route change is required. |
| [BudgetRadarService](../LifeDashboard.Application/Services/BudgetRadarService.cs) | Uses the existing setting's Payday to select the payroll period. It obtains income, expense summary, goal-purchase aggregate, and active commitments through existing services/repositories, then passes them to the assembler. |
| [BudgetRadarAssembler](../LifeDashboard.Application/Services/BudgetRadarAssembler.cs) | Calls `BudgetSnapshot.Calculate` and maps selected snapshot values to `RadarStateDto`. It currently does not map `RemainingLivingBudget`. |
| [RadarStateDto](../LifeDashboard.Application/Models/DTOs/RadarStateDto.cs) | Existing properties are `DailySafeLimit`, `TodaySpent`, `TodayRemaining`, `RemainingDays`, `TotalLivingBudget`, and `UncategorizedExpenseAmount`. It does not currently have `RemainingLivingBudget`. |

### Existing reusable data sources

- `IReportRepository.GetMonthlyTotalsAsync(userId, startDate, endDateExclusive)` supplies calendar-date-range income and expense totals. Its normal EF query is subject to the Transaction soft-delete filter.
- `ITransactionService.GetAllTransactionsAsync(userId)` returns `TransactionDto` items using `ITransactionRepository.GetAllAsync`, which orders by `TransactionDate` descending. The normal query is subject to the Transaction soft-delete filter.
- `IAccountService.GetAllAccountsAsync(userId)` returns `AccountDto` values with `CurrentBalance`. The existing account repository excludes soft-deleted accounts but does not exclude system accounts; Dashboard `TotalAssets` must additionally exclude system accounts according to `Phase5TotalAssetsScopeADR.md`.
- `IGoalService.GetAllAsync(userId, cancellationToken)` returns existing `GoalDto` data. Phase 5 retains the existing `Goals` response and must not introduce a Goal progress calculation or duplicate collection.
- `LifeDashboardDbContext` has a global Transaction filter for `!IsDeleted`. Existing report, transaction, and Budget Radar transaction reads use normal filtered queries; no `IgnoreQueryFilters()` is used by these read paths.
- Existing DI registrations are present for the services and repositories above. No new Repository Interface or data source is required for the planned aggregation.

---

## 2. Files To Modify

### Application and API

- `LifeDashboard.Application/Models/ReadModels/DashboardSummaryReadModel.cs`
- `LifeDashboard.Application/Models/DTOs/DashboardSummaryDto.cs`
- `LifeDashboard.Application/Services/DashboardService.cs`
- `LifeDashboard.Api/Controllers/DashboardController.cs`
- `LifeDashboard.Application/Models/DTOs/RadarStateDto.cs`
- `LifeDashboard.Application/Services/BudgetRadarAssembler.cs`

`BudgetRadarController` is not expected to change; it already returns the service's `RadarStateDto` at the approved route.

### Tests

Add separate Phase 5 tests for the newly approved response behavior, for example:

- `LifeDashboard.Tests/Dashboard/DashboardPhase5ReadModelTests.cs`
- `LifeDashboard.Tests/BudgetRadar/BudgetRadarRemainingLivingBudgetContractTests.cs`

Existing DTO shape assertions may be updated only to account for approved additive fields, as described in Section 5. Existing behavior assertions and tests remain frozen.

### Forbidden changes

- Entity or Domain model changes.
- `LifeDashboardDbContext`, schema, or migration changes.
- Transaction create/delete/write flow or Account balance logic.
- FixedCommitment Domain, formula, or payment tracking.
- Repository Interface changes or new repository/query methods.
- New API routes or request-contract changes.
- Changes to existing business behavior tests.

---

## 3. Implementation Sequence

### Phase 5A — Dashboard Summary

1. Extend the existing response and read model with the approved `TotalAssets` field. Do not introduce `TotalAccountBalance` as an alternate field.
2. Keep the existing `Goals` response unchanged. Do not add a `GoalsProgress` collection or recalculate Goal progress.
3. Use existing data sources only:
   - Monthly income and expense: `IReportRepository.GetMonthlyTotalsAsync`, using the calendar month containing `currentDate`.
   - Recent transactions: `ITransactionService.GetAllTransactionsAsync`, limited to 5 in the Application Service layer. Preserve existing `TransactionDate` descending order; do not add repository queries, sorting rules, or pagination.
   - `TotalAssets`: sum existing Account `CurrentBalance` values where `UserId` is the current user, `IsDeleted = false`, and `IsSystemAccount = false`, as defined in `Phase5TotalAssetsScopeADR.md`. Do not derive balances from Transactions.
4. Map the agreed read-model fields through `DashboardSummaryReadModel`, `DashboardSummaryDto`, and the existing Dashboard controller.

### Phase 5B — Budget Radar

1. Expose only `RemainingLivingBudget` from the existing `BudgetSnapshot` through `BudgetRadarAssembler` and `RadarStateDto`.
2. Do not change `BudgetSnapshot.Calculate`, payroll-period selection, FixedCommitment handling, or any budget formula.
3. Do not add `TotalBudget` or `TotalSpent` response fields.
4. Do not implement `UsagePercentage` or `Status`; these remain future/not implemented.

---

## 4. API Contract Validation

### Existing routes and behavior

- `GET /api/dashboard/summary` remains a read-only GET. Its required `currentDate` request remains unchanged.
- `GET /api/budget/radar` remains a read-only GET. Its required `currentDate` request remains unchanged.
- Neither endpoint may mutate database state or alter Transaction, Account balance, Goal, or FixedCommitment behavior.
- Existing response fields retain their names, types, meanings, and behavior. Only fields approved by the API contract may be added.

### Periods and approved fields

- Dashboard `MonthlyIncome` and `MonthlyExpense` use the calendar month containing `currentDate`.
- Budget Radar uses the existing Payday payroll period, not the calendar month.
- Dashboard additions are `TotalAssets`, `MonthlyIncome`, `MonthlyExpense`, and `RecentTransactions` (maximum 5).
- The existing `Goals` response remains unchanged; no `GoalsProgress` collection is added.
- The only Budget Radar addition is `RemainingLivingBudget`, exposed from the existing `BudgetSnapshot`.
- `TotalBudget` and `TotalSpent` are not Phase 5 additions. `UsagePercentage` and `Status` remain future/not implemented.
- Soft-deleted Transactions must not appear in recent transactions or contribute to transaction aggregates.

---

## 5. Test Strategy

### New tests

- Dashboard response coverage for `TotalAssets`, calendar-month income and expense, existing `Goals`, and `RecentTransactions` limited to 5 in existing descending date order.
- Dashboard regression coverage showing soft-deleted Transactions do not affect monthly totals or recent transactions.
- Budget Radar mapping coverage showing `RemainingLivingBudget` equals the existing `BudgetSnapshot` result, without duplicating its formula.
- Preserve Report soft-delete regression coverage; category breakdown remains Report responsibility, not Budget Radar responsibility.

### Existing tests and the narrow DTO-shape exception

Existing business behavior tests remain frozen. Do not modify, delete, skip, weaken, or change their behavior expectations.

Per `Phase5TestCompatibilityDecisionADR.md`, an existing test may be updated only when the affected assertion verifies DTO shape, including exact property sets, property names/types, or serialized shape. Such an update must:

- Cover only approved additive fields.
- Retain all existing field assertions and meanings.
- Assert the approved added fields and their types.
- Preserve every behavior assertion and unrelated test unchanged.

This may apply to exact-property assertions such as:

- `DashboardControllerApiTests.DashboardSummaryDto_has_only_composed_component_properties`
- `BudgetRadarAssemblerTests.Assemble_projects_commitments_builds_snapshot_and_maps_existing_dto`
- `BudgetRadarServiceTests.GoalPurchaseAggregate_WhenAvailableDoesNotChangeExistingRadarResponse`

`DashboardServiceTests.GetSummaryAsync_composes_results_in_order_without_mutating_sources` directly constructs the service with its existing dependencies. If implementation requires dependency-wiring changes in that test, only the instantiation/wiring may change under the compatibility ADR; its behavior assertions and expectations must remain unchanged.

Existing Transaction, soft-delete, balance, domain, aggregation-result, and other business behavior tests remain frozen and must pass unchanged.

---

## 6. Risk Assessment

### Soft-delete QueryFilter

Normal Transaction queries inherit the global `!IsDeleted` filter. Monthly totals, recent transaction reads, and existing Budget Radar aggregates must continue using filtered queries. Do not introduce `IgnoreQueryFilters()`.

### Calendar month versus payroll period

Dashboard monthly income and expense use the calendar month containing `currentDate`. Budget Radar uses the existing Payday payroll period. Do not substitute one period for the other.

### RecentTransactions limit and ordering

The existing transaction source is ordered by `TransactionDate` descending. Apply the fixed limit of 5 in `DashboardService`; same-date order remains whatever the existing source provides. The source currently materializes the full transaction collection before the service limit, so this avoids a new repository query but may over-fetch as history grows.

### BudgetSnapshot

`BudgetSnapshot` already contains `RemainingLivingBudget`; `BudgetRadarAssembler` currently maps only a subset of its values. Exposure must be a direct mapping. Do not recreate the calculation or alter FixedCommitment inputs.

### Account scope clarification

The existing account list excludes soft-deleted accounts but includes system accounts. The approved Dashboard aggregation rule excludes system accounts and includes only accounts matching the current user's `UserId`, `IsDeleted = false`, and `IsSystemAccount = false`. This rule applies only to `TotalAssets`; it does not change Account repository behavior or other consumers.

---

**Review outcome:** READY FOR IMPLEMENTATION within the approved scope, including the resolved `TotalAssets` filter. No C# code or tests have been changed by this documentation update.
