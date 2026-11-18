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

- **Goal:** Resolve implementation prerequisites and identify the complete
  legacy impact before changing code.
- **Changed Components:** None. Review the ADRs, current project
  boundaries, legacy readers/writers, Unit-of-Work behavior, and deployment
  and recovery constraints.
- **Dependency:** None; this phase precedes all implementation.
- **Acceptance Criteria:**
  - The Database Design ADR and Migration Plan have the required approval
    for implementation; unresolved database decisions are not treated as
    implemented facts.
  - Every `Goal.CurrentAmount` and `Transaction.GoalId` reader and writer
    is inventoried, including Goal lifecycle, Goal Purchase classification,
    and Budget Radar consumers.
  - The Application notification and same-Unit-of-Work approach for
    Transaction soft delete is feasible in the current architecture.
  - The staged migration, backfill pause, validation, cutover, and recovery
    responsibilities are agreed.

### Phase 1: Domain Model Addition

- **Goal:** Add the funding concept without coupling the Transaction or
  Goal aggregates to one another.
- **Changed Components:** `LifeDashboard.Domain` adds the independent
  `GoalFundingAllocation` Aggregate Root and its intrinsic lifecycle and
  amount rules. Transaction and Goal do not gain Allocation navigation
  properties.
- **Dependency:** Phase 0 decisions are approved.
- **Acceptance Criteria:**
  - Allocation means an explicit funding-purpose designation, not a money
    movement, budget, or account reservation.
  - The model supports an active/inactive lifecycle that preserves history.
  - Domain rules reject non-positive Allocation amounts and invalid
    Allocation state transitions.
  - Domain types do not reference EF Core, repositories, or the other
    aggregates.
  - Cross-Allocation totals and source Transaction eligibility remain
    Application-coordinated rules.

### Phase 2: Application Use Cases

- **Goal:** Implement explicit Allocation operations and coordinate
  Transaction and Goal behavior at the Application boundary.
- **Changed Components:** `LifeDashboard.Application` adds the create and
  lifecycle use cases, validates same-user ownership and eligible
  non-deleted Income sources, enforces the active Allocation cap, and
  coordinates Goal lifecycle transitions. It handles Transaction
  soft-delete notifications in the same Unit of Work as Allocation
  invalidation and history recording.
- **Dependency:** Phase 1 Domain model; persistence and concurrency
  contracts must be approved before wiring writes.
- **Acceptance Criteria:**
  - Creating a financial Transaction does not automatically create an
    Allocation; an unallocated Transaction remains valid.
  - A failed Transaction create cannot create an Allocation. If a separate
    Allocation command fails after Transaction creation, the Transaction
    remains valid and the Allocation failure is surfaced for a safe retry.
  - Allocation create is idempotent and serialized with the active-total
    check so concurrent requests cannot exceed the source amount.
  - Transaction amount changes, if introduced, are rejected when active
    Allocations would exceed the revised amount; Allocations are never
    silently clamped or redistributed.
  - Transaction soft delete either invalidates its active Allocations and
    records their transitions in the same Unit of Work, or is rejected
    while active Allocations remain if that atomicity cannot be guaranteed.
  - Restore revalidates the source and Allocation cap before reactivating
    prior Allocations; it does not create duplicates.
  - Saving Goal completion/reopening is coordinated against the canonical
    funded total in the same Unit of Work as the funding change.

### Phase 3: Read Model

- **Goal:** Provide consistent Goal progress to the Dashboard and other
  progress consumers.
- **Changed Components:** `LifeDashboard.Application` defines the
  `GoalFundingSummary` query/result; `LifeDashboard.Infrastructure`
  implements its read; `LifeDashboard.Web` consumes the result.
- **Dependency:** Phase 1 model and the required persisted Allocation and
  opening-progress data from Phase 2/Phase 4. The summary semantics must
  remain aligned with the Domain ADR.
- **Acceptance Criteria:**
  - Funded progress is calculated from opening progress plus active
    Allocations; invalidated Allocations are excluded.
  - Goals with no opening progress or Allocations return zero.
  - The query does not use `Goal.CurrentAmount` as a fallback and does not
    infer funding from `Transaction.GoalId`.
  - Multiple Allocations for one Income Transaction and multiple Goals are
    represented without double counting.
  - No persisted summary projection is added for the MVP.

### Phase 4: Migration

- **Goal:** Move existing progress to the new read model without inventing
  Allocation history or maintaining parallel sources of truth.
- **Changed Components:** `LifeDashboard.Infrastructure` persistence and
  the coordinated Application/Web read-write activation follow the
  Migration Plan sequence: **Expand → Backfill → Dual Read Validation →
  Cutover**. The cutover work includes stopping legacy progress writers,
  capturing each Goal's `CurrentAmount` as immutable opening progress, and
  enabling the new read/write path only after validation.
- **Dependency:** Phases 1–3 are ready; the Database Design ADR and
  Migration Plan are approved; all legacy consumers and writers are known.
- **Acceptance Criteria:**
  - Pre-cutover Transactions and `Transaction.GoalId` values are not
    backfilled as Allocations.
  - Opening-progress capture is one-time, restartable, and does not overwrite
    an already verified baseline.
  - Legacy Goal-progress writes are drained before snapshot and remain
    stopped through dual-read validation.
  - Every Goal's candidate progress is compared with the frozen legacy
    value, including zero-progress Goals; all differences are explained
    before cutover.
  - The cutover switches progress reads to the new summary and Allocation
    writes to the explicit flow without dual-writing `CurrentAmount`.
  - Recovery preserves authoritative new funding history and does not
    restore stale legacy values as a second source of truth.

### Phase 5: Legacy Removal

- **Goal:** Remove obsolete persistence and code paths after all consumers
  have moved to the new model.
- **Changed Components:** `LifeDashboard.Domain`,
  `LifeDashboard.Application`, `LifeDashboard.Infrastructure`, and
  `LifeDashboard.Web` remove references to `Goal.CurrentAmount` and
  `Transaction.GoalId` as each field becomes unused. Physical database
  retirement is performed only through a separately approved contract
  migration; this plan does not create one.
- **Dependency:** Phase 4 cutover and validation are complete; replacements
  exist for every legacy consumer.
- **Acceptance Criteria:**
  - No Goal-progress reader or writer depends on `Goal.CurrentAmount`.
  - Goal deposit writes and Saving Goal completion decisions use the
    Funding capability and canonical funded total.
  - Goal Purchase classification and Budget Radar no longer depend on
    `Transaction.GoalId`.
  - No compatibility path dual-writes or treats a legacy field as
    authoritative.
  - Opening progress and Allocation history remain retained and auditable
    after legacy-field removal.

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

- [ ] The Domain Model ADR, Database Design ADR, and Migration Plan have
  explicit approval for implementation; Proposed documents are not assumed
  to be accepted.
- [ ] The full set of legacy field readers and writers and their owners is
  documented.
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
- [ ] The Database Design ADR's persistence and same-user integrity choices
  are resolved before Infrastructure implementation.
- [ ] A rollback/recovery plan preserves new Allocation and opening-progress
  history after cutover.
- [ ] Each phase has a named acceptance owner, and no phase advances with
  unexplained data discrepancies.

This document is an implementation plan only. It contains no SQL,
migration code, API endpoint design, or UI wireframe.
