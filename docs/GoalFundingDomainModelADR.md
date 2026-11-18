# Goal Funding Domain Model ADR

## 1. Context

The current Goal funding model has three related concerns that need a clear
boundary:

- **`Goal.CurrentAmount` has ambiguous meaning.** It is persisted as Goal
  progress and participates in Saving Goal completion, but it does not prove
  that the corresponding money remains in an account or is reserved for that
  Goal.
- **Transaction and Goal are coupled.** The existing Transaction entity
  carries `GoalId` and a Goal navigation. Goal-specific services use that
  relationship for deposits and purchases, while queries use it to identify
  Goal Purchase expenses. This makes Transaction aware of Goal-specific
  behavior.
- **Funding Allocation needs an explicit record.** A Transaction records a
  financial event; it does not, by itself, express the user's intention to
  associate some amount with a Goal. The system must not infer that
  intention from a transfer, account, amount, or transaction description.

The current code and data remain in place until the bounded transition and
the required database and API decisions are separately implemented. This
ADR defines the accepted-candidate target model; it does not claim that the
target model has been implemented.

## 2. Final Decision Summary

Adopt the following target model:

```text
Transaction
    +
GoalFundingAllocation
    +
GoalFundingSummary (Read Model)
```

- **Transaction** remains the record of an Income, Expense, or Transfer.
- **GoalFundingAllocation** records an explicit user decision to allocate
  funding to a Goal. Its references to Transaction and Goal are identifiers;
  it does not add navigation properties to either aggregate.
- **GoalFundingSummary** presents funded progress as a read model derived
  from valid allocations and any identified migration opening-progress
  entry. It is not part of the Goal aggregate and is not an independent
  source of truth.

The following are not part of the target model:

- `Transaction.GoalId` as the formal Goal Funding relationship.
- `Goal.CurrentAmount` as the source of truth for funded progress.
- Account Bucket as the core Goal Funding model.
- A mixed Manual Adjustment model that combines implicit and explicit
  funding.

The legacy `Transaction.GoalId` and `Goal.CurrentAmount` are handled under
the bounded transition in Section 3; they are not silently reinterpreted as
new allocations.

## 3. Existing Model Migration Strategy

The current model contains `Transaction.GoalId` and `Goal.CurrentAmount`.
Removing them immediately would break existing Goal deposit/purchase flows,
Goal progress consumers, and the Budget Radar query. Retaining them
indefinitely beside the new model would create competing sources of truth.

Adopt **Bounded Transitional Compatibility**: preserve existing behavior
only for a defined transition, prohibit new features from depending on the
legacy fields, and retire them after consumers and historical data have an
approved migration path.

### `Transaction.GoalId`

**Status: Deprecated Legacy Field**

- New features must not read or write this field to create Goal Funding.
- New `GoalFundingAllocation` records do not depend on it.
- Existing Goal deposit/purchase and Budget Radar consumers may continue
  only as legacy compatibility paths until they are explicitly migrated.
- During the migration phase, no new service, query, or API behavior may
  depend on this relationship. Existing compatibility paths are restricted
  to the identified legacy consumers.
- A later migration removes the field and the Goal navigation after those
  consumers have moved to the approved model.
- Existing `GoalId` values are not automatically converted to allocations:
  they do not necessarily represent the same funding semantics or complete
  history.

### `Goal.CurrentAmount`

**Status: Deprecated Projection Field**

- It is not the source of truth for Goal funding.
- New Domain logic must not directly modify it.
- The new Dashboard reads `GoalFundingSummary`, not `Goal.CurrentAmount`.
- During Phase 0, the existing field and behavior remain unchanged.
- During migration, existing writers may remain only until their Goal
  deposit behavior has moved to the Funding capability. New features must
  not write it. After cutover, any retained API compatibility value is
  derived from the canonical read model and is not independently writable.
- A later migration removes the legacy persisted field after Goal lifecycle
  rules, API consumers, and historical progress have been addressed.

The current implementation still writes and reads `Goal.CurrentAmount` and
uses `Transaction.GoalId`. Those existing paths must be migrated before the
target rules above can be considered implemented. Until then, they remain
legacy behavior, not precedent for new features.

### Transition and cutover

**Phase 0 — Current behavior:** Keep the existing fields, data, and behavior
unchanged. This ADR does not authorize implementation or a migration.

**Migration phase:** New Goal Funding features use explicit allocations
only. Existing Goal deposit/purchase and Budget Radar behavior remains
available only through its legacy path until each consumer is migrated.
Do not dual-write `CurrentAmount` and allocations for the same funding
action. At cutover, preserve each existing `CurrentAmount` as a clearly
identified, immutable **legacy opening-progress entry** in the funding
history; do not invent a source Transaction or claim that its full audit
history is known. Do not backfill pre-cutover Transactions as new funding
Allocations: the opening-progress entry represents the complete pre-cutover
progress and this avoids double counting. Legacy `GoalId` values may remain
available to non-funding consumers during migration, such as Goal Purchase
classification, but they are never included in the funding summary.

**Post Migration:** Valid `GoalFundingAllocation` records and the
identified opening-progress entries are the sole source for funded
progress. `CurrentAmount` is no longer independently stored or written;
any compatibility API value is derived from `GoalFundingSummary`.
`Transaction.GoalId` and its Goal navigation are removed after all legacy
Goal deposit/purchase and Budget Radar consumers have been migrated.

The transition is bounded by these exit conditions, not by an open-ended
coexistence period:

1. Goal deposit writes and Saving Goal completion decisions use the Funding
   capability and its canonical funded total.
2. The Dashboard and all other progress readers use
   `GoalFundingSummary`; no reader treats the legacy field as authoritative.
3. Goal Purchase classification no longer depends on `Transaction.GoalId`.
4. Historical progress has been represented as identified legacy
   opening-progress entries, with no fabricated or double-counted
   Transaction history. Legacy Goal Purchase attribution is handled as
   non-funding history before `Transaction.GoalId` is removed.
5. No active writer or reader depends on `Transaction.GoalId` or the
   persisted `Goal.CurrentAmount`.

The old fields may coexist physically during this bounded migration, but
they must not be maintained as a second source of truth. Long-term
`Transaction.GoalId` plus `GoalFundingAllocation` coexistence is prohibited.

## 4. Domain Boundary Decision

### Transaction Domain

**Responsible for:**

- Income
- Expense
- Transfer

**Does not know about:**

- Goal
- Goal Funding Allocation

The target Transaction model contains no Goal Funding fields or navigation
properties. During transition, the deprecated legacy `GoalId` is isolated
from new behavior and removed only through the approved migration.

### Goal Domain

**Responsible for:**

- Goal lifecycle
- Target Amount
- Deadline for Goals that define one; this ADR does not add a deadline field
  or authorize schema changes

Goal does not reference Transaction or Goal Funding allocations. Removing
`CurrentAmount` from Domain responsibility requires the lifecycle transition
in Section 6; Goal completion rules must continue to be explicit.

### Goal Funding Capability

The capability is orchestrated in the Application layer and is responsible
for:

- Explicit Goal Funding Allocation commands and validation
- Funding calculation over valid allocations
- `GoalFundingSummary` read-model projection
- Coordinating Transaction and Goal references by identifiers, without
  aggregate navigation

**Aggregate decision:** Select the independent capability-managed option.
`GoalFundingAllocation` is its own Aggregate Root in the Domain model; it is
not an Entity inside the Goal or Transaction aggregate. The capability is
owned and orchestrated by Application services, while the funding record
and its intrinsic rules are Domain-owned and its persistence is implemented
by Infrastructure.

`GoalFundingAllocation` has no navigation property to Transaction or Goal;
it refers to them by identifiers only. Transaction and Goal entities and
services must not directly create, update, reverse, or otherwise modify an
Allocation. Create, reverse, and restore operations must pass through the
Goal Funding Application Service. The Application command also enforces
cross-Allocation invariants such as the active amount limit for a source
Transaction. That check must be serialized with persistence under
concurrency control; it is not an invariant of one Allocation root alone.

The Goal Funding Application capability is not a new subdomain and does not
create a dependency from Transaction Domain or Goal Domain to the Funding
model.

### Allocation semantic decision

`GoalFundingAllocation` is a **funding-purpose designation**, not a
financial movement and not a budget plan:

- **Transaction** records the financial fact that occurred.
- **Allocation** records the portion of that fact which the user explicitly
  designates for a Goal.
- Creating or reversing an Allocation does not change `Account` balances,
  create a Transaction, reserve cash, or prevent the money from being spent.
- Phase 1 eligible source Transactions are Income only. Expense and
  Transfer Transactions are not eligible funding sources. A Transfer is an
  actual movement between accounts, not Goal funding; it must never be
  inferred to be an Allocation.
- A user may leave an Income Transaction unallocated. If they explicitly
  allocate part of it, the same Income may have multiple Allocations, and
  the active Allocation total must not exceed its amount.
- Goal Purchase expenses remain spending facts, not funding Allocations.
  Their existing classification must be migrated separately before
  `Transaction.GoalId` is removed.

The funding history may also contain immutable migration opening-progress
entries. These preserve pre-cutover Goal progress without inventing a
Transaction or claiming it was a user-created Allocation. They are
distinguished from new Income-based Allocations and cannot be edited as if
they were new funding events.

## 5. Allocation Lifecycle

### Create

```text
User creates Transaction
        ↓
User explicitly creates GoalFundingAllocation
```

- Creating a Transaction does not automatically create an Allocation.
- A Transaction remains a valid financial fact without a Goal Allocation;
  allocation is optional and user initiated.
- Only an existing, non-deleted Income Transaction owned by the same user
  may be selected as a Phase 1 funding source. Each Allocation identifies
  one Goal; one Income Transaction may be explicitly split among multiple
  Goals.
- A failed Transaction creation cannot produce an Allocation from that
  unsaved Transaction.
- If Transaction creation succeeds but the separate Allocation command
  fails, retain the Transaction and report the Allocation failure. The
  Allocation command can be retried without creating a duplicate
  Transaction and must be idempotent.
- If a future use case requires both writes to succeed or fail together,
  it must use one Application use case and the same database Unit of Work;
  the two independent actions above are not atomic.
- Do not infer Goal funding from a Transfer or any other Transaction type.
  The Application capability validates user ownership, eligibility,
  positive allocation amount, idempotency, and the total-allocation limit.

### Delete

When a Transaction is soft-deleted:

```text
Transaction Application Service
        ↓
Application Notification
        ↓
Goal Funding Handler
        ↓
Mark its active Allocations invalid
        ↓
GoalFundingSummary excludes invalid Allocations on the next read
```

- Select lifecycle option C: preserve the Allocation history and mark the
  Allocation invalid when its source Transaction is soft-deleted. Do not
  erase or independently soft-delete that history.
- The notification is an Application-layer message emitted by the
  Transaction Application Service, not a Domain Event published by the
  Transaction entity. Its handler marks related Allocations invalid within
  the same Unit of Work as the soft delete.
- `GoalFundingSummary` is computed from valid allocations on read for MVP.
  The notification changes allocation validity; it does not write or
  recompute a separate persisted summary.
- If same-transaction invalidation cannot be guaranteed, reject deletion
  while active allocations exist until the allocations are explicitly
  reversed.

### Restore

If Transaction restore is added, the restore Application Service must
revalidate the source and its invalidated Allocations in the same Unit of
Work. If the restored Transaction remains an eligible Income Transaction
and the active allocation limit is still satisfied, restore its previously
invalidated Allocations as active. If validation fails, reject the restore
until the allocation conflict is explicitly corrected. Do not create a
second Allocation or discard the invalidation history.

### Update Amount

The current application does not provide a Transaction amount-update path.
If one is introduced, it must revalidate the active allocation sum against
the revised eligible Transaction amount:

```text
Active Allocation Sum <= Revised Transaction Amount
```

- Never silently clamp, rewrite, or redistribute allocations.
- If the revised amount still covers all active allocations, the edit may
  proceed subject to Transaction eligibility rules.
- If active allocations exceed the revised amount, reject the edit until
  the excess is explicitly reversed or corrected.
- Run the validation and update under concurrency control so a concurrent
  Allocation cannot invalidate the check.
- Do not automatically create additional Allocations when a Transaction
  amount increases.

## 6. CurrentAmount Final Decision

The Goal aggregate retains:

- `TargetAmount`
- Goal lifecycle state and its explicitly defined transition rules

The Goal aggregate no longer owns or directly modifies `CurrentAmount` as
funding state. `GoalFundingSummary` is responsible for:

- `FundedAmount`
- The calculated Goal progress presented to read clients

The new Dashboard reads the read model. It does not derive funding from
`Goal.CurrentAmount`, and the Application read path computes the summary
from valid allocations plus any identified migration opening-progress
entries. A persisted summary projection is not required for the MVP.

The existing Saving Goal completion behavior currently depends on
`CurrentAmount`. After cutover, a Saving Goal is Completed when the
canonical funded total is greater than or equal to `TargetAmount`. The Goal
Funding Application use case evaluates the before/after total and
coordinates the Goal lifecycle transition in the same Unit of Work as an
Allocation creation, invalidation, or restoration.

If a reversal or target change makes the funded total fall below the target,
the Saving Goal returns to Active and `CompletedDate` is cleared. When the
funded total meets the target again, the Goal becomes Completed and records
the new transition date. Purchase Goal completion remains an explicit
purchase lifecycle action and is not inferred from funded progress.

A Completed Goal accepts no new Allocation. A Goal with any funding
history, including a migration opening-progress entry, cannot be deleted;
the funding history must remain auditable. Changing a Goal's type is
rejected after funding history exists. A target change is allowed only after
the Saving Goal completion rule is re-evaluated in the same Unit of Work.

## 7. MVP Boundary

### Phase 1 supports

- Explicit Goal Funding Allocation
- Goal Progress Read Model
- Dashboard Query using `GoalFundingSummary`
- Income-only funding sources, explicit partial/multiple-Goal allocation,
  and active-allocation amount validation

### Out of scope

- Auto Funding Rules
- AI Allocation
- Investment Valuation
- Account Bucket

The lifecycle and domain boundaries for eligibility, partial/multiple
allocation, amount limits, retry/idempotency, soft-delete invalidation,
restore, amount correction, same-user validation, and concurrency are
defined in Sections 4–5. Goal status transition behavior and legacy progress
cutover follow Sections 3 and 6.

## 8. Database ADR Prerequisites

### Database Design Readiness Criteria

The following Domain decisions are now fixed and are prerequisites for the
Database ADR:

- `GoalFundingAllocation` is an independent Domain Aggregate Root managed by
  the Application capability, not nested under Goal or Transaction.
- Allocation is an explicit, Income-only funding-purpose designation. It
  does not move or reserve account money; Transfers are not funding sources.
- Active allocation totals are capped by the source Income Transaction
  amount. Transaction amount changes revalidate and reject invalid totals;
  they do not rewrite allocations.
- Transaction soft delete preserves Allocation history and synchronously
  invalidates related allocations in the same Unit of Work. Restore
  revalidates and reactivates them only when the constraints still hold.
- MVP `GoalFundingSummary` is calculated from valid Allocation history and
  identified opening-progress entries on read; it is not a persisted
  projection.
- Phase 0 behavior is unchanged. Migration uses an identified legacy
  opening-progress entry for existing `CurrentAmount`, does not fabricate
  Transaction history, and ends when the consumer retirement criteria in
  Section 3 are met.
- After migration, valid Allocation history plus identified opening-progress
  entries is the only source for funded progress. The legacy fields are not
  long-term parallel sources.
- Saving Goal completion/reopening, Purchase Goal completion, Goal deletion,
  Goal type changes, and target changes follow the lifecycle rules in
  Section 6.

**Readiness: Ready for Database Design ADR.** This means the Domain
semantics, aggregate boundary, lifecycle, and legacy transition are fixed.
It does not mean that persistence, schema, migration, or code changes have
been implemented.

The next Database ADR may choose the physical persistence details for these
fixed Domain decisions and define the migration execution plan. It must not
reopen their semantics without a revision to this ADR.

This ADR does not define a database schema or authorize a migration.

## Decision Status

**Accepted Candidate**

**Reason:** Domain boundary established, implementation deferred.
