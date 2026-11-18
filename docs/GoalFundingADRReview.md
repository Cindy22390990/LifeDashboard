# Goal Funding ADR Review

## Review Result

**Need Revision**

The proposed direction is workable as a future Application capability, but it
does not yet align with the existing Goal/Transaction model. In particular,
`Transaction` currently has a `GoalId` foreign key and `Goal` navigation,
`Goal.CurrentAmount` is persisted and drives Saving Goal completion, and the
existing Goal use cases coordinate those fields with transaction creation.
The new ADR needs a compatibility and lifecycle boundary before it is ready
to drive database design.

No `docs/Phase1TransactionAtomicity*` document was found. This review also
considered the current Unit of Work implementation and
[phase2-goal-transaction-boundary.md](./phase2-goal-transaction-boundary.md).

## Architecture Alignment

### Consistent with the current architecture

- Application-level orchestration is a reasonable home for a Goal Funding
  capability. The Application project references Domain, while Infrastructure
  references both Application and Domain. Existing services use Application
  repository and Unit of Work interfaces rather than depending directly on
  Infrastructure implementations.
- A funding record can keep scalar `GoalId`, `TransactionId`, and `UserId`
  identifiers without adding CLR navigation properties to either aggregate.
  EF Core can map foreign keys without exposing navigations; an explicit
  `WithMany()` mapping with no navigation expression does not require a
  collection property on `Transaction` or `Goal`.
- The existing Dashboard query is already composed in Application: `DashboardService`
  obtains Goals through `IGoalService` and combines them with other summary
  data. Query-specific aggregates also exist, for example
  `BudgetRadarQueryRepository` returning a compact aggregate. A
  `GoalFundingSummary`/`GoalProgressDto` is therefore compatible with the
  current read-model pattern.
- The current `UnitOfWork` can wrap multiple writes in one database
  transaction. `GoalDepositService` and `GoalPurchaseService` use that pattern
  to coordinate transaction/account updates with Goal state updates.

### Existing conflicts and coupling

- `Transaction` currently contains `GoalId`, a `Goal` navigation, and
  `AssociateWithGoal(Goal)`. EF mapping configures the `Transaction.GoalId`
  relationship. This directly conflicts with the proposed rule that the
  Transaction Domain must not know Goal. `Goal` itself has no Transaction
  navigation, so the existing dependency is one-way, not fully bidirectional.
  See [Transaction.cs](../LifeDashboard.Domain/Entities/Transaction.cs) and
  [LifeDashboardDbContext.cs](../LifeDashboard.Infrastructure/Data/LifeDashboardDbContext.cs).
- Application coupling also exists: `ITransactionService` exposes
  `CreateTransactionWithinExistingTransactionAsync(..., Goal goal)`, and
  `TransactionService` associates the Goal with the transaction. This is not
  an Application-to-Infrastructure dependency inversion violation, but it
  conflicts with keeping generic transaction creation independent of Goal.
  The prior boundary review identified this feature-specific coupling in
  [phase2-goal-transaction-boundary.md](./phase2-goal-transaction-boundary.md).
- `Goal.CurrentAmount` is currently a persisted Domain property, returned by
  `GoalService` and used by `Goal.ApplyDeposit` to complete Saving Goals.
  `GoalDepositService` changes it in the same Unit of Work as the transfer.
  Removing it from the aggregate changes both persistence and the existing
  Goal lifecycle; a read model alone does not preserve the current completion
  rule.
- The existing Budget Radar query also reads `Transaction.GoalId` to
  distinguish Goal Purchase expenses. Removing or repurposing that field
  affects existing query semantics, not just Goal Funding.
- No Application event/notification infrastructure was found. The proposed
  notification strategy would be new architecture, not a reuse of an
  existing event bus.

### Aggregate and repository boundary

- The no-navigation rule is feasible, but must be explicit in both the
  allocation model and EF configuration: no `Transaction.FundingAllocations`,
  no allocation-to-Transaction navigation, and no Goal navigation. A
  relational FK may still exist; the FK does not itself create a CLR
  navigation.
- The Application capability can coordinate separate repositories by scalar
  IDs. It need not load a Goal aggregate through a Transaction navigation or
  add cross-aggregate methods to `IGoalRepository` or
  `ITransactionRepository`.
- If allocations are persisted, an allocation repository port is needed,
  such as `IGoalFundingAllocationRepository`, for creation and the required
  summary query. Keep this to the allocation capability; do not add a
  repository per relationship or an aggregate repository spanning Goal and
  Transaction. A grouped read query may be exposed by the same port initially.
- Clarify where the persisted allocation record belongs. Application
  orchestration belongs in Application; the record's ownership must follow
  the chosen boundary. If the record enforces durable business invariants,
  define it as a funding ledger model with those invariants, not merely as a
  DTO or an unspecified “Application Concept.”

## Identified Risks

1. **Legacy relationship and compatibility:** Existing Goal deposits and
   purchases use `Transaction.GoalId`; deletion of Goal-linked transactions
   is currently rejected. Removing this relationship without a transition
   plan would change existing behavior and the Budget Radar query. Existing
   `Goal.CurrentAmount` and linked transactions do not necessarily provide
   enough evidence to reconstruct a complete allocation ledger.
2. **CurrentAmount and lifecycle divergence:** Saving Goal completion is
   based on `CurrentAmount`. If the amount becomes a read model, the ADR must
   specify how Goal status and completion date are changed, and what happens
   when funding is reversed or reduced. Purchase Goal completion currently
   follows a separate purchase flow.
3. **Separate commands are not atomic:** The proposed sequence creates a
   Transaction first and an Allocation in an independent action. If
   Transaction creation fails, no allocation should be possible. If
   Allocation creation fails, the already-committed Transaction remains.
   This is acceptable only if the product treats allocation as an optional,
   separately retryable action; it is not an atomic “fund Goal” operation.
   Retries must not create duplicate transactions or allocations.
4. **Soft delete and stale funding:** Transaction deletion is currently a
   soft delete and is rejected only when `Transaction.GoalId` is set. A new
   allocation that uses only `TransactionId` would not automatically prevent
   deletion. The soft-deleted transaction remains in the database, and an
   allocation-only sum could continue to count it unless the read query or
   allocation lifecycle explicitly handles it.
5. **Amount changes:** There is currently no Transaction amount-update
   service/API path, and `Amount` has a private setter. If amount changes are
   later added, existing allocations cannot silently remain valid when
   their total exceeds the revised eligible amount. Reject, reverse, or
   compensate according to an explicit policy.
6. **Concurrent over-allocation and duplicate commands:** If one Transaction
   may fund multiple Goals or have partial allocations, concurrent requests
   can exceed its eligible amount. The persistence design must define
   transaction-level serialization/concurrency and idempotency; a simple
   per-row check is not sufficient for a sum across allocation rows.
7. **Ambiguous eligibility and semantics:** The ADR must identify which
   Transaction types may be allocated, whether allocations are a purpose
   ledger or represent actual cash movement, whether Purchase expenses can
   be funded, and whether allocations affect Account balances. Without this,
   `FundedAmount` can double-count transfers or imply cash is reserved.
8. **Goal lifecycle and user ownership:** Define behavior for allocation to
   another user's Goal/Transaction, deleted or completed Goals, edits to
   Goal type/target, zero or negative amounts, and historical records.

## Recommended Changes

- State explicitly that the proposal is a future model and does not remove
  the existing Goal-linked transaction behavior until a compatibility
  decision is approved. The Database Design ADR should include a legacy
  transition/read-compatibility policy; do not assume `CurrentAmount` can be
  backfilled from `GoalId` transactions.
- Keep generic `TransactionService` unaware of Goal in the target design.
  Put any combined funding orchestration in a Goal Funding Application
  capability that coordinates existing transaction/account operations and
  allocation persistence by IDs.
- Decide whether “create Transaction” and “create Allocation” are
  intentionally independent user actions or one atomic use case:
  - If independent, document that Transaction success is retained when
    Allocation fails, and make Allocation retryable/idempotent.
  - If one atomic funding action, execute both writes under the same
    `IUnitOfWork`/database transaction. The current caller-owned transaction
    method is a convention that does not verify a transaction is active.
- Before removing `Goal.CurrentAmount`, specify how Saving Goal completion
  and `CompletedDate` are maintained from funding totals, and how reversal
  affects them. Keep the existing Goal API field compatible during any
  transition if clients still consume it.
- Define Transaction soft-delete and future amount-edit behavior for existing
  allocations. For strong consistency, handle invalidation synchronously in
  the same database transaction or use explicit compensating allocation
  records; do not assume an asynchronous notification gives atomic behavior.
  Avoid introducing a general event bus for this MVP.
- Define eligible Transaction types, allocation amount limits, partial and
  multiple-Goal allocation semantics, and whether a transfer is an actual
  movement or only a source reference. Ensure the same money is not counted
  both as an Account balance and as a separate Goal asset.

## Implementation Constraints

- Preserve the Domain boundary: `Transaction` must not acquire Goal Funding
  navigation properties, and `Goal` must not acquire Transaction
  navigation/properties. Keep IDs scalar in the allocation record; configure
  any EF foreign keys without CLR navigation properties.
- Keep validation and orchestration in an Application capability. Validate
  that Goal, Transaction, and caller belong to the same user; use existing
  user-scoped repositories where possible. Do not infer allocation from
  transaction type, amount, account, or note.
- Use one allocation persistence port for allocation writes and an initial
  grouped summary query. Use `IGoalRepository`/`ITransactionRepository` for
  aggregate ownership and eligibility; do not create cross-aggregate
  navigation queries.
- For MVP, calculate `GoalFundingSummary`/`GoalProgressDto` as an Application
  read model from allocation records, grouped by Goal. The existing
  `DashboardService` composition pattern and query-specific aggregate
  repositories support this. No Projection Table is warranted until a
  measured performance or consistency requirement justifies one.
- Preserve `Goal.CurrentAmount` and its API/persistence semantics until a
  migration, legacy-data reconciliation, Goal completion policy, and
  compatibility plan are approved. Do not dual-write two independent
  sources of truth without reconciliation rules.
- Define soft-delete invalidation, allocation reversal, and concurrent
  allocation limits before enabling users to rely on `FundedAmount`.
- The listed MVP exclusions—automatic allocation, AI suggestions, account
  buckets, and investment valuation—are appropriate. Add explicit MVP
  boundaries for eligible Transaction types, partial/multiple allocations,
  failed-command retry/idempotency, deletion/reversal, Goal lifecycle,
  ownership, concurrency, and legacy data.
