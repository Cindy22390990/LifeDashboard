# Phase 4A Dashboard API Contract Specification

Status: Proposed additive contract only. No application code, existing API, controller, Dashboard UI, or database schema is changed by this document.

## 1. Purpose and Scope

Define one future Dashboard summary response that composes existing Budget Radar, Account, and Goal application data. This contract does not implement an endpoint or prescribe UI presentation.

The proposed summary is additive: existing `GET /api/budget/radar`, `GET /api/Account`, and `GET /api/goals` contracts remain unchanged. The Dashboard summary must not introduce new calculations or business rules; its data is provided by Application services.

## 2. Proposed Endpoint Contract

```http
GET /api/dashboard/summary?currentDate=yyyy-MM-dd
Authorization: Bearer <JWT>
```

- Authentication is required.
- `userId` is obtained from the authenticated JWT claims; clients must not send a user id.
- `currentDate` is required and is passed unchanged to the existing Budget Radar Application use case.
- The endpoint is a proposed new contract. It does not replace or alter existing endpoints.

Expected status semantics follow the existing component contracts:

| Status | Meaning |
| --- | --- |
| `200 OK` | All requested summary sections are returned. Empty account/goal collections are valid empty arrays. |
| `400 Bad Request` | Missing or invalid `currentDate`, following the existing Budget Radar behavior. |
| `401 Unauthorized` | Missing or invalid JWT. |
| `404 Not Found` | Required user SystemSetting is unavailable, following the existing Budget Radar behavior. |

No new error fields or custom error semantics are introduced here.

## 3. DashboardSummaryDto

The response is a composition of existing component DTOs. It reuses their types rather than redefining their fields:

```csharp
public sealed class DashboardSummaryDto
{
    public RadarStateDto Budget { get; init; } = null!;
    public IReadOnlyList<AccountDto> Accounts { get; init; } = Array.Empty<AccountDto>();
    public IReadOnlyList<GoalDto> Goals { get; init; } = Array.Empty<GoalDto>();
}
```

Example JSON shape:

```json
{
  "budget": {
    "dailySafeLimit": 322.58,
    "todaySpent": 85.50,
    "todayRemaining": 237.08,
    "remainingDays": 10,
    "totalLivingBudget": 5000.00,
    "uncategorizedExpenseAmount": 120.00
  },
  "accounts": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "name": "Cash",
      "type": 1,
      "currentBalance": 5000.00,
      "isSystemAccount": false,
      "allowNegativeBalance": false
    }
  ],
  "goals": [
    {
      "id": 1,
      "name": "Emergency fund",
      "targetAmount": 10000.00,
      "currentAmount": 2500.00,
      "tier": 1,
      "goalType": 1,
      "status": 1,
      "note": "",
      "completedDate": null
    }
  ]
}
```

The example values illustrate shape only; the contract does not prescribe enum serialization changes or new display semantics.

## 4. Section Contracts

### Budget

`Budget` is exactly the existing `RadarStateDto`, nested as a property. Do not copy its fields into `DashboardSummaryDto` or define a second budget DTO with equivalent values.

- Values are produced by the existing `IBudgetRadarService` and its `BudgetSnapshot` calculation.
- The summary endpoint does not recalculate payroll periods, commitments, expense totals, remaining days, or daily limits.
- Existing negative values, rounding, nullable internal Goal Purchase metrics, and uncategorized-expense behavior remain governed by the Budget Radar contract.
- No Account balance, Goal progress, or additional totals are mixed into the Budget section.

### Accounts

`Accounts` is the current per-user Account list using the existing `AccountDto` shape:

- `Id`
- `Name`
- `Type`
- `CurrentBalance`
- `IsSystemAccount`
- `AllowNegativeBalance`

Use the existing Account Application query/service. Do not calculate a total balance, filter account types, or change soft-delete inclusion as part of this contract. The list semantics remain the same as the existing Account list contract until separately revised.

### Goals

`Goals` is the current per-user Goal list using the existing `GoalDto` shape:

- `Id`
- `Name`
- `TargetAmount`
- `CurrentAmount`
- `Tier`
- `GoalType`
- `Status`
- `Note`
- `CompletedDate`

Use the existing Goal Application query/service. Include goals according to the existing list semantics; do not add status filters, progress percentages, tier-lock decisions, completion calculations, or aggregate totals. `CurrentAmount` is returned as stored progress and must not be used to recompute Budget values.

### Quick Actions

`Quick Actions` are omitted from this API contract. They represent client navigation or commands rather than server-owned Dashboard state. Their availability must not be encoded as API business rules in this phase.

## 5. Domain Model, Read Model, and API DTO Boundaries

### Domain Model

- `BudgetSnapshot` remains the immutable Domain value object that owns Budget Radar arithmetic.
- `Account` and `Goal` remain Domain entities and are not serialized directly by the proposed API.
- Domain models do not depend on Dashboard DTOs, HTTP, or UI concerns.

### Application Read Model

The Application layer composes a `DashboardSummaryReadModel` from existing use cases/query results. Conceptual shape:

```text
DashboardSummaryReadModel
  Budget: existing RadarStateDto result or equivalent application projection
  Accounts: Account application projection collection
  Goals: Goal application projection collection
```

The Application layer is responsible for user scoping, invoking the existing Budget/Account/Goal queries, and assembling their results. It must not perform new business calculations. If a distinct internal read model is introduced, it should use application projections rather than Domain entities and remain independent of HTTP serialization.

### API DTO

`DashboardSummaryDto` is the transport composition. Its sections reuse the existing `RadarStateDto`, `AccountDto`, and `GoalDto` contracts. Mapping must be mechanical; it must not calculate or reinterpret values.

This separation keeps the Budget formula in Domain/Application calculation code, data retrieval in existing Application query services/repositories, and serialization at the API boundary.

## 6. No Duplicate Budget Contract

The summary endpoint does not rename, copy, or extend the six existing Radar fields. The `Budget` property uses the existing `RadarStateDto` contract as a nested component. Consequently:

- Existing `GET /api/budget/radar` remains supported without change.
- The nested Budget fields retain the same names, types, nullability, and calculations as the existing response.
- The summary response does not add a second `periodIncome`, commitment total, Goal Purchase total, or account balance to the Budget block.
- Any future expansion of `RadarStateDto` is separately reviewed and is reflected by type reuse, not duplicated field maintenance.

## 7. Data Ownership and Consistency

- All three sections are scoped to the authenticated user by Application services; no section accepts client-supplied `userId`.
- All values originate in Application. The client must not recalculate Budget values, goal completion, goal progress, or account totals.
- Existing service/query behavior is preserved, including current list contents and error behavior.
- A successful response represents a composition of the three query results; this contract does not promise a cross-module database snapshot/transaction or impose a new consistency guarantee.
- If a required section cannot be produced, do not silently substitute zero or an empty array for an error. Empty arrays are valid only when the corresponding query succeeds and returns no rows.

## 8. Future Extension Candidates

The following fields are not part of the initial contract and require a separate product/API decision:

- `asOfDate`, payroll period start/end, or payroll month as additional Budget context.
- Goal Purchase amount/count, which currently remain internal Snapshot/query metrics and are not part of `RadarStateDto`.
- Active FixedCommitment detail rows or a separate commitment summary.
- Account-level totals or account grouping; account soft-delete and balance semantics must first be decided.
- Goal summary counts, completion counts, or computed progress percentages.
- Uncategorized transaction counts or itemized transaction details.
- `generatedAtUtc`, a response contract version, or other operational metadata.

Do not add a generic `metadata` dictionary or speculative fields to the initial DTO. Add future sections additively and only after their source, semantics, nullability, and compatibility are specified.

## 9. Contract Acceptance Criteria

1. The endpoint is additive; all existing APIs remain unchanged.
2. The response has only the `Budget`, `Accounts`, and `Goals` sections in its initial form.
3. `Budget` reuses `RadarStateDto` and does not redeclare its fields.
4. Account and Goal items reuse their existing DTO contracts and list semantics.
5. No response field requires frontend calculation or introduces a new business rule.
6. Application provides all section data using the authenticated user's scope.
7. Quick Actions are not returned by the server.
8. The specification does not require Controller implementation, UI work, or schema changes.
