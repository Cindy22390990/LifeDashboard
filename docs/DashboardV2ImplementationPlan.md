# Dashboard V2 Implementation Plan

## Status

Draft roadmap. The approved Phase 0 Dashboard frontend implementation is
complete; this plan does not authorize future API, Domain, Privacy Mode,
Spendable Amount, or Goal funding work.

## Purpose

This document proposes an implementation roadmap for Dashboard V2. It
describes scope, sequencing, dependencies, risks, and validation; it does not
authorize or perform implementation.

## Phase 0 Implementation Completion Record

The approved Dashboard V2 frontend UI scope has been implemented and
validated using the existing Dashboard summary data:

- Financial Overview displays today's remaining budget allowance.
- Budget Radar Summary no longer repeats today's remaining allowance.
- Recent Activity collapses by default on mobile.
- Account Details collapses by default on mobile.
- Goal Summary improves long-name wrapping on mobile while retaining all
  existing Goals.
- Added the shared `useMobileViewport` hook.
- Updated the `dashboardFormatting` helper.
- Removed the legacy `BudgetRadarPanel` component and its stylesheet.

The implementation was frontend-only. No API, DTO, Domain, Infrastructure,
or database migration changes were made.

### Phase naming clarification

- In [DashboardV2Phase0ApprovedScope.md](./DashboardV2Phase0ApprovedScope.md),
  “Phase 0” refers to the approved product scope and its boundary.
- The phases in this implementation plan describe the implementation
  workflow: the product-decision phase is complete, and the frontend
  information-architecture work is the implementation phase.
- In
  [DashboardV2Phase0CompletionChecklist.md](./DashboardV2Phase0CompletionChecklist.md),
  “complete” means the approved product scope has been implemented and its
  stated validation has been completed.

These uses of “Phase 0” describe related but distinct milestones.

The current approved Phase 5 API contract remains authoritative until the
proposed [Dashboard V2 API Contract](./DashboardV2ApiContract.md) and any
related decisions are approved. Privacy Mode and Spendable Amount remain
blocked by their respective Proposed ADRs.

## Current State Analysis

### Current Dashboard architecture

- `DashboardController` exposes the authenticated, read-only
  `GET /api/dashboard/summary` route and requires a `currentDate` query
  value.
- `DashboardService` composes the Budget Radar read model, account and Goal
  data, calendar-month income and expense totals, and up to five recent
  transactions into `DashboardSummaryReadModel`.
- `DashboardSummaryDto` returns `Budget`, `Accounts`, `Goals`, `TotalAssets`,
  `MonthlyIncome`, `MonthlyExpense`, and `RecentTransactions`.
- The Dashboard response reuses `RadarStateDto` for its nested `Budget`.
  The separate `GET /api/budget/radar` route returns the same existing
  Budget Radar read model.
- `BudgetRadarService` obtains the user's Payday setting, selects the
  applicable Payday period, reads existing income, expense, Goal purchase,
  and Fixed Commitment data, and assembles the current `RadarStateDto`.
- Dashboard monthly totals use the calendar month containing `currentDate`;
  Budget Radar uses the Payday payroll period containing that date.

### Current frontend components

- `DashboardPage.tsx` loads the existing Dashboard summary and composes
  `FinancialOverview`, `BudgetRadarSummary`, `GoalSummary`, `RecentActivity`,
  `AccountDetails`, and `QuickAddEntry`.
- The existing presentation components read the Dashboard summary; the
  Budget summary uses `summary.budget` without a separate request.
- Total Assets masking is local Dashboard UI state. The API continues to
  return its existing value.
- Goal Summary displays all existing Goals. Mobile presentation must not
  truncate this list to Top 3.
- The approved mobile decision is to keep Recent Activity and Account
  Details available but collapsed by default; this behavior is implemented
  in the current frontend.
- Existing stores and API services provide the current response data. Their
  responsibilities should remain unchanged unless an approved contract
  requires additional data.

### Existing limitations

- The page gives many information groups similar visual weight.
- Account, calendar-month, and Payday-period values can be mistaken for
  equivalent measures if their meanings and periods are not explicit.
- Sensitive monetary values are rendered directly; the current API also
  returns those values.
- Hiding Total Assets alone would not hide account balances or other
  financial values.
- `Budget.RemainingLivingBudget` is a Payday-period budget value, not
  Spendable Amount.
- The Domain Model does not represent a complete reservation concept.
  Goals, Fixed Commitments, Allocation Ratio, transactions, and balances
  cannot currently provide a reliable Spendable Amount.
- Fixed Commitments participate in current Budget Radar processing but
  commitment details are not part of `RadarStateDto`.

## Implementation Principles

1. **Preserve existing financial semantics.** Do not change the meaning of
   Total Assets, monthly totals, Goal progress, or any Budget Radar field.
   Clearly distinguish calendar-month totals from Payday-period values.
2. **Do not introduce Spendable Amount yet.** Do not add a placeholder,
   rename an existing value, or calculate it from balances, Goals,
   commitments, ratios, transactions, or Budget Remaining before
   [ADR-0001](./ADR-0001-SpendableAmount.md) is resolved and an approved
   contract exists.
3. **Do not duplicate calculation logic.** The backend remains responsible
   for approved financial aggregation and domain/read-model calculations.
   The frontend formats and renders supplied values and does not recreate
   financial rules.
4. **Keep API and Domain responsibilities separated.** Controllers handle
   HTTP concerns; Application services orchestrate approved read models;
   Domain owns approved financial rules; Infrastructure provides data
   access. API DTOs expose only approved contract fields.
5. **Privacy behavior must follow the ADR decision.** Do not infer scope,
   defaults, persistence, synchronization, or API filtering from the UX
    proposal. The separately approved Phase 0 exception is a Dashboard-only,
    non-persistent Total Assets UI mask. Broader Privacy Mode must follow
    [ADR-0002](./ADR-0002-FinancialPrivacyMode.md) only after its decisions
    are finalized.
6. **Preserve current contracts by default.** Do not change request
   parameters, routes, response field meanings, transaction behavior, or
   account balance behavior as part of a visual hierarchy refactor.
7. **Keep scope incremental.** Separate the information-architecture
   refactor from privacy behavior and any future Spendable Amount capability.
8. **Preserve the Goal display decision.** Show all existing Goals in Goal
   Summary; record the mobile length risk without imposing a Top 3 limit.
   Goal funding remains a future concept, not a Phase 0 feature.

## Phase Plan

### Phase 0 — Contract and ADR approval

**Goal**

This product decision phase is complete for the Phase 0 UI scope. The
approved hierarchy is Summary First on mobile, with Recent Activity and
Account Details collapsed and every existing Goal retained in Goal
Summary. Broader Privacy Mode, Spendable Amount, and Goal funding remain
separate future decisions.

**Files likely affected**

- `docs/DashboardV2DesignProposal.md`
- `docs/DashboardV2ApiContract.md`
- `docs/ADR-0001-SpendableAmount.md`
- `docs/ADR-0002-FinancialPrivacyMode.md`
- Any separately approved contract or decision document

This is a documentation and product decision phase; production code should
not change.

**Dependencies**

- Product agreement on Level 1, Level 2, and Level 3 information.
- Explicit confirmation that any first UI-only slice uses existing API data
  and does not include Spendable Amount or unresolved privacy behavior.
- Separate decisions for any new API fields, privacy filtering, preference
  storage, or future financial rules.

**Risks**

- Treating a proposed information example as an approved API requirement.
- Starting implementation while privacy or Spendable Amount decisions are
  still ambiguous.

**Validation**

- Review each proposed Dashboard value against the existing Phase 5
  contract.
- Confirm each future field or behavior has an explicit approval and owner.
- Confirm deferred items are clearly marked as out of scope for the initial
  UI slice.

### Phase 1 — Backend read-model preparation

**Goal**

Confirm that the approved first-slice UI can be served by the existing
Dashboard and Budget Radar responses. Make backend changes only if an
approved contract identifies a genuine missing read-model value.

**Files likely affected**

No backend file is assumed to require change for an existing-data-only
information-architecture refactor. If a separate contract approves an
additional read-model field, possible files include:

- `LifeDashboard.Api/Controllers/DashboardController.cs`
- `LifeDashboard.Api/Controllers/BudgetRadarController.cs`
- `LifeDashboard.Application/Services/DashboardService.cs`
- `LifeDashboard.Application/Services/BudgetRadarService.cs`
- `LifeDashboard.Application/Models/ReadModels/DashboardSummaryReadModel.cs`
- `LifeDashboard.Application/Models/DTOs/DashboardSummaryDto.cs`
- `LifeDashboard.Application/Models/DTOs/RadarStateDto.cs`
- Directly related API and Application tests

The actual file set depends on the approved contract. This plan does not
propose a DTO shape or endpoint change.

**Dependencies**

- Phase 0 approval of the API contract for the fields being implemented.
- Confirmation that each field is available from supported domain and
  repository data.
- For any privacy-based filtering, an approved privacy/security decision and
  response compatibility plan.

**Risks**

- Adding data not needed by the approved UI scope.
- Duplicating Budget Radar calculations in `DashboardService`.
- Changing the existing Payday period, transaction filtering, Goal behavior,
  or account balance behavior while preparing a read model.

**Validation**

- Focused Application unit tests for any new orchestration behavior.
- API integration/contract tests for any approved response addition.
- Regression checks that existing fields and their meanings remain intact.
- Verify that the Dashboard's nested Budget continues to match the existing
  Radar read model where applicable.

### Phase 2 — Frontend information-architecture refactor

**Status: Complete** for the approved existing-data Dashboard UI scope.

**Goal**

Reorganize the Dashboard around the approved information hierarchy using
existing response values. Reduce competing visual weight without adding
financial calculations or unapproved data.

**Files likely affected**

- `LifeDashboard.Web/src/presentation/pages/DashboardPage.tsx`
- `LifeDashboard.Web/src/presentation/pages/DashboardPage.css`
- `LifeDashboard.Web/src/presentation/components/dashboard/FinancialOverview.tsx`
- `LifeDashboard.Web/src/presentation/components/dashboard/BudgetRadarSummary.tsx`
- `LifeDashboard.Web/src/presentation/components/dashboard/GoalSummary.tsx`
- `LifeDashboard.Web/src/presentation/components/dashboard/RecentActivity.tsx`
- `LifeDashboard.Web/src/presentation/components/dashboard/AccountDetails.tsx`
- Their directly related styles and `QuickAddEntry.tsx` if layout changes
- Existing Dashboard child components and their styles if the approved
  decomposition requires it

Existing API services, domain types, and stores should remain unchanged when
the work only reorders and presents existing data.

**Dependencies**

- Phase 0 approval of the first-slice hierarchy and content.
- Phase 1 only if an approved UI element depends on a newly approved
  backend field.
- The separately approved Total Assets UI mask is limited to the Dashboard.
  Broader Privacy Mode and Spendable Amount remain excluded unless their
  ADRs have independently been resolved.

**Risks**

- Moving fields without preserving their period labels or domain meaning.
- Letting secondary information compete with the three first-layer mobile
  summaries instead of collapsing Recent Activity and Account Details.
- The complete Goal list may make the mobile page long as the number of
  Goals grows; do not address this by silently truncating to Top 3.
- Introducing frontend sorting, filtering, aggregation, or status inference
  during component extraction.

**Validation**

- Targeted frontend component tests for the rendered sections, empty states,
  and API-supplied values.
- Build and targeted lint checks after implementation.
- Manual visual QA at mobile (390px), tablet (768px), and desktop (1280px)
  viewports.
- Check for horizontal overflow, clipped/wrapped long content, accessible
  disclosure actions, the mobile order, and fixed navigation covering page
  content. Confirm all existing Goals remain represented.
- Authenticated runtime smoke test of the existing Dashboard API mapping.

### Phase 3 — Financial Privacy Mode

**Goal**

Implement broader privacy behavior only after the finalized Financial
Privacy ADR. The separately approved, Dashboard-only Total Assets UI mask
belongs to Phase 2 and does not authorize a global or API-level privacy
feature. This phase may be skipped if broader Privacy Mode is deferred.

**Files likely affected**

Frontend-only concealment may affect:

- `DashboardPage.tsx` and its CSS
- `FinancialOverview.tsx` and its CSS
- Other covered account, Goal, transaction, report, export, or notification
  presentation components
- A frontend preference/state module only if the approved scope requires it

If the approved decision requires a user-wide preference or API disclosure
control, possible backend files may include:

- Existing settings or a separately approved preference service and
  contract
- Relevant Application DTOs/services and API controllers
- Persistence/domain/infrastructure files only if an approved storage
  decision requires them
- Corresponding API contract and security tests

No specific preference field, storage model, or endpoint is selected here.

**Dependencies**

- Approval of [ADR-0002-FinancialPrivacyMode.md](./ADR-0002-FinancialPrivacyMode.md),
  including financial categories, default, toggle/reveal behavior,
  persistence, cross-page scope, and accessibility.
- Separate approval of whether privacy is UI-only, API-level, or hybrid.
- Updated API contract and compatibility plan if response disclosure changes.

**Risks**

- Masking only headline values while exposing component or derived values.
- Treating UI masking as API security.
- Inconsistent behavior across Dashboard, Accounts, Transactions, Reports,
  exports, or notifications.
- A persistent or synchronized preference behaving unexpectedly on shared
  devices.

**Validation**

- Frontend tests verify every in-scope amount and derived representation is
  masked in hidden state and rendered correctly when revealed.
- Accessibility checks ensure hidden values are not exposed through
  accessible names or descriptions contrary to the approved decision.
- API security/contract tests if the API is required to omit or restrict
  values.
- Manual checks across every approved product surface and session/device
  behavior.

### Phase 4 — Future Spendable Amount

**Goal**

Consider a Spendable Amount only after its business meaning and supporting
domain rules are approved. This phase is not authorized by the current
proposed ADRs and is not part of the initial Dashboard V2 UI refactor.

**Files likely affected**

The affected files cannot be determined until the domain and API decisions
are complete. Possible areas include:

- Domain concepts and related domain services, if approved rules establish
  reusable financial policy
- Application services and read models for orchestration
- Infrastructure repositories/queries only for approved data sources
- API DTOs/controllers only after a response contract is approved
- Frontend domain types, API mapping, and Dashboard presentation
- Focused domain, Application, integration, and frontend tests
- Database mapping and migration only if an approved persistence decision
  requires it

No classes, fields, tables, formulas, or endpoints are designed by this plan.

### Future concept — Goal funding (not scheduled for Phase 0)

The product direction for possible Manual Contribution and Linked Account
Allocation is recorded in
[DashboardV2GoalFundingConcept.md](./DashboardV2GoalFundingConcept.md).
This is not an implementation phase or an approved data model. Any future
work must first define how Account Balance, Goal Allocated Amount, and Goal
Spending Amount differ. Do not add a `GoalAccount` entity,
`GoalTransaction`, database migration, API contract, or DTO under this
Phase 0 plan.

**Dependencies**

- Approval of [ADR-0001-SpendableAmount.md](./ADR-0001-SpendableAmount.md)
  with account scope, time horizon, reservation semantics, Goal behavior,
  Fixed Commitment treatment, transaction/settlement behavior, and
  non-duplication rules.
- Approved domain and API design, including treatment of negative or
  missing values and update timing.
- Separate schema/migration approval if new persisted domain facts are
  required.

**Risks**

- Implementing a formula before rules are complete and presenting an
  assumption as financial fact.
- Double-counting Goal transfers, paid commitments, and recorded expenses.
- Counting balances that are not spendable or omitting valid funds.
- Creating domain or schema changes before the supporting concept is
  understood.

**Validation**

- Domain tests for approved reservation and account-eligibility rules.
- Application tests for read-model composition and edge cases.
- API integration/contract tests for the approved response.
- Frontend tests verify direct rendering only, with no duplicate formula.
- Reconciliation scenarios confirm no double counting across account
  balances, transactions, Goals, and commitments.

## Backend Plan

The current contracts already support an existing-data-only Dashboard
reorganization. Do not change backend code simply to match a new visual
hierarchy.

### Controller

- Preserve the authenticated, read-only Dashboard and Budget Radar routes
  and existing `currentDate` request semantics.
- Change a controller only if an approved contract requires a new request or
  response behavior.
- Keep financial business rules out of controllers.
- Do not add an endpoint based only on the proposed Dashboard layout.

### Application service

- Keep `DashboardService` responsible for composing the approved Dashboard
  read model from existing services and repositories.
- Keep `BudgetRadarService` responsible for the existing Payday-period
  budget flow.
- Do not recompute Budget Radar values in `DashboardService` or calculate
  financial values in the controller.
- Add orchestration only for approved data that is not already available
  through the current read model.

### DTO and read model

- Keep existing DTO fields and meanings stable.
- Add fields only after the Dashboard V2 API Contract is approved and the
  field has a supported source and explicit semantics.
- Do not add Spendable Amount, privacy preference fields, Fixed Commitment
  details, UI labels, colors, warnings, or status thresholds without their
  corresponding approved decision and contract.
- Do not change database schema or introduce a migration as part of the
  information-architecture phase.

## Frontend Plan

### `DashboardPage`

- Compose the approved first-layer areas. On mobile, use Summary First:
  Financial Overview, Budget Radar Summary, then Goal Summary.
- Keep Recent Activity and Account Details available but collapsed by
  default on mobile.
- Keep all existing Goals in Goal Summary; do not cap the list at Top 3.
- Keep Quick Add behavior and transaction submission flow unchanged unless a
  separate product task approves a change.
- Continue mapping backend values directly; do not aggregate accounts or
  transactions to create new financial totals.

### Components

- Keep `BudgetRadarSummary` focused on supplied Budget Radar values with
  explicit Payday-period context.
- Reuse existing Goal and account data; preserve all Goals and account
  details without introducing funding relationships.
- Keep `RecentActivity` and `AccountDetails` content available behind
  mobile disclosure controls.
- Extract components only where it improves separation and maintainability
  of the approved layout; do not create components merely to anticipate
  unapproved features.
- Add Fixed Commitment presentation only if an approved API contract
  supplies the required data.

### State management

- Reuse the existing Dashboard summary flow. Keep transaction entry in its
  existing feature flow.
- Do not add a store or API request for values already included in
  `summary.budget`.
- Add privacy preference state or a new store only if the approved
  preference scope requires it.
- Keep server-sourced financial data and local display state distinguishable.

### Display responsibility

- Frontend formats currency and dates and renders approved API values.
- Frontend does not calculate Spendable Amount, budget totals, ratios,
  warning thresholds, or financial status.
- The approved Phase 0 Total Assets mask is Dashboard-only local UI state
  and must not claim to remove the value from the API response.
- Broader Privacy Mode requires the separate ADR decision.

## Testing Strategy

Testing begins with implementation phases only after their inputs and
contracts are approved. Test scope must remain focused on behavior introduced
by the relevant phase.

### Unit tests

- Application tests for any newly approved read-model orchestration and
  preserved period semantics.
- Domain tests for future financial rules only after ADR-0001 is resolved.
- Do not change existing transaction, soft-delete, or balance behavior
  assertions to accommodate Dashboard presentation work.

### Integration tests

- Verify the authenticated Dashboard and Budget Radar routes preserve
  request behavior and expose the approved response fields.
- Verify user scoping, soft-delete exclusion, calendar-month totals, and
  Payday-period Budget Radar behavior remain intact.
- If API privacy filtering is approved, verify included/omitted values and
  authorization behavior against explicit contract scenarios.

### Frontend tests

- Verify information sections render values directly from the Dashboard
  response and preserve empty/loading/error behavior.
- Verify no frontend-derived financial totals, percentages, thresholds, or
  status labels are introduced.
- If Privacy Mode is approved, cover every in-scope amount and derived
  visualization in both hidden and revealed states.
- Verify keyboard and assistive-technology behavior for privacy controls
  according to the approved UX decision.
- Verify mobile disclosure controls for Recent Activity and Account Details
  and confirm all Goals remain visible in Goal Summary.

### Manual verification

- Use an authenticated session to smoke-test Dashboard and Budget Radar
  endpoints and compare displayed values with API responses.
- Review responsive behavior at 390px, 768px, and 1280px.
- Check overflow, clipping, long text, button access, navigation overlays,
  chart behavior, and empty states.
- If privacy behavior is implemented, verify all approved surfaces,
  exports, notifications, and cross-device behavior manually as applicable.

## Migration Strategy

### Safe changes, subject to Phase 0 approval

- Reorder and group existing API-supplied information in the frontend while
  preserving values, request behavior, and data flow.
- Apply the approved mobile Summary First order and collapse the two
  secondary sections without removing their data or functionality.
- Preserve the complete Goal list and record its page-length risk; do not
  change Goal presentation to Top 3 without a new decision.
- Adjust presentation labels to clarify existing meanings and periods, as
  long as labels do not imply new financial rules.
- Extract or reorganize presentation components without changing stores or
  endpoint contracts.
- Keep API fields additive if a separate approved requirement needs new
  data; preserve current fields and meanings.

### Changes requiring ADR/contract approval

- Any privacy preference persistence, cross-device synchronization, or
  server-side filtering.
- Any changes to response fields, request parameters, endpoints, or
  interpretation of current fields.
- Exposure of Fixed Commitment details or historical Dashboard data.
- Any Spendable Amount concept, calculation, reservation rules, or related
  domain/persistence changes.
- Any Goal funding behavior or relationship among Goals, Accounts, and
  Transactions. The concept requires separate ADR/API/Domain design.
- Any change to Goal, Transaction, Account balance, or Fixed Commitment
  business behavior.

### Do not introduce yet

- Spendable Amount or a proxy label such as “available” for
  `RemainingLivingBudget`.
- New API or DTO fields, endpoints, database fields, entities, or
  migrations not approved by an ADR and contract.
- Frontend financial calculations, inferred warnings, Budget Radar status,
  or thresholds.
- A privacy setting whose scope, default, persistence, and security meaning
  have not been approved.
- A `GoalAccount` entity, `GoalTransaction`, database migration, API
  contract, or DTO for Goal funding.

### Deployment sequencing

If a future approved phase changes the API contract, coordinate backend and
frontend deployment so clients do not require fields before the server
provides them. If response filtering is approved, plan compatibility for
existing consumers and avoid treating absent values as numeric zero.

## Risks

1. **Premature Spendable Amount:** Users may rely on a number derived from
   assumptions about Goals, Fixed Commitments, accounts, or budget periods.
2. **Privacy implementation mismatch:** A narrow frontend mask may leave
   sensitive values visible elsewhere or be misunderstood as API protection.
3. **Dashboard information overload:** Reordering without reducing visual
   competition may preserve the current density; adding unsupported details
   would worsen it.
   The approved full Goal list is a known mobile page-length risk; future
   pagination, collapse, or summary options require a separate decision.
4. **Duplicate financial calculations:** Repeating backend calculations in
   the frontend can produce inconsistent values and undermine the approved
   source of truth.
5. **Contract compatibility:** Conditional or premature response changes
   can break existing consumers and create frontend/backend release-order
   problems.
6. **Period confusion:** Calendar-month totals and Payday-period budget
   values may be presented as if they cover the same period.

## Exit Criteria

### First existing-data UI implementation

The Phase 0 product decisions are approved and recorded in
[DashboardV2Phase0ApprovedScope.md](./DashboardV2Phase0ApprovedScope.md).
The approved implementation boundary is:

- Use existing response data only; no API/DTO/Domain change.
- On mobile, prioritize the three first-layer areas and collapse Recent
  Activity and Account Details.
- Display all existing Goals and retain the documented page-length risk.
- Apply only the separately approved Total Assets Dashboard UI mask.
- Preserve period labels and financial meanings; exclude Spendable Amount
  and Goal funding behavior.

### Privacy or Spendable Amount phases

- Privacy implementation requires an approved ADR-0002 decision, finalized
  privacy contract, and cross-surface/security acceptance criteria.
- Spendable Amount implementation requires an approved ADR-0001 decision,
  a defined domain policy, approved API contract, and any required
  persistence/migration approval.
- Each phase has focused test cases, manual verification criteria, and
  explicit compatibility expectations before code changes begin.

## Assumptions

- The Phase 5 API contract remains authoritative until superseded by an
  approved contract.
- The first implementation slice can use existing Dashboard Summary and
  nested Budget response data without adding an API request.
- The initial information-architecture refactor does not alter CRUD,
  transaction, Goal, or account balance behavior.
- The limited Total Assets UI mask is approved; broader Privacy Mode and
  Spendable Amount remain unapproved while their ADR decisions are open.
- Goal funding is a future concept only and has no approved API, Domain, or
  persistence design.

## Future Decisions (not Phase 0 blockers)

- Whether to extend Goal funding beyond the recorded concept, including
  source, allocation, spending, and transaction semantics.
- Whether a future Goal list improvement should use pagination, collapse,
  or a summary mode; the current decision is to display all Goals.
- Whether broader Privacy Mode, Spendable Amount, or Fixed Commitment
  details should be implemented, and under what approved contracts.

## References

- [DashboardV2DesignProposal.md](./DashboardV2DesignProposal.md)
- [ADR-0001-SpendableAmount.md](./ADR-0001-SpendableAmount.md)
- [ADR-0002-FinancialPrivacyMode.md](./ADR-0002-FinancialPrivacyMode.md)
- [DashboardV2ApiContract.md](./DashboardV2ApiContract.md)
- [Phase5DashboardApiContract.md](./Phase5DashboardApiContract.md)
- [DashboardV2Phase0ApprovedScope.md](./DashboardV2Phase0ApprovedScope.md)
- [DashboardV2MobileUXProposal.md](./DashboardV2MobileUXProposal.md)
- [DashboardV2GoalFundingConcept.md](./DashboardV2GoalFundingConcept.md)
