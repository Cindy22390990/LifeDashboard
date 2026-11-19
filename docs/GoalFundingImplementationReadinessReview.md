# Goal Funding Implementation Readiness Review

## Review Scope

Reviewed the five requested Goal Funding ADR and planning documents and
inspected the current Domain, Application, and Infrastructure code relevant
to `GoalId`, `CurrentAmount`, Goal deposit/purchase, Transaction handling,
Dashboard reads, and Unit of Work.

No ADR, plan, code, or database artifact was changed. No build, test, or
migration was run.

## 1. Domain Consistency

| Check | Result | Finding |
|---|---|---|
| Transaction remains independent of Goal | **FAIL — current code** | The target ADR boundary is clear, but `Transaction` still has `GoalId`, a Goal navigation, and `AssociateWithGoal`. `TransactionService` accepts a Goal for legacy flows, and the Budget Radar query uses `GoalId`. |
| `Goal.CurrentAmount` is not the target source of truth | **FAIL — current code** | `Goal` still stores and mutates `CurrentAmount`; deposit and Goal service mappings return it, and Dashboard obtains Goal DTOs through that service. The ADR correctly makes it deprecated, but implementation has not occurred. |
| `GoalFundingAllocation` has a clear responsibility | **PASS — architecture decision** | The ADR defines it as an independent Aggregate Root, managed through the Application capability, with identifier references only. No `GoalFundingAllocation` implementation was found in the inspected code. |
| Funding represents explicit intent | **NEED REVISION — legacy transition** | The target model requires an explicit user Allocation, but the current `GoalDepositService` creates a Transfer and calls `Goal.ApplyDeposit`. A Transfer cannot become a new Allocation under the Income-only Domain decision. The intended replacement or retirement of this legacy deposit behavior is not yet resolved. |

The Domain decision is internally consistent, but current code has not
reached that boundary. In particular, do not treat existing
`Transaction.GoalId` values or the legacy Transfer-based deposit flow as
Allocation records.

## 2. Database Readiness

| Check | Result | Finding |
|---|---|---|
| Allocation persistence direction follows the Domain ADR | **PASS — proposed design** | The Database Design ADR keeps Allocation independent, uses required identifier references, preserves validity history, and separates opening progress from user-created Allocations. |
| `CurrentAmount` transition is safe | **PASS with gates** | The proposed one-time opening-progress capture, stopped legacy writers, retry protection, and per-Goal validation avoid treating the old value as event history. The new source must remain disabled until validation is accepted. |
| Data-loss risk | **NEED REVISION before Contract** | The staged design avoids intended loss if its gates are followed. However, the opening baseline preserves only the known total, not a complete funding history. Also, Goal Purchase attribution currently depends on `Transaction.GoalId`; that classification must have an approved retained/replacement treatment before removing the relationship or its data. |
| Expand → Backfill → Validate → Cutover → Contract | **PASS — sequence** | The Migration Plan defines this order. The revised Implementation Plan maps expansion to Phase 2, backfill and validation to Phase 3, cutover to Phase 4, and retirement to Phase 5. |

The Database Design ADR and Migration Plan are both still marked
**Proposed**. The Implementation Plan requires an ADR freeze in Phase 0;
that prerequisite has not yet been satisfied by the document statuses.

## 3. Existing Code Compatibility

### Needs to be modified or replaced

- [`Goal.cs`](../LifeDashboard.Domain/Entities/Goal.cs): `ApplyDeposit`
  mutates `CurrentAmount` and completes Saving Goals from that field. Goal
  completion must instead be coordinated against the canonical funded total.
- [`GoalDepositService.cs`](../LifeDashboard.Application/Services/GoalDepositService.cs):
  it creates a Transfer, associates it with a Goal, and increments
  `CurrentAmount`. This is not compatible with an Income-only explicit
  Allocation. Its future user-visible behavior needs a decision before
  implementation.
- [`Transaction.cs`](../LifeDashboard.Domain/Entities/Transaction.cs) and
  [`TransactionService.cs`](../LifeDashboard.Application/Services/TransactionService.cs):
  the Goal navigation/`GoalId` association and Goal-accepting creation path
  preserve aggregate coupling. Transaction deletion currently rejects any
  Goal-associated Transaction; it must eventually coordinate Allocation
  invalidation in the same Unit of Work or reject deletion while active
  Allocations remain.
- [`GoalService.cs`](../LifeDashboard.Application/Services/GoalService.cs),
  [`GoalDtos.cs`](../LifeDashboard.Application/Models/DTOs/GoalDtos.cs), and
  [`DashboardService.cs`](../LifeDashboard.Application/Services/DashboardService.cs):
  Goal progress is currently mapped from `CurrentAmount`; progress reads
  must move to `GoalFundingSummary`.
- [`LifeDashboardDbContext.cs`](../LifeDashboard.Infrastructure/Data/LifeDashboardDbContext.cs):
  current EF mapping includes the Transaction–Goal relationship and
  `CurrentAmount`; new persistence and later Contract changes must follow
  the approved database decision.
- [`BudgetRadarQueryRepository.cs`](../LifeDashboard.Infrastructure/Repositories/BudgetRadarQueryRepository.cs):
  Goal Purchase and Budget Radar classification query `Transaction.GoalId`.
  Their replacement or retained classification must be ready before
  `GoalId` is removed.
- [`UnitOfWork.cs`](../LifeDashboard.Infrastructure/Data/UnitOfWork.cs)
  and [`IUnitOfWork.cs`](../LifeDashboard.Application/Interfaces/IUnitOfWork.cs):
  they provide database transaction and save/commit/rollback operations.
  The inspected code does not provide an Application notification
  dispatcher; soft-delete coordination must be implemented without
  weakening same-Unit-of-Work atomicity.

### Needs to be preserved during transition

- The financial Transaction creation, account-balance, and soft-delete
  behavior that is independent of Goal funding.
- Goal target and lifecycle behavior, while moving Saving Goal completion
  decisions to the canonical funded total.
- Goal Purchase as a spending/lifecycle concept. It is not a funding
  Allocation, and its classification must remain available until its
  dependency on `GoalId` is replaced.
- Legacy fields and consumers only for the bounded compatibility period;
  no new Goal Funding behavior may use them.
- Unit-of-Work transaction support for coordinated writes. It is not, by
  itself, a Transaction notification mechanism.

### To be retired after migration gates pass

- `Transaction.GoalId`, its Goal navigation and association method, after
  all Goal Purchase, Goal deposit, and Budget Radar consumers are migrated.
- `Goal.CurrentAmount` as stored funding state and its direct mutation path,
  after opening progress is captured and all readers/writers use the
  canonical funding source.
- The legacy Transfer-based Goal deposit path, only after its intended
  replacement or retirement is explicitly resolved.
- Compatibility mappings and code only after the new source is validated
  and the audit history is complete. Any retained external compatibility
  value must be projected from the read model, not independently stored.

## 4. Implementation Phase Validation

| Check | Result | Finding |
|---|---|---|
| Circular dependency | **PASS** | The revised phase graph is linear from Preparation through Legacy Retirement. Phase 3 prepares and validates data before Phase 4 cutover; it does not depend on cutover. |
| Missing prerequisite | **NEED REVISION** | Phase 0 requires an ADR freeze, but the Database Design ADR and Migration Plan remain Proposed. Also, the legacy Transfer-based Goal Deposit behavior has no defined compatible replacement under the Income-only Allocation model. |
| Hidden migration dependency | **NEED REVISION** | Phase 3 requires a candidate Goal Funding read result to generate its validation report, but the phases do not explicitly identify where the shadow/read-only query used by that report is implemented and verified. It must exist before Phase 3 validation, while remaining disabled as the Production Dashboard source. |
| Premature removal | **PASS — with stated gates** | Phase 5 defers compatibility cleanup and physical column/relationship removal until after cutover, consumer retirement, and audit checks. Goal Purchase attribution remains a specific prerequisite for removing `GoalId`. |

The circular dependency is resolved. Readiness is still blocked by
prerequisite and compatibility gaps, not by phase ordering.

## 5. MVP Scope Validation

| Scope item | Result | Finding |
|---|---|---|
| Transaction creation | **PASS** | Existing financial Transaction creation remains a separate operation; it must not automatically create an Allocation. |
| Explicit `GoalFundingAllocation` | **PASS — target scope** | The MVP is an explicit user designation against an eligible Income Transaction. The current Transfer-based Goal deposit is a legacy behavior, not an eligible source. |
| `GoalFundingSummary` read model | **PASS — target scope** | The summary is calculated from opening progress plus active Allocations; no persisted summary table is required. |
| Dashboard reads the summary | **PASS — after cutover** | The revised plan gates Dashboard activation on successful backfill validation. |
| Automatic Funding Rule | **Excluded** | Consistent with the ADRs and Implementation Plan. |
| AI Allocation | **Excluded** | Consistent with the ADRs and Implementation Plan. |
| Account Bucket | **Excluded** | Consistent with the ADRs and Implementation Plan. |
| Investment Valuation | **Excluded** | Consistent with the ADRs and Implementation Plan. |

## 6. Final Decision

**Architecture Status: B. NEED REVISION**

Before implementation begins:

1. Complete Phase 0 ADR freeze; the Database Design ADR and Migration Plan
   are still Proposed.
2. Resolve how the current Transfer-based `GoalDepositService` behavior is
   replaced or retired. Do not reinterpret a Transfer as an Income
   Allocation.
3. Identify and verify the shadow Goal Funding query used for Phase 3
   validation, and approve a replacement/retention path for Goal Purchase
   attribution before Contract removes `Transaction.GoalId`.

The phase cycle is resolved and the target MVP scope is consistent. These
remaining prerequisites prevent classifying the architecture as ready for
implementation.
