# Dashboard V2 Phase 0 UI Specification

## Status

**Approved for Phase 0 Frontend Implementation**

The original proposals in this document are superseded where they conflict
with [DashboardV2Phase0ApprovedScope.md](./DashboardV2Phase0ApprovedScope.md)
and the latest confirmed mobile decisions. Broader Privacy Mode,
Spendable Amount, and Goal funding behavior remain outside Phase 0.

## 1. Dashboard Goal Definition

### Approved Phase 0 primary goal: A. Financial Status Overview

The Dashboard should first help a user understand their current financial
status. This matches the Dashboard V2 design goal of answering “How is my
financial situation now?” and can be approached using existing Dashboard
Summary and Budget Radar data without introducing a new financial
calculation.

This direction is approved for the Phase 0 Dashboard.

### Why this direction

- The existing response already provides Total Assets, calendar-month
  Income and Expense, and a Payday-period Budget read model.
- These values can be presented with their existing names and scopes.
- Recent transactions, Goals, account details, and Quick Add can support the
  overview without competing with it as the primary purpose.
- It keeps Phase 0 focused on organizing information rather than changing
  financial operation behavior.

### Why not B. Financial Operations Center

- The embedded Quick Add form is currently substantial and competes with
  status information.
- Making operations the primary purpose would require a product decision
  about whether the full form remains on the Dashboard or moves to a
  dedicated flow.
- Existing transaction CRUD behavior is not in scope for this UI hierarchy
  decision.

### Why not C. Budget Control Center

- Budget Radar is a Payday-period budget view, not a consolidated account
  balance or Spendable Amount.
- A budget-first Dashboard could cause users to mistake `RemainingLivingBudget`
  or `TodayRemaining` for money available across accounts.
- Spendable Amount and budget rules have not been approved for expansion.

## 2. Information Architecture

The levels below describe information priority, not an exact screen layout.
Phase 0 must preserve the API-provided values and their existing meanings.

### Level 1 — first-layer priority

On mobile, use Summary First and present these three areas before secondary
information. This is a priority order, not a guarantee that every item fits
above the fold at every screen height.

- **Financial Overview:** Total Assets, Monthly Income, and Monthly Expense
  retain their existing meanings. Total Assets is masked by default in the
  Dashboard UI only.
- **Budget snapshot:** a concise Payday-period budget value, with its period
  clearly stated; keep it visually distinct from Total Assets.
- **Goal Summary:** display all existing Goals and their progress using
  existing data. Do not truncate to Top 3 or create an aggregate.
- **Date/period context:** identify calendar-month values and Payday-period
  values accurately.

Do not add Spendable Amount as a placeholder, estimate, or renamed budget
value.

### Level 2 — available but not competing for first attention

- **Budget Radar supporting values:** daily spending, daily safe limit,
  remaining days, total living budget, and uncategorized expense. These
  remain detail to the headline Payday-period budget value.
- **Recent Transactions:** existing API items in their supplied order and
  count (up to five); collapse this section by default on mobile.
- **Quick Add:** a button opening the existing transaction-entry flow.

### Level 3 — moved to another page or an explicit detail area

- **Account Details:** account names, types, and individual balances;
  collapse this section by default on mobile. Do not delete the content or
  change its existing behavior.
- Expanded transaction details beyond the compact recent-activity section.
- Any Fixed Commitment detail view. Dashboard Summary does not currently
  return commitment details; adding them is not part of Phase 0.

### Information semantics to preserve

- Monthly Income/Expense cover the calendar month containing `currentDate`.
- Budget Radar covers the Payday period containing `currentDate`.
- Total Assets is an account-balance aggregate that excludes system
  accounts; it does not mean immediately spendable money.
- Goal progress is not a reservation ledger.
- Fixed Commitments may influence the existing Budget Radar calculation,
  but their payment state and reservation semantics are not represented by
  the current Dashboard response.

## 3. Phase 0 Display Decision

The following choices are approved. “Collapse” changes presentation only;
it does not remove data, alter calculations, or change behavior on the
feature's own page.

| Feature | Keep | Collapse | Remove | Move | Approved decision and reason |
|---|---:|---:|---:|---:|---|
| Total Assets | Yes |  |  |  | Keep in Financial Overview with its existing meaning; mask by default in Dashboard UI and allow reveal. |
| Monthly Income and Expense | Yes |  |  |  | Keep in Financial Overview and label as calendar-month values. |
| Budget Radar | Yes |  |  |  | Keep the Payday-period summary in the first layer; do not change values or calculation. |
| Goal Summary | Yes |  |  |  | Display all existing Goals; do not truncate to Top 3, collapse the Goal list, or present Goal amounts as reserved funds. |
| Recent Transactions |  | Yes, mobile |  |  | Preserve API order and count; collapse the section by default in mobile mode. |
| Quick Add | Yes |  |  |  | Keep the entry button; use the existing transaction-entry flow and do not embed the full form. |
| Account Details |  | Yes, mobile |  |  | Keep account details available; collapse the section by default in mobile mode. |
| Fixed Commitment details |  |  | Yes |  | Do not add a separate Fixed Commitment section. Existing Budget Radar behavior remains unchanged. |

## 4. Total Assets Privacy Decision

`ADR-0002-FinancialPrivacyMode.md` remains Proposed for broader Privacy
Mode decisions. The specific Phase 0 Total Assets UI mask is separately
approved by the product decision and Approved Scope.

### Approved Phase 0 behavior: Option A — UI mask for Total Assets only

Mask Total Assets by default on the Dashboard and allow the user to reveal
or hide it. This is local display behavior only. Other financial values
remain visible; the API continues returning its existing data.

Do not treat this limited mask as comprehensive privacy or API protection.
Do not add a global toggle, cross-page masking, preference persistence,
synchronization, or API filtering as part of this decision.

| Option | Phase 0 impact | Assessment |
|---|---|---|
| **A. Eye icon UI mask for Total Assets only** | Masks only the Dashboard Total Assets display; API and other monetary displays remain unchanged. | **Approved** for Phase 0 with the limited scope stated above. |
| **B. Dashboard Privacy Mode toggle** | Requires decisions about which Dashboard values to mask, default, toggle scope, persistence, and whether account details are covered. | Not approved for Phase 0; broader scope remains open in ADR-0002. |
| **C. Defer all privacy behavior** | Keeps all current display behavior. | Not selected for Total Assets; broader Privacy Mode and API-level behavior remain deferred. |

UI masking does not protect values already returned by the API. Any broader
privacy implementation must follow a separately approved ADR and API
contract where applicable.

## 5. Component Structure

### Current implementation structure

```text
DashboardPage
├── FinancialOverview
├── BudgetRadarSummary
├── GoalSummary
├── RecentActivity
├── AccountDetails
└── QuickAddEntry
```

### Approved presentation responsibilities

- **DashboardPage:** loads the existing summary and composes the page.
- **FinancialOverview:** presents Total Assets, Monthly Income, and Monthly
  Expense; the Total Assets mask is Dashboard-local UI state.
- **BudgetRadarSummary:** presents supplied Budget Radar values and
  Payday-period context.
- **GoalSummary:** presents all existing Goals and their existing progress;
  no Top 3 cap or new aggregate.
- **RecentActivity and AccountDetails:** retain existing data and behavior;
  collapse these sections by default in mobile mode.
- **QuickAddEntry:** links to the existing transaction-entry flow.

### Future API support required

- **Fixed Commitment details:** not available in the Dashboard response.
  Showing a separate commitment list requires an approved API contract and
  response data.
- **Privacy preference or API filtering:** requires ADR-0002 approval and,
  if server-provided or enforced, an approved API contract.
- **Spendable Amount:** requires ADR-0001 resolution, domain rules, and a
  separately approved API field before a component may render it.

The approved Phase 0 component structure requires no new API request, DTO,
store, or backend calculation.

## 6. Phase 0 Scope Boundary

### Phase 0 can do

- Reorder the existing Dashboard content to match the approved hierarchy.
- Use Summary First on mobile and collapse Recent Activity and Account
  Details by default.
- Keep all existing Goals visible in Goal Summary; do not use Top 3.
- Extract presentation-only components and reorganize CSS.
- Improve section headings, period labels, spacing, responsive layout, and
  readability while preserving financial meanings.
- Keep current API response, data flow, store behavior, and Quick Add
  transaction behavior unchanged.
- Hide secondary content from the initial mobile presentation only, without
  deleting data or changing source behavior.
- Keep Total Assets masked by default in the Dashboard UI, with an
  accessible reveal control, as separately approved.

### Phase 0 cannot do

- Calculate or display Spendable Amount, including by reusing
  `RemainingLivingBudget` or Total Assets under a new label.
- Add financial formulas, allocation rules, thresholds, warnings, or
  inferred status.
- Add API requests, endpoints, response fields, DTO properties, or alter
  the existing API contract.
- Add a global Privacy Mode, cross-page masking, preference persistence,
  synchronization, or API privacy filtering without separate approval.
- Add Fixed Commitment details to the Dashboard without an approved API
  contract.
- Change transaction, Goal, Fixed Commitment, account balance, or
  BudgetSnapshot domain behavior.
- Add Goal funding behavior, cap the existing Goal list, or introduce a
  Goal/account or Goal/transaction relationship.
- Add database schema, entities, or migrations.

## 7. Acceptance Criteria

Acceptance applies to implementation of the approved decisions. It does
not imply that responsive QA has already been performed.

### Desktop — 1280px

- The primary financial overview and distinct Payday-period budget value
  are apparent without scanning secondary sections.
- Secondary areas do not compete visually with Level 1.
- Financial values retain clear labels and period context.
- Content remains within the page container without horizontal overflow.

### Tablet — 768px

- Level 1 remains clearly ordered and readable without requiring the user to
  inspect every section.
- Supporting sections remain distinguishable and accessible.
- No value, label, action, or section heading is clipped or obscured.

### Mobile — 390px

- Primary values and their labels stack without horizontal scrolling.
- Summary First order prioritizes Financial Overview, Budget Radar Summary,
  and Goal Summary before secondary information.
- Recent Activity and Account Details are collapsed by default; their
  information remains accessible.
- Goal Summary displays all existing Goals, not only Top 3.
- Long transaction descriptions and account/Goal names wrap without
  covering monetary values or controls.
- The Quick Add button and navigation actions remain reachable.
- Fixed navigation, if present, does not cover content or controls.

### All viewports

- Within five seconds, a user can identify the current financial overview
  and distinguish it from the Payday-period budget value.
- The user does not need to read every card or expand every Level 2/3 area
  to understand the high-level status.
- No UI text implies Total Assets or `RemainingLivingBudget` is Spendable
  Amount.
- Monthly values are identified as calendar-month values; Budget Radar
  values are identified as Payday-period values.
- No new warning, status, color threshold, percentage, or financial
  calculation is inferred in the frontend.
- Recent transactions retain API-provided ordering and count.
- Goal funding, Account balance, and Goal spending are not conflated or
  connected by new behavior.
- Quick Add submission and validation behavior is unchanged.
- API routes, request parameters, DTOs, response fields, and backend
  behavior are unchanged.
- Privacy expectations are explicit: only Dashboard Total Assets is
  masked; UI masking does not claim API data or other amounts are protected.

## Decision Status

**APPROVED FOR PHASE 0 FRONTEND IMPLEMENTATION**

Spendable Amount is explicitly excluded from the Phase 0 UI and current API;
it must remain excluded unless ADR-0001 and a future API contract are
approved.

## Assumptions

- The current Phase 5 API contract and response types remain unchanged for
  Phase 0.
- The existing `summary.budget` provides the Budget Radar values used by
  the Dashboard; no additional Budget Radar request is needed.
- Collapsing Recent Activity or Account Details on mobile does not alter
  their source data or behavior.
- The Goal Summary continues to show every Goal, even if its length grows.
- The mobile ordering describes priority before secondary sections; exact
  viewport height may vary.

## Unanswered Questions

There are no unresolved product decisions blocking the approved Phase 0
scope. Future work still requires decisions on broader Privacy Mode,
Spendable Amount, Fixed Commitment presentation, and Goal funding.

## Risks

1. **Privacy expectation mismatch:** Deferring Privacy Mode leaves sensitive
   values visible; users must not be led to believe otherwise.
2. **False financial interpretation:** Poor labels could make
   Payday-period Budget Remaining look like cash available to spend.
3. **Mobile disclosure accessibility:** Collapsed secondary content must
   remain discoverable and operable with keyboard and assistive technology.
4. **Scope creep:** Adding Fixed Commitment details or privacy preferences
   would require new API/domain decisions outside Phase 0.
5. **Goal list length:** Displaying every Goal preserves the approved
   decision, but mobile page length may grow as users add Goals. Pagination,
   collapse, or summary modes are future considerations only.

## References

- [DashboardV2DesignProposal.md](./DashboardV2DesignProposal.md)
- [DashboardV2ImplementationPlan.md](./DashboardV2ImplementationPlan.md)
- [DashboardV2ApiContract.md](./DashboardV2ApiContract.md)
- [ADR-0001-SpendableAmount.md](./ADR-0001-SpendableAmount.md)
- [ADR-0002-FinancialPrivacyMode.md](./ADR-0002-FinancialPrivacyMode.md)
- [DashboardV2Phase0ApprovedScope.md](./DashboardV2Phase0ApprovedScope.md)
- [DashboardV2MobileUXProposal.md](./DashboardV2MobileUXProposal.md)
- [DashboardV2GoalFundingConcept.md](./DashboardV2GoalFundingConcept.md)
