# Phase 5 Test Compatibility Decision ADR

Date: 2026-11-16

Status: Accepted

## Context

The Phase 5 API contract permits additive DTO response fields. Existing tests are otherwise a frozen contract, including tests that assert an exact DTO property set. Those exact-shape assertions can fail when an approved additive field is introduced, even when existing fields and behavior are preserved.

## Decision

For Phase 5, a narrow exception is permitted for DTO shape assertion tests only. All other existing tests remain frozen.

This ADR clarifies the test compatibility rule in `Phase5ApiContractDecisionADR.md`. It does not change Phase 5 scope, approve new fields by itself, or authorize changes to production behavior.

The approved Phase 5 response additions are:

- Dashboard: `TotalAssets`, `MonthlyIncome`, `MonthlyExpense`, and `RecentTransactions` (maximum 5). The existing `Goals` collection remains; no `GoalsProgress` collection is added.
- Budget Radar: `RemainingLivingBudget` only. `TotalBudget` and `TotalSpent` are not Phase 5 additions; `UsagePercentage` and `Status` remain future/not implemented.

## Permitted test updates

An existing test may be updated only when its assertion is limited to DTO shape, such as:

- The exact set or count of DTO properties.
- DTO property names and types.
- The serialized response shape, only when the affected assertion verifies shape rather than behavior.

The update must:

1. Cover only additive fields explicitly approved in `Phase5DashboardApiContract.md` and the applicable ADR.
2. Keep every existing field, type, and meaning in the assertion.
3. Assert the approved added fields and their types.
4. Preserve all unrelated assertions, test cases, inputs, and behavior checks.

For a mixed test containing both shape and behavior assertions, only the shape assertion may be updated. The test's behavior assertions remain unchanged.

## Tests that remain frozen

No change is permitted to existing tests covering:

- Domain behavior.
- Transaction create/delete behavior or atomicity.
- Soft delete, QueryFilter, or deleted-transaction exclusion.
- Account balance behavior or recovery.
- Business calculations, aggregation results, or service behavior.
- Existing API request contracts, route behavior, authorization, error mapping, or side effects.

These tests must not be modified, deleted, disabled, skipped, or weakened to make Phase 5 pass.

## API compatibility

This exception does not reduce API compatibility requirements:

- Approved response additions must be additive.
- Existing response fields must retain their names, types, meanings, and behavior.
- Existing request contracts and API routes must remain unchanged.
- No new calculation, data source, or domain behavior is approved by a test-shape update.

## Implementation gate

Before changing a DTO, confirm that each new field is approved by the Phase 5 API contract and supported by existing data.

If implementation causes a frozen business-behavior test to fail, fix the implementation or stop for an ADR review. Do not change that test.

If a requested field is not approved, is marked future/not implemented, or requires a new calculation or data source, this ADR does not authorize its implementation.

## Scope and impact

This decision permits narrowly scoped updates to exact DTO-shape assertions for approved Phase 5 response fields. It does not permit updates to business-behavior tests or implementation code by itself.

No Entity, schema, migration, repository interface, Transaction flow, soft-delete behavior, or balance rule is changed by this ADR.

DashboardService constructor changes required by approved Phase 5
dependencies are considered implementation changes rather than
business contract changes.

Updating service instantiation in tests is allowed provided:

- existing behavioural assertions remain unchanged
- no business expectations are modified
- only dependency wiring is updated