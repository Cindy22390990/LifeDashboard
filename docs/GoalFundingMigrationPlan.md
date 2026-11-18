# Goal Funding Migration Plan

## Status

**Proposed — not implemented.**

This plan operationalizes the decisions in
[GoalFundingDomainModelADR.md](./GoalFundingDomainModelADR.md) and
[GoalFundingDatabaseDesignADR.md](./GoalFundingDatabaseDesignADR.md). It
does not define a schema, SQL script, EF migration, or API design.

## 1. Migration Context

### Legacy model

- `Goal.CurrentAmount` stores the current Goal progress, but does not
  preserve how that progress was funded.
- `Transaction.GoalId` associates a Transaction with a Goal for legacy
  behaviors. It does not establish that the Transaction's amount, or any
  portion of it, was explicitly allocated to that Goal.
- Existing Goal deposit, purchase-classification, and Dashboard consumers
  may still rely on these fields and must be migrated before the fields are
  removed.

### Target model

- `GoalFundingAllocation` records a user's explicit designation of some or
  all of an eligible Income Transaction to a Goal. It is not a transfer,
  account balance, or inferred relationship.
- `Legacy Opening Progress` preserves each Goal's pre-cutover
  `CurrentAmount` as a separately identified baseline. It is not an
  Allocation and does not invent a source Transaction.
- `GoalFundingSummary` is calculated from opening progress plus active
  Allocations. It is the post-cutover read model, not a separately persisted
  source of truth.

Migration is bounded: the legacy fields may remain physically during the
transition, but they must not be maintained as parallel funding sources.

## 2. Data Classification Strategy

### A. Historical data convertible to Allocation

**None of the current legacy fields or relationships qualify.**

The current model has no independent record of an explicit allocation
amount. A historical `Transaction.GoalId`, even when it points to an
Income Transaction, is not proof of explicit funding intent or of the
amount assigned. Therefore:

- Do not convert pre-cutover Transactions or `GoalId` values into
  `GoalFundingAllocation` records.
- Create Allocations only from explicit post-cutover user actions.
- Any newly discovered authoritative historical allocation source would
  require a separately approved reconciliation decision; it must not be
  inferred or included in this migration by default.

### B. Data preserved as a legacy reference or opening progress

- Copy each Goal's `CurrentAmount` once into its identified Legacy Opening
  Progress entry. This preserves the known pre-cutover total without
  claiming an event-level funding history.
- Retain `Transaction.GoalId` only for identified legacy non-funding
  consumers, such as Goal Purchase classification, until those consumers
  have an approved replacement.
- Do not include `Transaction.GoalId` in the post-cutover funding
  calculation.

### C. Data whose funding meaning cannot be derived

The following cannot be reconstructed from `CurrentAmount`,
`Transaction.GoalId`, Transaction type, amount, description, or account
movement alone:

- Whether a Transaction was intentionally designated as Goal funding.
- The portion of a Transaction assigned to a Goal, including splits across
  multiple Goals.
- Whether a Transfer represents Goal funding; Transfers are not eligible
  funding sources.
- A complete event history explaining the existing `CurrentAmount`.

Do not fabricate Allocations or distribute amounts to make totals appear to
match. Preserve the known total as opening progress and keep unrelated
legacy classification separate.

## 3. Migration Sequence

The required sequence is:

**Expand → Backfill → Dual Read Validation → Cutover → Contract**

| Stage | Writes | Reads | Source of Truth |
|---|---|---|---|
| Expand | Existing legacy Goal-progress writes may continue. New Allocation writes remain disabled. | Existing consumers continue reading legacy fields. New code may be deployed in a disabled/shadow-capable state. | `Goal.CurrentAmount` for existing Goal progress; `Transaction.GoalId` only for identified legacy behaviors. |
| Backfill | Drain or disable legacy Goal-progress writers while capturing the snapshot. The migration process writes opening-progress entries only; it creates no Allocations. | Legacy values remain available; the candidate opening-progress result may be inspected without serving as the user-facing result. | Frozen `Goal.CurrentAmount`. |
| Dual Read Validation | Keep legacy Goal-progress writes and new Allocation writes disabled until validation completes. Unrelated Transaction activity may continue only if it cannot change Goal progress through the legacy path. | Compare the legacy progress with the candidate read from opening progress. Do not expose an incomplete candidate result as the Dashboard result. | `Goal.CurrentAmount` remains authoritative; the new result is shadow-only. |
| Cutover | Enable explicit Allocation writes. Stop all writes to `Goal.CurrentAmount`. | Switch Goal-progress readers to `GoalFundingSummary`; do not read `CurrentAmount` as funding state. Keep `Transaction.GoalId` only for remaining non-funding legacy consumers. | Opening progress plus active `GoalFundingAllocation` records. |
| Contract | No writes to either retired field. | No funding reader uses `CurrentAmount`; no active consumer uses `Transaction.GoalId` before its removal. | Opening progress plus active Allocations remains the sole funding source. |

### Expand

- Add the persistence capability defined by the Database Design ADR while
  preserving legacy fields and behavior.
- Deploy only code that is safe to operate before cutover. Keep new summary
  reads and Allocation writes disabled.
- Prohibit new features from using `Transaction.GoalId` to create Goal
  Funding.

### Backfill

- Drain or disable all operations that write `Goal.CurrentAmount` before
  taking the snapshot. Coordinate deployment so no legacy writer can race
  or overwrite the captured value.
- Capture each Goal's current value once as Legacy Opening Progress,
  retaining migration provenance and capture time.
- Do not backfill legacy Transactions as Allocations.
- Keep the new Goal Funding read/write path disabled until the backfill is
  complete.

### Dual Read Validation

- Calculate a shadow Goal progress result from opening progress and active
  Allocations; before cutover, there should be no migrated historical
  Allocations.
- Compare the candidate result with the frozen legacy `CurrentAmount` for
  every Goal, including Goals with zero progress.
- Investigate and reconcile every unexplained difference. Do not hide
  discrepancies through rounding, inferred Allocations, or silent
  fallbacks.
- Keep `CurrentAmount` as the authoritative result until validation passes
  and cutover is explicitly approved.

### Cutover

- Switch all Goal-progress reads to `GoalFundingSummary` and enable explicit
  Allocation commands only after the opening-progress snapshot and
  validation are complete.
- Treat opening progress as immutable. New funding changes are represented
  by explicit Allocation lifecycle operations, not writes to
  `CurrentAmount`.
- Keep any remaining `Transaction.GoalId` behavior isolated to its
  identified non-funding legacy consumers.

### Contract

- Remove `Goal.CurrentAmount` only after no reader or writer depends on it
  and the post-cutover read path is verified.
- Remove `Transaction.GoalId` and its Goal relationship only after all
  Goal deposit/purchase and Budget Radar consumers have been migrated.
- Retain opening progress and Allocation history. Contracting legacy fields
  must not delete or rewrite the new funding history.

## 4. Legacy Field Retirement

| Field | Stop using for new funding | Stop writing | Safe to remove |
|---|---|---|---|
| `Goal.CurrentAmount` | Immediately for new features; the new summary must not read it as funding state. | Before the opening-progress snapshot. Keep it frozen for dual-read validation, then do not write after cutover. | After cutover validation and once every Goal-progress reader and writer uses the new read/write path. |
| `Transaction.GoalId` | Immediately as a Goal Funding relationship. New Allocations do not depend on it. | Stop writes from any Goal Funding behavior immediately. Legacy non-funding flows may continue only until their replacement is ready; then stop all writes. | After Goal deposit/purchase classification and Budget Radar consumers no longer depend on it, and all remaining readers/writers are retired. Remove its relationship only as part of the approved contract phase. |

Physical coexistence during the transition is temporary. Do not dual-write
legacy progress and Allocations for the same funding action. Long-term
coexistence of `Transaction.GoalId` as a funding link and
`GoalFundingAllocation` is prohibited.

## 5. Failure Handling

### Migration interruption

- Do not advance to the next stage until the current stage's completion
  checks pass.
- If interruption occurs before cutover, keep the legacy path authoritative
  and the new read/write path disabled.
- Preserve completed backfill records and migration provenance so recovery
  can determine which Goals were processed.
- After cutover or any authoritative Allocation writes, do not use a
  destructive rollback or copy stale values back into `CurrentAmount`.
  Recover through a forward correction or an explicitly reconciled backup.

### Backfill rerun

- Make the opening-progress capture restartable and idempotent, with at
  most one opening entry per Goal.
- A retry must recognize already-captured Goals and must not overwrite
  their captured baseline with a later or stale value.
- Resume only after confirming the legacy Goal-progress writers remain
  stopped; do not take a second snapshot over changed data.

### Duplicate Allocation creation

- Allocation creation must use the idempotency behavior defined by the
  Database Design ADR. Retrying the same command must not create another
  Allocation.
- A duplicate request must be resolved by its command identity and existing
  result, not by guessing from Goal, Transaction, or amount.
- Idempotency does not replace the serialized check that active Allocation
  totals do not exceed the source Income Transaction amount.

### Data inconsistency

- Do not cut over while any Goal has an unexplained difference between
  frozen legacy progress and the candidate opening-progress result.
- Do not repair discrepancies by inventing Allocation history or silently
  changing the legacy value.
- Record the affected Goals, the evidence reviewed, and the approved
  reconciliation outcome before retrying validation.
- If inconsistency is discovered after cutover, preserve the authoritative
  funding history and apply a forward correction; do not resume legacy
  writes as a second source of truth.

## 6. Acceptance Criteria

Implementation may begin only when all of the following are satisfied:

- The Domain and Database Design ADR decisions remain approved and
  consistent; no migration step changes Allocation semantics.
- Every legacy reader and writer of `CurrentAmount` and `Transaction.GoalId`
  is inventoried, including Goal lifecycle and Budget Radar consumers.
- The cutover can stop and drain legacy Goal-progress writers, and a
  responsible operator can verify that the snapshot is stable.
- Every Goal receives either a verified opening-progress entry or an
  explicitly documented, approved exception; no historical Transaction is
  inferred to be an Allocation.
- Backfill can be safely retried without duplicate or overwritten opening
  progress.
- Dual-read validation covers every Goal, including zero-progress Goals,
  and has no unexplained differences before cutover.
- The Allocation write path is idempotent and enforces the active amount
  limit under the Database Design ADR's concurrency strategy.
- The new read path returns progress from opening progress plus active
  Allocations, and does not use `CurrentAmount` as a fallback.
- Cutover and recovery procedures preserve new funding history and do not
  restore `CurrentAmount` as a competing source of truth.
- Retirement of each legacy field is gated on verified removal of all its
  readers and writers.

This plan defines migration sequencing and data treatment only. It does not
authorize Entity, DbContext, schema, API, or migration-code changes.
