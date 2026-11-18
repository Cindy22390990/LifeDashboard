# Goal Funding Implementation Plan Review

## Review Scope

Reviewed:

- [GoalFundingDomainModelADR.md](./GoalFundingDomainModelADR.md)
- [GoalFundingDatabaseDesignADR.md](./GoalFundingDatabaseDesignADR.md)
- [GoalFundingMigrationPlan.md](./GoalFundingMigrationPlan.md)
- [GoalFundingImplementationPlan.md](./GoalFundingImplementationPlan.md)

No ADR, implementation plan, or code was changed as part of this review.

## 1. Domain Boundary Review

**Result: Aligned.**

- **Transaction responsibility:** The financial Transaction remains the
  record of Income, Expense, or Transfer. The implementation plan does not
  make it responsible for Goal Funding and does not create an Allocation
  automatically when a Transaction is created.
- **Goal responsibility:** Goal remains responsible for its lifecycle and
  target. The plan coordinates completion/reopening with the canonical
  funded total but does not make `Goal.CurrentAmount` a new Goal-owned
  funding source.
- **Allocation responsibility:** `GoalFundingAllocation` remains an
  independent Aggregate Root. The plan prohibits Goal and Transaction
  navigation properties and assigns cross-aggregate coordination to the
  Application layer.
- **`GoalId` coupling:** The plan restricts `Transaction.GoalId` to
  identified legacy consumers during the bounded transition. It does not
  use it for new Allocation creation or funding queries. Legacy
  Goal Purchase and Budget Radar consumers remain explicit removal
  prerequisites.

No new Domain boundary or dependency conflict was identified.

## 2. Data Source Review

**Result: Aligned, with a migration-stage gate required.**

- Active explicit Allocations are the only source for new user-initiated
  funding changes.
- Legacy Opening Progress is a one-time, immutable migration baseline. It
  is not an Allocation and does not imply a historical Transaction.
- `Goal.CurrentAmount` remains temporarily available for legacy behavior
  and dual-read comparison, but the plan stops its writes before snapshot
  and removes it as a funding reader/source after cutover.
- The new summary is calculated from opening progress plus active
  Allocations; it does not fall back to `CurrentAmount` or infer funding
  from `Transaction.GoalId`.

The read/write path must remain disabled until the persistence expansion,
opening-progress backfill, and validation gates are complete. The plan
states that activation occurs after validation; this gate should apply to
both query execution and Allocation commands, not only user-visible
cutover.

## 3. Application Flow Review

**Result: Responsibilities are generally clear.**

| Flow | Review |
|---|---|
| Transaction Create | Transaction use case owns the financial fact. Allocation is optional and separate; failed Transaction creation cannot produce an Allocation. A later Allocation failure leaves the valid Transaction and is retryable. |
| Transaction Update | The plan correctly notes there is no current amount-update path. If one is introduced, it must validate the revised amount against active Allocations and reject an over-limit update without rewriting Allocations. |
| Transaction Delete | Transaction Application Service owns soft delete. An Application notification handler invalidates active Allocations and records history in the same Unit of Work; otherwise deletion is rejected while active Allocations remain. |
| Allocation Create | Goal Funding Application use case validates eligible Income, ownership, Goal eligibility, idempotency, and the serialized active-total cap. |
| Allocation Invalid | Soft-delete invalidation is explicitly coordinated and preserves history. The plan also names Allocation reverse/lifecycle use cases; implementation acceptance should ensure explicit reversal records the same auditable validity transition. |
| Allocation Restore | Restore revalidates the source and cap before reactivating existing Allocations; it does not create duplicates. |

No conflicting ownership was identified. The concrete notification
dispatch mechanism and repository/Unit-of-Work implementation can follow
existing code patterns once readiness conditions are met.

## 4. Implementation Order Review

**Result: NEEDS CLARIFICATION before implementation.**

The high-level order—Preparation → Domain Model → Application Use Cases →
Read Model → Migration → Legacy Removal—is reasonable, and the plan
correctly defers destructive legacy removal until after cutover and
consumer retirement.

However, there is a circular phase dependency:

- Phase 3 (Read Model) depends on persisted Allocation and opening-progress
  data from Phase 2 **and Phase 4**.
- Phase 4 (Migration) depends on Phases 1–3 being ready.
- Phase 2 also requires Goal lifecycle changes to use the canonical funded
  total, while the named summary query is introduced in Phase 3.

The plan should distinguish implementation readiness from activation and
resolve this dependency before work is sequenced. For example, it can state
which calculation contract is available to Phase 2, which read behavior
can be built and validated before the backfill, and that no live
Allocation write or summary read is enabled until Expand, Backfill, and
Dual Read Validation pass. This is a plan/dependency clarification; it does
not require reopening the approved Domain semantics.

## 5. Missing Decisions

### Must be approved before implementation

- The Database Design ADR and Migration Plan are both marked **Proposed**.
  The Implementation Plan correctly makes their approval a readiness gate;
  that gate must be satisfied before implementation relies on their
  persistence, concurrency, or cutover choices.
- Resolve the Phase 2/3/4 dependency and explicitly gate live read/write
  activation on the migration sequence described above.

No additional Domain ADR decision was identified as necessary by this
review.

### May be settled during Code Implementation

- Concrete Domain type/member names and placement, following existing
  project conventions.
- The specific Application notification dispatch, repository, and
  Unit-of-Work integration, provided same-Unit-of-Work invalidation is
  preserved.
- Internal query decomposition and test organization, provided the summary
  semantics, zero-progress behavior, and no-fallback rule remain unchanged.
- Detailed UI interaction within the approved explicit funding scope; no
  UI wireframe or new funding behavior is implied here.

## Review Result

**Status: NEED REVISION**

The Domain boundaries, funding sources, legacy treatment, and individual
Transaction/Allocation responsibilities align with the ADRs. The
Implementation Plan's Phase 2/3/4 dependency is circular, and the
pre-cutover activation gate needs to be explicit. Clarify those sequencing
points before using the plan to start implementation. No ADR content needs
to be changed based on this review.
