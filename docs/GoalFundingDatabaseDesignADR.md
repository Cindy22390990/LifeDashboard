# Goal Funding Database Design ADR

## Status

**Proposed**

This ADR translates the fixed Domain decisions in
[GoalFundingDomainModelADR.md](./GoalFundingDomainModelADR.md) into a
relational persistence proposal. It does not reopen those decisions and
does not represent an implemented schema or migration.

## 1. Scope and Current Persistence Baseline

The Domain ADR fixes the following constraints:

- `GoalFundingAllocation` is an independent Domain Aggregate Root with no
  Goal or Transaction navigation properties.
- New funding entries are explicit, Income-only purpose designations; they
  do not move or reserve Account funds.
- Legacy Goal progress is migrated as identified opening progress, not
  fabricated Transaction history.
- Invalidated Allocations remain auditable; the MVP
  `GoalFundingSummary` is calculated on read and is not persisted.

The current EF Core model and migration history show:

- `Goals.Id` is an `int` identity key. `Goals.CurrentAmount` is currently a
  non-null `decimal(18,2)` column.
- `Transactions.Id` is a `uniqueidentifier`; `Transactions.GoalId` is a
  nullable `int` FK to `Goals.Id`, with `ON DELETE SET NULL` and an index.
- Transactions have `IsDeleted`, `DeletedAt`, and `DeletedReason`; the EF
  model applies a query filter for non-deleted transactions.
- The repository contains `InitialCreate` and
  `AddTransactionSoftDelete` migrations, and the current model snapshot
  still includes `CurrentAmount` and `GoalId`.
- This is the repository migration state only; it does not establish which
  migrations have been applied to any live database.

## 2. Table Design

### Existing `Goals`

Keep the existing table during the compatibility phase. Do not add a
funding summary column. The legacy `CurrentAmount` column remains until the
cutover and retirement conditions in the Domain ADR are met, then is
removed.

Add a unique alternate key/index on `(UserId, Id)` so dependent records can
enforce same-user ownership with composite foreign keys. `Id` remains the
existing primary key.

### New `GoalFundingAllocations`

Store explicit user-created allocations only:

- Allocation identifier
- `UserId`
- Required `GoalId`
- Required `TransactionId`
- `Amount` as `decimal(18,2)`
- Current validity state (`IsActive`)
- Creation timestamp
- Required idempotency key for the explicit create command

Every row in this table is an explicit Allocation against an eligible
Income Transaction. Do not store migration opening progress as an
Allocation with a missing or invented Transaction reference.

Use a database-generated `bigint` primary key for Allocation rows. The
Domain Aggregate Root remains independent; the database key choice does
not create an aggregate relationship.

### New `GoalFundingAllocationStatusHistory`

Keep an append-only record of Allocation validity transitions, including
invalidation on source Transaction soft delete and reactivation on
Transaction restore. Each history record references one Allocation and
records the transition kind and occurrence time. This is a funding audit
record, not a general application event bus.

`IsActive` on the Allocation is the current query state; the history table
preserves prior transitions. Do not hard-delete an Allocation or its status
history.

### New `GoalFundingOpeningProgress`

Store the one-time migration opening-progress value separately from
user-created Allocations:

- One opening-progress entry per Goal
- `UserId` and required `GoalId`
- The captured legacy progress amount as `decimal(18,2)`
- Migration provenance and capture timestamp

The entry is immutable after cutover and is not linked to a fabricated
Transaction. The Goal Funding read query adds this opening progress to the
Goal's active Allocation total.

### `GoalFundingSummary`

Do **not** create a `GoalFundingSummaries` table for the MVP. Calculate
`FundedAmount` on read from:

1. The Goal's opening-progress entry, if present.
2. Its active explicit Allocations.

The Dashboard query starts from all user Goals and left-joins/aggregates
these sources so Goals with no opening progress or Allocations return zero.
A persisted projection requires a separate decision and a measured
performance or availability need.

## 3. Foreign-Key and Delete Strategy

| Dependent data | FK | Required? | Delete behavior |
|---|---|---:|---|
| Allocation → Goal | `(UserId, GoalId)` → `Goals(UserId, Id)` | Yes | Restrict / No Action |
| Allocation → source Transaction | `(UserId, TransactionId)` → `Transactions(UserId, Id)` | Yes | Restrict / No Action |
| Allocation status history → Allocation | `AllocationId` → Allocation primary key | Yes | Restrict / No Action |
| Opening progress → Goal | `(UserId, GoalId)` → `Goals(UserId, Id)` | Yes | Restrict / No Action |

Add unique alternate indexes on `(UserId, Id)` for Goals and Transactions
as needed by the composite FKs. This prevents an Allocation from referring
to a Goal or Transaction owned by a different user, even if the caller
supplies mismatched identifiers.

The Allocation Transaction FK is required because only explicit Income
Allocations are stored there. A legacy opening-progress entry has no
Transaction FK and is stored in its separate table.

An Allocation reference does not add an EF navigation to Transaction or
Goal. Configure the relationships without navigation expressions; do not
add inverse collections to either existing Entity.

Transaction soft delete does not delete the referenced row, so the FK
remains valid. The Application notification handler must invalidate the
Allocation and append its status-history record in the same Unit of Work as
the soft delete. Hard deletion of a referenced Transaction or a Goal with
funding history is restricted. User deletion/cascading financial-history
retention remains governed by the application's existing user-retention
policy and must not cascade-delete funding records implicitly.

Income eligibility cannot be guaranteed by a simple FK. The Application
command must validate that the source is an Income Transaction owned by the
same user and is not soft-deleted, within the serialized database
transaction that checks the allocation cap.

## 4. Index and Concurrency Strategy

Create indexes to support the known read/write paths:

- **All Goal progress for one user:** unique `Goals(UserId, Id)` plus a
  filtered `GoalFundingAllocations(UserId, GoalId)` index for active rows,
  including `Amount`.
- **Allocation cap for one source Transaction:** filtered active-row index
  on `GoalFundingAllocations(UserId, TransactionId)`, including `Amount`.
- **Soft-delete/restore fan-out:** the same Transaction lookup index supports
  finding active Allocations by source Transaction.
- **Idempotent create:** unique index on `(UserId, IdempotencyKey)`.
- **Opening progress:** unique `(UserId, GoalId)` index, enforcing at most
  one migration opening entry per Goal.
- **Allocation audit history:** `(AllocationId, OccurredAtUtc)` index for
  ordered transition history.

Do not add an `IsDeleted` field to Allocations; invalidation and
reactivation use the Domain-approved validity lifecycle. Filtered indexes
are SQL Server-specific and should be represented explicitly in EF mapping.

The active-allocation sum must remain at or below the source Transaction
amount. A check constraint cannot enforce a sum across multiple rows.
Serialize Allocation creation and future Transaction amount changes by
locking the source Transaction row inside the same database transaction
(SQL Server `UPDLOCK`/`HOLDLOCK`), then validate the sum and persist the
change. Every writer of this invariant must use the same locking protocol.
The unique idempotency index prevents duplicate allocations on retried
commands; it does not replace the amount-cap check.

## 5. Migration Strategy

Use an expand/cutover/contract sequence. No migration is created by this
ADR.

### Phase 0

- No schema or data changes.
- Keep `Goals.CurrentAmount`, `Transactions.GoalId`, and current behavior
  unchanged.

### Expand

- Add the Allocation, Allocation status-history, and opening-progress
  persistence described above.
- Keep the legacy columns and FK in place.
- Deploy Application code that can use the new funding history, but keep the
  new summary read/write path disabled until the opening-progress snapshot is
  complete.
- Ensure no new feature writes Goal Funding through `Transaction.GoalId`.
- Do not add a persisted Goal Funding summary.

### Cutover and backfill

- Drain or disable all legacy Goal-progress writers before taking the
  opening snapshot. Coordinate deployment so an old writer cannot race the
  snapshot.
- Copy each Goal's current value once into its immutable opening-progress
  entry. Use an idempotent migration marker/unique Goal constraint so a
  retry cannot create a duplicate opening entry.
- Do not backfill pre-cutover Transactions as Allocations. The opening
  progress represents the complete pre-cutover Goal progress; importing
  those Transactions as well would double count.
- Enable the new Goal Funding read/write path only after the snapshot is
  complete, so the Dashboard never reads an incomplete opening-progress
  baseline.
- Keep legacy `Transaction.GoalId` only for remaining non-funding consumers,
  including Goal Purchase classification, until those consumers have a
  replacement and are retired.
- Validate the read result as opening progress plus post-cutover active
  Allocations before removing legacy data.

### Contract

- After all Goal progress readers and writers use the new read/write path,
  remove the `Goals.CurrentAmount` column. Any required API compatibility
  value is projected from the read model, not stored as a second source.
- Remove `Transactions.GoalId`, its FK, index, and navigation only after
  Goal deposit/purchase and Budget Radar consumers no longer depend on it.
- Retain opening-progress and Allocation history; do not delete them as part
  of legacy-column cleanup.

### Rollback

Before any post-cutover Allocation writes, the additive schema can be
rolled back after confirming no new records need preservation. After
Allocations or opening-progress entries become authoritative, do not run a
destructive Down migration that loses them or writes stale values back to
`CurrentAmount`; use a forward corrective migration or restore from an
explicitly reconciled backup.

## Consequences

- Goal progress has one post-cutover source: opening progress plus valid
  Allocation history.
- Transaction and Goal remain independent in the EF model; relationships
  are represented by scalar foreign keys without navigation properties.
- The MVP avoids a synchronized summary table and derives the Dashboard
  summary at query time.
- Same-user composite FKs, amount-cap locking, idempotency, and lifecycle
  history add indexes and write-path requirements that must be implemented
  together in a later migration and Application change.

## Decision Status

**Proposed — not implemented.**

No Entity, EF mapping, schema, migration, or code was changed by this ADR.
