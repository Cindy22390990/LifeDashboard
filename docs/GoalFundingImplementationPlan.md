# Goal Funding Implementation Plan

## Status

**Proposed — planning only.**

This plan is based on
[GoalFundingDomainModelADR.md](./GoalFundingDomainModelADR.md),
[GoalFundingDatabaseDesignADR.md](./GoalFundingDatabaseDesignADR.md), and
[GoalFundingMigrationPlan.md](./GoalFundingMigrationPlan.md). It describes
implementation phases; it does not authorize code, Entity, or database
changes by itself.

The Goal Funding **Phase 0: Preparation** below is a project preparation
phase. It does not change or reopen the frozen Dashboard V2 Phase 0 scope.

## 1. Implementation Scope

### MVP Include

- **`GoalFundingAllocation`:** an independent Domain Aggregate Root with no
  Goal or Transaction navigation properties.
- **Explicit Funding Flow:** user-initiated Allocation of a positive amount
  from an existing, non-deleted Income Transaction to a Goal. Allocation is
  optional and does not move or reserve account funds.
- **GoalFunding Query:** a read model calculated from immutable Legacy
  Opening Progress and active Allocations. The MVP does not persist a
  separate summary projection.
- **Legacy Compatibility:** keep the existing behavior and fields only
  during the bounded transition. `Goal.CurrentAmount` is captured as
  opening progress; `Transaction.GoalId` remains available only to
  identified legacy consumers until their replacement is ready. Neither is
  a second source of truth for new Goal Funding.

### Out of Scope

- Auto Allocation or automatic funding rules.
- AI Recommendation.
- Investment Tracking or valuation.
- Account Bucket as a funding model.
- Inferring Allocations from Transfers, expenses, account balances, or
  legacy `Transaction.GoalId` values.

## 2. Project Impact Analysis

| Project | Add | Modify | Must not own |
|---|---|---|---|
| `LifeDashboard.Domain` | The independent `GoalFundingAllocation` Aggregate Root and its intrinsic rules, including positive amount and validity lifecycle. | Goal lifecycle rules only as needed to support the approved funded-total completion behavior; keep Goal and Transaction independent of Allocation. | Persistence, EF configuration, cross-aggregate lookups, concurrency coordination, or UI concerns. |
| `LifeDashboard.Application` | Goal Funding use cases for explicit create/reverse/restore operations, source validation, active-total validation, the summary query, and Application-layer coordination for Transaction notifications and Goal lifecycle changes. | Transaction soft-delete and any future amount-update use cases to coordinate Allocation validity and cap validation in the same Unit of Work. Add repository/query abstractions where required by existing patterns. | EF Core details, database-specific locking implementation, or presentation behavior. |
| `LifeDashboard.Infrastructure` | Allocation and opening-progress persistence, EF mappings, repository/query implementations, history persistence, and the database concurrency protocol selected by the Database Design ADR. | Unit-of-Work and persistence integration for atomic Allocation lifecycle changes, soft-delete coordination, and the staged migration. | Domain decisions, user intent, Goal Funding presentation, or independent business-rule fallbacks. |
| `LifeDashboard.Web` | An explicit user flow for allocating eligible Income and a Goal progress view using the application read result. | Integrate with the application's existing write/read boundary; preserve any transitional behavior needed by identified legacy consumers. | Funding calculations, Allocation eligibility/cap enforcement, direct persistence access, or a parallel `CurrentAmount` source of truth. |

The Application notification for Transaction soft delete is an
Application-layer message, not a Domain Event emitted by the Transaction
entity. Use the existing dispatch pattern if present; if none exists, add
only the minimal Application-level mechanism needed for the required
same-Unit-of-Work behavior.

## 3. Implementation Phases

### Phase 0: Preparation

- **Goal:** Freeze the architecture and establish the implementation and
  migration baseline before adding the new capability.
- **Changed Components:** None. Complete ADR review/freeze, inventory
  existing readers and writers, and record the current test/CI baseline.
- **Dependency:** None; this phase precedes all implementation.
- **Acceptance Criteria:**
  - The Domain Model ADR, Database Design ADR, and Migration Plan are
    explicitly approved/frozen for implementation; proposed decisions are
    not assumed to be accepted.
  - Existing `Goal.CurrentAmount` and `Transaction.GoalId` readers and
    writers are inventoried, including Goal lifecycle, Goal Purchase
    classification, and Budget Radar consumers.
  - The current test/CI baseline and any known failures are recorded for
    comparison. This plan revision does not execute tests.
  - The Application notification and same-Unit-of-Work approach for
    Transaction soft delete, plus deployment and recovery ownership, are
    agreed.

### Phase 1: Domain/Foundation Implementation

- **Goal:** Build the new Goal Funding capability without making it a
  Production source of truth.
- **Changed Components:** `LifeDashboard.Domain` adds the independent
  `GoalFundingAllocation` Aggregate Root and intrinsic lifecycle rules.
  `LifeDashboard.Application` adds the repository interface, Goal Funding
  Application Service, and validation rules. Add focused Domain and
  Application unit tests.
- **Dependency:** Phase 0 decisions are approved/frozen.
- **Acceptance Criteria:**
  - Allocation is an explicit funding-purpose designation for eligible
    Income only; it is not a money movement or account reservation.
  - Domain rules cover positive amounts and valid Allocation lifecycle
    transitions; cross-Allocation caps and source eligibility remain
    Application-coordinated.
  - Allocation references Goal and Transaction by identifiers only; neither
    aggregate gains an Allocation navigation property.
  - The service and interface may be implemented and unit-tested, but
    Allocation writes are not enabled in Production and the service is not
    the Production Goal-progress source.
  - The Dashboard source is unchanged. `Goal.CurrentAmount` is not removed
    or repurposed, and existing `Transaction.GoalId` behavior is unchanged.

### Phase 2: Database Expansion

- **Goal:** Add persistence support for the new capability while preserving
  the legacy model and its current meaning.
- **Changed Components:** `LifeDashboard.Infrastructure` adds the
  approved persistence model, mappings, indexes, constraints, repository
  implementation, and additive database migration for Allocations,
  Allocation lifecycle history, and opening progress.
- **Dependency:** Phase 1 is complete; the Database Design ADR is approved.
- **Acceptance Criteria:**
  - New persistence enforces the approved ownership, reference,
    idempotency, and query requirements.
  - Existing `Goal.CurrentAmount` and `Transaction.GoalId` columns,
    relationships, and data meanings remain unchanged.
  - This is additive expansion only: no legacy column is dropped and no
    existing data is reinterpreted or backfilled as an Allocation.
  - Production Allocation writes and Dashboard reads remain disabled.
  - The expanded database can coexist with the legacy application behavior.

### Phase 3: Migration Preparation

- **Goal:** Generate and validate the opening-progress baseline without
  switching the Production Goal-progress source.
- **Changed Components:** `LifeDashboard.Infrastructure` performs the
  approved Legacy Opening Progress generation. The migration process
  produces a validation report comparing frozen legacy progress with the
  candidate Goal Funding read result.
- **Dependency:** Phase 2 expansion is deployed; all legacy progress
  writers are identified and can be paused.
- **Acceptance Criteria:**
  - Stop/drain legacy `CurrentAmount` writers before snapshot and keep them
    stopped throughout backfill and validation.
  - Capture each Goal's `CurrentAmount` once as immutable opening progress;
    retries do not duplicate or overwrite a verified baseline.
  - Do not convert pre-cutover Transactions or `Transaction.GoalId` values
    into Allocations.
  - Validate every Goal, including zero-progress Goals, and resolve all
    unexplained differences before cutover.
  - The validation report is reviewed and accepted.
  - The new Dashboard source and Production Allocation writes remain
    disabled; the frozen legacy value remains the Production source until
    Phase 4 is approved.

### Phase 4: Application Cutover

- **Goal:** Switch Production Goal-progress reads and writes to the
  validated Goal Funding capability.
- **Changed Components:** `LifeDashboard.Application` and
  `LifeDashboard.Web` activate the `GoalFundingSummary` read path and
  explicit Allocation use cases. Permanently disable legacy
  `CurrentAmount` writes for Goal progress.
- **Dependency:** Phase 3 validation report is accepted; the cutover and
  rollback/recovery plan is approved and executable.
- **Acceptance Criteria:**
  - The new Funding Source passes validation and its result matches the
    frozen legacy value for every Goal before activation.
  - Dashboard and other Goal-progress readers switch to
    `GoalFundingSummary`, calculated from opening progress plus active
    Allocations; `CurrentAmount` is not used as a fallback.
  - Explicit Allocation writes are enabled only after validation and
    cutover approval. New funding actions do not dual-write
    `CurrentAmount`.
  - Legacy Goal-progress writes stay disabled after cutover.
  - A rollback/recovery plan is demonstrated: before new authoritative
    Allocation writes, revert only if no new funding history is lost;
    afterward, use forward correction or reconciled recovery and never
    restore stale `CurrentAmount` as a competing source.

### Phase 5: Legacy Retirement

- **Goal:** Remove legacy compatibility code after all consumers and
  historical requirements have been addressed.
- **Changed Components:** `LifeDashboard.Domain`,
  `LifeDashboard.Application`, `LifeDashboard.Infrastructure`, and
  `LifeDashboard.Web` remove obsolete `CurrentAmount` write paths,
  `Transaction.GoalId` usage, and related compatibility code. Physical
  column/relationship removal requires the separately approved Contract
  migration; this plan does not implement it.
- **Dependency:** Phase 4 cutover is stable; the Migration Plan is
  complete; all legacy consumers have replacements.
- **Acceptance Criteria:**
  - There are no active readers or writers relying on legacy progress or
    `Transaction.GoalId`.
  - Goal deposit writes and Saving Goal completion decisions use the
    canonical Goal Funding source.
  - Goal Purchase classification and Budget Radar no longer depend on
    `Transaction.GoalId`.
  - Opening progress and Allocation lifecycle history are complete,
    retained, and auditable.
  - Compatibility code and, after its separate approval, legacy
    persistence can be removed without deleting or rewriting funding
    history.

## Dependency Graph

```text
Phase 0 — Preparation
        ↓
Phase 1 — Domain/Foundation Implementation
        ↓
Phase 2 — Database Expansion
        ↓
Phase 3 — Migration Preparation
        ↓
Phase 4 — Application Cutover
        ↓
Phase 5 — Legacy Retirement
```

The dependency is linear. Phase 3 prepares and validates data for Phase 4;
it does not depend on Production cutover. Phase 4 consumes the completed
Phase 3 validation report. There is no `Phase 3 → Phase 4 → Phase 3`
dependency.

## 4. Transaction Boundary Design

Here, **Transaction** means the financial Transaction entity; database
transaction/Unit of Work refers to persistence atomicity.

### Financial Transaction creation

- The Transaction use case owns creation of the financial fact.
- Creating an Income, Expense, or Transfer does not automatically create a
  Goal Funding Allocation.
- If Transaction creation fails, no Allocation may reference the unsaved
  Transaction.
- If creation succeeds and the user later submits a separate Allocation
  command that fails, keep the financial Transaction and surface the
  Allocation failure. Make the Allocation command safely retryable.
- A future use case that explicitly requires both operations to succeed or
  fail together must coordinate them in one Application use case and
  Unit of Work; this is not the default flow.

### Allocation creation

- The Goal Funding Application use case owns explicit Allocation creation.
- It verifies the source exists, is an Income Transaction, is not
  soft-deleted, belongs to the same user, and the Goal is eligible.
- It serializes the active-allocation sum check with persistence and
  enforces `active Allocation total <= source Income amount`.
- It uses idempotency for retries and does not rely on `Transaction.GoalId`
  or add navigation properties to either aggregate.

### Financial Transaction amount modification

- The current application has no amount-update path; this responsibility
  applies if one is introduced.
- The Transaction update use case and Goal Funding capability validate the
  revised amount against active Allocations under the same concurrency
  protocol used by Allocation creation.
- Reject an update that would make active Allocations exceed the revised
  amount. Do not silently clamp, redistribute, or create Allocations.

### Financial Transaction soft delete

- The Transaction Application Service owns the soft delete and emits an
  Application-layer notification.
- The Goal Funding handler invalidates active Allocations and appends their
  lifecycle history in the same Unit of Work.
- The next summary read excludes the invalidated Allocations; no persisted
  summary needs recomputation.
- If same-Unit-of-Work invalidation is unavailable, reject the deletion
  while active Allocations exist rather than leave inconsistent funding
  state.

## 5. Testing Strategy

This section defines validation strategy only; it does not add test code.

### Domain Test

Verify intrinsic Allocation rules and lifecycle transitions, including
positive amounts, active/inactive state changes, and prohibition on
invalid transitions. Verify that Transaction and Goal remain independent
of the Allocation aggregate.

### Application Test

Verify explicit use-case behavior: Income eligibility, same-user
validation, optional Allocation, idempotent retries, active-total
enforcement, Transaction amount-update rejection, soft-delete invalidation,
restore revalidation, and Goal lifecycle coordination. Cover success,
validation failure, and Unit-of-Work atomicity boundaries.

### Integration Test

Verify the Infrastructure implementation with the actual supported
database provider: persistence mapping, composite ownership constraints,
active-row query behavior, soft-delete/restore handling, idempotency,
concurrency behavior, summary calculation, and the staged backfill/
validation/cutover process. Validate migration retry and recovery behavior
against representative legacy data without treating legacy Transaction
references as Allocations.

## 6. Implementation Readiness Checklist

Before writing implementation code, confirm:

- [ ] Phase 0 ADR freeze is complete, including explicit approval of the
  Database Design ADR and Migration Plan.
- [ ] The full set of legacy field readers and writers and their owners is
  documented.
- [ ] The existing test/CI baseline is recorded for later comparison.
- [ ] The eligible Income Transaction and Goal validation rules are
  implementable through existing Application abstractions.
- [ ] The repository and Unit-of-Work approach supports atomic Allocation
  lifecycle updates with Transaction soft delete.
- [ ] The concurrency protocol and idempotency behavior are agreed and
  implementable by every writer of the Allocation cap invariant.
- [ ] Goal completion/reopening and restore behavior can be coordinated with
  funding changes in the same Unit of Work.
- [ ] Opening-progress capture can pause legacy writers, be retried safely,
  and be validated for every Goal before cutover.
- [ ] The new capability can remain disabled as a Production source through
  Phases 1–3.
- [ ] The Database Design ADR's persistence and same-user integrity choices
  are resolved before Infrastructure implementation.
- [ ] A rollback/recovery plan preserves new Allocation and opening-progress
  history after cutover.
- [ ] Each phase has a named acceptance owner, and no phase advances with
  unexplained data discrepancies.

This document is an implementation plan only. It contains no SQL,
migration code, API endpoint design, or UI wireframe.
