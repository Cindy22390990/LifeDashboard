# Goal Funding Domain Model ADR Review

## Review Result

**NEED REVISION**

The ADR establishes a useful target boundary and explicitly separates the
new model from current legacy behavior. It is not yet a sufficient,
unambiguous input for Database Design because several decisions that affect
record lifecycle, aggregate ownership, and legacy cutover are still
conditional or open.

## Findings

### 1. Aggregate Boundary

**Mostly clear, with one unresolved decision.**

- The target prohibits Goal/Transaction navigation for allocations and
  states that `GoalFundingAllocation` is not nested under either aggregate.
  This is a clear boundary direction.
- The ADR says an Allocation “may be managed as its own aggregate root,”
  rather than deciding whether it is one. That uncertainty matters to
  transaction consistency, repository ownership, and persistence lifecycle.
- The cross-record rule `Active Allocation Sum <= Revised Transaction
  Amount` cannot be enforced by an individual Allocation root alone. The ADR
  appropriately assigns enforcement to the Application command and a
  persistence transaction, but does not yet select the allocation aggregate
  ownership model.

**Recommendation:** Decide that each independently created/reversed
Allocation is an independent funding aggregate root, not owned by Goal or
Transaction, while explicitly treating the per-Transaction total as a
cross-record invariant enforced by one serialized Application command.

### 2. GoalFundingAllocation Ownership

**Layer separation is reasonable; ownership wording needs tightening.**

The ADR places orchestration in Application and the durable funding record
in Domain. This is consistent if the Allocation has stable business
invariants and lifecycle (explicit creation, validation, reversal). It also
avoids making the Application DTO itself the persisted domain concept.

The phrase “owned by this capability” should mean product/use-case ownership,
not that the persisted entity belongs to the Application project. State the
distinction explicitly: Application owns the command/query capability;
Domain owns the independent Allocation aggregate; Infrastructure
implements its persistence.

### 3. Transaction / Goal Dependency Risk

**The target has no circular dependency; the legacy transition still does.**

The target Allocation uses scalar references and no navigations, so it does
not create a Transaction ↔ Goal cycle. However, the current
`Transaction.GoalId` and `Transaction.Goal` navigation remain a direct
Transaction-to-Goal dependency. The ADR acknowledges this as a deprecated
legacy path, which is appropriate, but new work must not extend it.

The transition is not yet bounded by a concrete exit condition. “After
consumers and historical data have an approved migration path” does not
identify when legacy flows stop writing `CurrentAmount`, when Goal
deposit/purchase consumers move, or when the Budget Radar query no longer
uses `GoalId`.

**Recommendation:** Define measurable cutover/retirement conditions and
state that no new service, query, or API behavior may rely on the legacy
relationship. Preserve existing behavior only until those conditions are
met.

### 4. Application Notification and Clean Architecture

**Compatible in principle, but the consistency contract is incomplete.**

An Application-level notification emitted by an Application service does
not violate Clean Architecture if the Domain entity does not publish an
Application event or depend on a handler. The ADR correctly says this is
not a Transaction Domain dependency on Goal.

Two issues remain:

- The current code has no Application notification/event mechanism, so this
  is a new Application capability, not an existing pattern.
- The ADR requires synchronous handling in the same Unit of Work but also
  says `GoalFundingSummary` is computed on read and no persisted projection
  is required for MVP. If the summary is computed on read, there is no
  stored summary to recompute; deletion handling should invalidate/reverse
  allocations, and the next query should calculate the summary. If a
  persisted projection is selected later, its update and transaction
  boundary must be specified.

**Recommendation:** For MVP, define a synchronous Application-level
transaction-deletion handler that updates allocation validity/history in
the same Unit of Work. Let the read query derive the summary from valid
allocations. Do not describe a separate summary recomputation write unless a
persisted projection is chosen.

### 5. Transaction Update / Soft Delete / Allocation Consistency

**Amount update is sufficiently directional; deletion semantics are not
fully selected.**

- The amount-update rule is clear at a high level: revalidate the active
  allocation total; do not silently clamp; reject the edit if allocations
  exceed the revised eligible amount until corrected. The current
  application has no amount-update path, so this remains a future contract.
- For soft delete, the ADR says to “invalidate or reverse” allocations.
  These are different lifecycle choices. It also offers same-Unit-of-Work
  handling or rejecting deletion if that cannot be guaranteed. The ADR
  should choose the normal policy and define whether history is represented
  by an explicit compensating action or another immutable audit fact.
- The current transaction service rejects deletion only when legacy
  `GoalId` is present. Under the new model, allocations do not use that
  field, so the delete path must check/coordinate allocations explicitly.

**Recommendation:** Choose append-only reversal as the normal soft-delete
policy; preserve the original allocation, exclude its reversed amount from
the summary, and perform the reversal with the Transaction soft delete in
one Unit of Work. If the operation cannot be atomic, reject deletion until
the allocations are reversed. Retain the already stated amount-edit
revalidation/rejection rule.

### 6. Domain Decisions Needed Before Database Design

The following are still prerequisites rather than settled Domain decisions:

1. **Allocation meaning:** Is an Allocation only an explicit progress/
   purpose record, or does it represent actual movement of money? If it is
   only a purpose record, it must not change Account balances.
2. **Eligibility:** Which Transaction types may be allocated? Are Goal
   Purchase expenses separate spending facts rather than funding sources?
3. **Allocation cardinality and limits:** Confirm partial allocations,
   multiple Goals per Transaction, and the maximum active allocation total.
4. **Goal lifecycle:** Define the exact Saving Goal completion transition,
   completion date, and behavior when an allocation is reversed, the target
   changes, or a Goal is deleted/completed.
5. **Aggregate ownership:** Confirm the independent Allocation aggregate
   root and the application-level enforcement of the cross-allocation sum.
6. **Legacy cutover:** Select how existing `CurrentAmount` is handled
   (reconciled history, explicitly identified opening progress, or separate
   legacy display) and define retirement conditions. Do not infer a complete
   allocation ledger from `GoalId`.
7. **Notification contract:** Confirm that MVP deletion handling is
   synchronous within the transaction and that the read model is computed
   from valid allocation history, rather than maintaining an unspecified
   projection.

## Recommended Revision

Before Database Design, amend the ADR to:

- Make Allocation an independent Domain aggregate root, with Application
  services owning orchestration and Infrastructure owning persistence
  implementation.
- Set a finite legacy transition with explicit cutover and removal
  conditions for `Transaction.GoalId` and `Goal.CurrentAmount`.
- Decide allocation semantics, eligible Transaction types, allocation
  cardinality/limits, and Goal completion/reversal behavior.
- Choose append-only Allocation reversal as the soft-delete policy and
  define the same-Unit-of-Work requirement.
- Align the notification description with the MVP read strategy: update
  allocation validity synchronously; derive `GoalFundingSummary` on read
  unless a persisted projection is separately approved.

## Database Design ADR Readiness

**Not ready to proceed.**

The ADR is a useful architectural starting point, but the unresolved
allocation semantics, Goal lifecycle, aggregate-root choice, and legacy
cutover affect persistence constraints and data migration. Resolve these
Domain decisions first; then use the accepted ADR as the input to Database
Design. This review does not propose a schema or migration.
