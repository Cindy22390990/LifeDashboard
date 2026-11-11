# Phase 4B Dashboard Application Service Specification

Status: Proposed Application orchestration boundary. This document does not modify code, Controller, API contract, Dashboard UI, or database schema.

## 1. Purpose and Constraints

Define how a future Dashboard Application use case composes existing Budget Radar, Account, and Goal application results into the `DashboardSummaryReadModel` described by the Phase 4A contract.

This service is an orchestrator only. It adds no business rules, calculations, filtering policies, or persistence behavior. The existing `IBudgetRadarService`, `IAccountService`, and `IGoalService` contracts remain unchanged.

## 2. IDashboardService Responsibility

Proposed Application boundary:

```csharp
public interface IDashboardService
{
    Task<DashboardSummaryReadModel> GetSummaryAsync(
        Guid userId,
        DateOnly currentDate,
        CancellationToken cancellationToken);
}
```

`DashboardService` responsibilities:

- Receive the authenticated `userId` from its caller and the requested `currentDate`.
- Call existing Application services with the same `userId`.
- Pass `currentDate` only to `IBudgetRadarService`.
- Pass the existing cancellation token to `IGoalService.GetAllAsync`; current Account and Budget service signatures do not accept cancellation tokens, so do not change them as part of this phase.
- Compose all successful component results into `DashboardSummaryReadModel`.
- Return the complete read model or propagate a component failure; do not produce a partial summary with substituted zero values or empty collections.

`DashboardService` must not:

- Depend on or call repositories directly.
- Recompute Budget or inspect `BudgetSnapshot` to derive different numbers.
- Filter Account/Goal lists, recalculate progress/completion, or aggregate totals.
- Know HTTP routes, status codes, JSON serialization, React routes, or UI actions.
- Change the public contracts of existing services.

## 3. Dependencies and Existing Service Composition

`DashboardService` depends only on Application service interfaces:

```text
IDashboardService
  -> IBudgetRadarService.GetRadarStateAsync(userId, currentDate)
  -> IAccountService.GetAllAccountsAsync(userId)
  -> IGoalService.GetAllAsync(userId, cancellationToken)
```

The Budget Radar service remains the only owner of payroll-period derivation and BudgetSnapshot calculation. The Account service remains the owner of the existing user-scoped Account query/projection. The Goal service remains the owner of the existing user-scoped Goal query/projection.

The Dashboard use case must not call `ITransactionRepository`, `IFixedCommitmentRepository`, `IAccountRepository`, or `IGoalRepository` directly.

### Read ordering

Invoke the three existing use cases sequentially in the initial implementation. The current composition root registers repositories and services as scoped; they may share the same scoped `LifeDashboardDbContext`. `Task.WhenAll` across these service calls risks concurrent operations on one EF Core context and is not part of this contract.

The calls are independent in business terms, but the composed response is not a cross-module database transaction or point-in-time snapshot. No new consistency guarantee is introduced.

## 4. DashboardSummaryReadModel

Use an Application-level read model to represent the completed composition:

```csharp
public sealed record DashboardSummaryReadModel(
    RadarStateDto Budget,
    IReadOnlyList<AccountDto> Accounts,
    IReadOnlyList<GoalDto> Goals);
```

The record contains existing Application service projections, not Domain entities. It is not an HTTP response type and must not contain UI state, navigation commands, or preformatted display strings.

Collection semantics:

- Preserve the exact Account and Goal lists returned by their existing services.
- Do not add status filters, soft-delete rules, totals, counts, or sorting in `DashboardService`.
- Empty arrays are valid when the successful service query returns no items.
- A failed component query fails the composition; it is not converted into an empty array.

The query is user-scoped by passing the same `userId` to all three Application services. `DashboardService` does not accept a client-selected user id through an API query/body contract.

## 5. Component Ownership

| Component | Source and owner | Dashboard service action |
| --- | --- | --- |
| Budget | `IBudgetRadarService` returns the existing `RadarStateDto`. Its service owns period logic and `BudgetSnapshot` formula. | Copy the result into `ReadModel.Budget`; do not recalculate or add fields. |
| Accounts | `IAccountService.GetAllAccountsAsync(userId)` returns `IEnumerable<AccountDto>`. | Preserve returned items as the Accounts collection. |
| Goals | `IGoalService.GetAllAsync(userId, cancellationToken)` returns `IReadOnlyList<GoalDto>`. | Preserve returned items as the Goals collection. |

Do not infer relationships between Account balances, Goal progress, or Budget totals. They are separate components with separate source-of-truth services.

## 6. ReadModel to API DTO Mapping Boundary

`DashboardSummaryReadModel` is the Application composition result. `DashboardSummaryDto` is the Phase 4A transport contract.

The eventual API-boundary mapper performs only mechanical top-level composition:

```text
DashboardSummaryDto.Budget   <- DashboardSummaryReadModel.Budget
DashboardSummaryDto.Accounts <- DashboardSummaryReadModel.Accounts
DashboardSummaryDto.Goals    <- DashboardSummaryReadModel.Goals
```

- The Budget property reuses `RadarStateDto`; do not redeclare or rename its six fields.
- Account entries reuse `AccountDto`; Goal entries reuse `GoalDto`.
- No enum conversion, numeric aggregation, null substitution, or business-rule evaluation occurs in the mapping.
- The API boundary does not mutate or reshape the lists beyond serialization.

This specification defines the mapping boundary only. It does not implement a Controller, add an endpoint, or change any existing response.

## 7. Error and Cancellation Semantics

- Missing SystemSetting and invalid `currentDate` retain the behavior of the existing Budget Radar use case and its caller.
- Account/Goal service failures propagate; do not hide them by returning partial success.
- Pass `cancellationToken` to Goal service, whose existing method supports it. The current Account and Budget service contracts have no cancellation parameter; adding one is outside this scope.
- The Application service does not define HTTP status codes or an error DTO.

## 8. Domain, ReadModel, and API DTO Separation

### Domain Model

- `BudgetSnapshot` owns Budget calculations and remains immutable.
- `Account` and `Goal` remain Domain entities.
- The Dashboard service does not depend on Domain entities directly for list construction or calculations.

### Application ReadModel

- `DashboardSummaryReadModel` composes results from existing Application services.
- It is application-facing data, not a persisted entity and not a transport/controller type.
- It contains no UI state, business-rule result not already produced by component services, or repository query object.

### API DTO

- `DashboardSummaryDto` is the response contract proposed in Phase 4A.
- Existing `RadarStateDto`, `AccountDto`, and `GoalDto` remain unchanged and are reused as component types.
- Mapping is mechanical and must not become a second calculation layer.

## 9. Acceptance Criteria

1. `IDashboardService` is a single Application orchestration boundary and introduces no alternate Budget service.
2. The service calls Budget, Account, and Goal existing Application interfaces; it does not access repositories.
3. The same authenticated `userId` is passed to all component services; only Budget receives `currentDate`.
4. Budget values come directly from `IBudgetRadarService`; no budget formula is repeated.
5. Account and Goal values are preserved as returned; no new filters, totals, counts, or completion calculations are introduced.
6. Reads are sequential to avoid concurrent access to scoped EF Core contexts.
7. A component failure does not yield a partial response with fabricated data.
8. The read-model/API DTO mapping copies the three component values without changing existing DTO fields.
9. No Controller, API route, UI, database schema, or migration is defined or modified here.

## 10. Explicitly Excluded Responsibilities

The Dashboard Application service must not own:

- Payday boundary calculation, fixed commitment deduction, daily limit arithmetic, or negative-budget handling.
- Goal progress percentages, tier eligibility/locking, or completion transitions.
- Account balance reconciliation, total cash calculations, or soft-delete visibility changes.
- Transaction querying, category classification, or spending aggregation.
- Quick Action availability, URLs, labels, permissions, or UI state.
- HTTP authorization attributes, route/query binding, response codes, or JSON serialization.
