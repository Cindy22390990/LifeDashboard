# Goal Funding ADR Revision Proposal

## 1. Current Conflict Summary

The proposed `GoalFundingAllocation` direction is not a clean replacement
for the current model yet:

- `Transaction` currently has `GoalId`, a `Goal` navigation, and
  `AssociateWithGoal(Goal)`. EF Core maps that relationship, and existing
  Goal deposit/purchase flows use it. The Budget Radar query also uses
  `Transaction.GoalId` to identify Goal Purchase expenses.
- `Goal.CurrentAmount` is persisted and returned by `GoalService`. The
  Saving Goal completion rule in `Goal.ApplyDeposit` depends on it.
  `GoalDepositService` currently updates it in the same Unit of Work as the
  related transfer.
- The new decision says neither Domain should know the other, and proposes
  `GoalFundingAllocation` as the funding record. Retaining both models
  without choosing a canonical source creates ambiguous progress totals,
  potential double counting, and unclear deletion behavior.
- The stated Transaction-then-Allocation sequence consists of separate
  actions. It does not provide atomicity between the two actions.

The ADR therefore needs a deprecation and migration strategy. Existing
transactions and Goal progress cannot safely be assumed to reconstruct a
complete allocation history.

## 2. Migration Strategy Options

### A. Breaking Change

Remove `Transaction.GoalId` and its Goal navigation, and remove
`Goal.CurrentAmount` as part of one cutover.

**Advantages**

- Establishes the new boundary and a single canonical funding model quickly.
- Avoids a long period of dual reads and writes.
- Reduces future maintenance after all consumers have moved.

**Disadvantages**

- Breaks existing Goal deposit/purchase flows and consumers of
  `GoalDto.CurrentAmount` unless they are migrated at the same time.
- Requires replacement behavior for the Budget Radar query that currently
  relies on Goal-linked purchase transactions.
- Historical `CurrentAmount` and `GoalId` data do not necessarily contain
  enough information to create an accurate allocation ledger.

**Technical risks**

- Existing Goal status/completion dates can change or become inconsistent
  during cutover.
- An unverified backfill could invent allocation history or count the same
  funds twice.
- A partial deployment could leave existing clients or services expecting
  fields that have already been removed.

**Future maintenance cost**

Low after a fully coordinated migration, but high immediate delivery and
rollback risk. This is not recommended as the default Phase 1 transition.

### B. Transitional Compatibility

Retain the legacy relationship and progress behavior for a defined
transition, while the new capability introduces explicit allocations.
Define a cutover after which the allocation ledger is canonical and the
legacy fields are compatibility-only or removed.

**Advantages**

- Allows existing Goal flows and API consumers to continue during migration.
- Gives the team time to replace Budget Radar and Goal lifecycle consumers.
- Supports a controlled reconciliation of historical Goal progress.

**Disadvantages**

- Requires a clearly bounded period and precise read/write ownership.
- A summary that combines legacy progress and new allocations can double
  count; a summary that ignores legacy progress can appear to reset Goals.
- Compatibility behavior adds temporary complexity.

**Technical risks**

- Unrestricted dual writes can diverge if one path fails or is retried.
- Legacy Goal-linked transactions may be mistaken for allocation records.
- A stale compatibility value can disagree with the new summary.

**Future maintenance cost**

Medium during the transition and low after the legacy path is retired. This
is the recommended option only with a documented cutover and no indefinite
dual-write state.

**Required transition policy**

1. Before cutover, existing flows remain authoritative; new commands must
   not silently write both `CurrentAmount` and allocations.
2. At cutover, product must choose how existing progress is represented:
   preserve it as a clearly identified legacy opening balance, reconcile
   verified history, or keep the legacy progress separate until it is
   resolved. Do not infer a complete history from `GoalId` alone.
3. After cutover, allocations become the only write source for funded
   progress. Any retained `CurrentAmount` API value is a compatibility
   projection of the canonical summary, not an independently writable
   source.
4. Keep `Transaction.GoalId` only as long as existing behavior needs it;
   migrate its Budget Radar and Goal purchase consumers before removing or
   repurposing it.

### C. Keep Legacy Model

Keep `Transaction.GoalId` and `Goal.CurrentAmount` as long-term models and
add `GoalFundingAllocation` alongside them.

**Advantages**

- Minimizes short-term changes to existing flows and contracts.
- Allows the new allocation feature to be introduced without an immediate
  legacy cutover.

**Disadvantages**

- Leaves two representations of Goal progress and multiple ways to express
  Goal-related transactions.
- Requires permanent rules for which model is authoritative in every
  service, query, and API response.
- Makes reconciliation and debugging harder as behavior evolves.

**Technical risks**

- Goal totals can diverge or double count.
- Soft deletion and amount corrections may update one representation but not
  the other.
- Future developers may use the legacy relationship for new features despite
  the ADR boundary.

**Future maintenance cost**

High and recurring. This option is not recommended unless the product
explicitly decides that the two models represent distinct, separately named
facts rather than competing versions of funded progress.

### Comparison Summary

| Option | Compatibility | Boundary clarity | Technical risk | Long-term maintenance |
|---|---|---|---|---|
| A. Breaking Change | Low | High after cutover | High during migration | Low after migration |
| B. Transitional Compatibility | High during transition | High after explicit cutover | Medium and controllable | Low after retirement |
| C. Keep Legacy Model | High | Low unless semantics are distinct | High ongoing | High |

## 3. Recommended Decision

Choose **B. Transitional Compatibility**, with a finite transition and a
single canonical source after cutover.

### Transaction and Allocation failure policy

An ordinary Transaction remains a valid financial fact without a Goal
Allocation. Allocation is an explicit, optional user action; most
transactions need not have one.

- If Transaction creation fails, no Allocation can be created from that
  unsaved Transaction.
- If Transaction creation succeeds but the separate Allocation command
  fails, keep the Transaction. Report the Allocation failure and allow the
  user to retry the Allocation without creating a duplicate Transaction.
- Make Allocation creation idempotent for a retried user command.
- If a future UX treats both writes as one indivisible “fund this Goal”
  action, use a single Application use case and the same database Unit of
  Work to commit or roll back both. Do not describe the current two-command
  flow as atomic.

### Soft-deleted Transaction policy

Do not soft-delete or erase the historical Allocation record. Preserve the
original allocation history and create an explicit reversal/invalidation
when its source Transaction is soft-deleted. Exclude reversed allocations
from the active funding summary.

For consistency, process the soft delete and allocation reversal within the
same database transaction. If that cannot be guaranteed, reject deletion
while active allocations exist and require the user to reverse them first.
An after-commit notification alone may leave the summary inconsistent if
its handler fails.

### Transaction amount-change policy

The current application has no Transaction amount-update path. If one is
introduced, revalidate the active allocation total against the revised
eligible amount. Do not silently clamp or rewrite allocations.

In the example, reducing a Transaction from 10,000 to 8,000 while 5,000 is
allocated remains within the proposed cap, subject to eligibility rules. If
the active allocation total exceeds the revised amount, reject the edit
until the excess is explicitly reversed or otherwise corrected. The ADR
must choose this policy before amount editing is supported.

### CurrentAmount policy

- Do **not** remove `CurrentAmount` immediately. Existing application logic,
  persistence, and API output depend on it.
- Deprecate its use only after all Goal progress writers and readers have a
  defined replacement. During transition it may remain as a read-compatible
  projection, but it must not be independently writable after cutover.
- Do not retain it as an independently maintained Domain cache in the MVP.
  That would create a second source of truth and require every create,
  reversal, and correction path to keep it synchronized.
- Compute `GoalFundingSummary` in the Application read path from the
  canonical allocation history. A projection table is not required for the
  MVP unless measured performance or availability requirements later
  justify one.
- Preserve the existing Saving Goal completion behavior deliberately:
  define how the canonical funded total triggers status and completion-date
  changes, and what a reversal does to a completed Goal. The read model
  alone must not silently replace this Domain lifecycle rule.

## 4. Required ADR Amendments

Amend the Goal Funding decision to explicitly state:

1. **Legacy transition:** Select Option B, define the transition's end
   condition, identify the consumers of `Transaction.GoalId` and
   `CurrentAmount`, and define how historical progress is represented.
   Do not automatically treat every legacy Goal-linked Transaction as an
   Allocation.
2. **Canonical source:** Define which model is authoritative before and
   after cutover. Prohibit uncoordinated dual writes and specify how existing
   API consumers receive progress during transition.
3. **Allocation semantics:** Define eligible Transaction types, whether an
   Allocation is an earmark or actual money movement, partial allocation,
   multiple Goals per Transaction, amount caps, and whether Goal Purchase
   expenses are funding sources or a separate spending fact.
4. **Failure and retry:** State that a standalone Transaction remains valid
   when its optional Allocation command fails; define retry/idempotency.
   If a use case requires atomicity, specify that both writes share one
   Application transaction.
5. **Deletion and correction:** Define soft-delete reversal, Allocation
   history retention, summary treatment, and future Transaction amount
   update behavior.
6. **Goal lifecycle:** Define how funded totals affect Saving Goal status
   and `CompletedDate`, and how reversal, target changes, Goal deletion, and
   completed Goals are handled.
7. **Domain boundary:** Keep the Goal Funding capability's orchestration in
   Application. The persisted Allocation is not a navigation owned by
   Transaction or Goal. If it enforces durable funding invariants, place
   those rules in a Domain-owned funding record; otherwise explicitly
   classify it as an application-owned persistence record.
8. **Event consistency:** Do not rely on an unspecified asynchronous
   Application Event to maintain immediate consistency. Define synchronous
   same-Unit-of-Work handling for destructive/corrective operations, or
   explicitly accept eventual consistency and its recovery behavior.

### Aggregate and layer recommendation

The Goal Funding **capability** belongs in Application as the command and
query orchestration boundary; that does not by itself determine the layer of
the persisted Allocation.

For the proposed explicit, auditable ledger with validation and reversal
rules, prefer a small Domain-owned funding record with scalar Goal and
Transaction identifiers and no navigation properties. Application services
coordinate it with existing Goal/Transaction repositories and the Unit of
Work. This is a focused capability, not a requirement to create a separate
subdomain.

An independent Aggregate Root is not required merely because EF Core has a
DbSet. If an Allocation has its own identity, creation/reversal lifecycle,
and invariants, it can be treated as an independently managed aggregate
root; it must not be nested under `Goal` or `Transaction`. The
per-Transaction total-allocation limit spans multiple records, so the
Application command and persistence transaction must also enforce that
cross-record invariant under concurrency.

## 5. Database Design Prerequisites

Do not begin detailed database design until the following are accepted:

- The transitional strategy, cutover condition, legacy progress handling,
  and compatibility behavior for existing `CurrentAmount` consumers.
- Whether allocations represent earmarked progress or actual movement of
  money, eligible Transaction types, partial allocations, and the maximum
  active allocation per source.
- The Transaction creation/allocation failure contract, retry and
  idempotency expectations, and the same-Unit-of-Work boundary where
  atomicity is required.
- Soft-delete reversal and history retention, plus a policy for any future
  Transaction amount corrections.
- Goal completion, reversal, deletion, and edit behavior after progress is
  computed from the canonical funding summary.
- Same-user validation for the requesting user, source Transaction, and
  Goal; and a concurrency strategy that prevents simultaneous commands from
  exceeding the source's eligible amount.
- A compatibility plan for existing Goal Purchase classification and
  other consumers of `Transaction.GoalId`.
- Confirmation that the MVP uses an Application query/read model without a
  persisted projection unless later measurements justify one.

This proposal intentionally does not define table names, columns,
relationships, indexes, or migration scripts. Those belong in the
subsequent Database Design ADR after these domain and lifecycle decisions
are accepted.
