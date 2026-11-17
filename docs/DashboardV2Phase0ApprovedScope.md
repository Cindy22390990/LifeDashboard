# Dashboard V2 Phase 0 Approved Scope

## Status

Approved for frontend implementation based on the product decisions confirmed
on 2026-11-17.

This approved scope supersedes conflicting proposed recommendations or
blocking status in earlier Phase 0 proposal documents. It approves only the
limited Phase 0 behavior described here; it does not approve changes to
ADR-0001, ADR-0002, the API contract, or the Domain Model.

## 1. Product Decision

| Decision | Approved choice |
|---|---|
| Dashboard purpose | Financial Status Overview |
| First-screen structure | Three core information areas |
| First-screen areas | Financial Overview, Budget Radar Summary, Goal Summary |
| Mobile information hierarchy | Summary First; prioritize the three first-layer areas before secondary information |
| Mobile secondary sections | Recent Activity and Account Details are collapsed by default; their content and functions remain available |
| Total Assets | Mask by default in the Dashboard UI; allow click/tap to reveal |
| Privacy boundary | UI-only mask for Total Assets. No API filtering, all-site Privacy Mode, or authorization/permission system |
| Quick Add | Replace the embedded full form with a Quick Add button that opens the existing transaction-entry flow |
| Goals | Keep a Goal Summary on the Dashboard and display all existing Goals; do not make the full GoalPyramid a primary section |
| Second-level information | Recent Transactions and Account Details |
| Full transaction management | Move off the Dashboard; keep management in its existing transaction area |
| Spendable Amount | Excluded; do not calculate, display, or substitute another value |
| Fixed Commitment presentation | Do not add a new Dashboard presentation; preserve existing Budget Radar behavior |
| Backend and persistence | No API, DTO, Domain, database, or migration changes |

The Total Assets mask is a screen-display convenience only. The API response
continues to contain the existing value, and other financial amounts remain
visible under this Phase 0 decision.

## 2. Dashboard V2 Information Architecture

### First layer — three core information areas

1. **Financial Overview**
   - Present the existing Total Assets value with a UI mask enabled by
     default and a control to reveal it.
   - Include relevant existing financial overview context without changing
     its source or meaning.
   - Keep calendar-month Income and Expense distinct from Payday-period
     Budget values.

2. **Budget Radar Summary**
   - Summarize existing Budget Radar data.
   - Keep its Payday-period meaning and labels clear.
   - Do not label its remaining budget or daily values as Spendable Amount
     or available account cash.

3. **Goal Summary**
   - Keep a summary using existing Goal data and display all existing Goals;
     do not limit the list to Top 3.
   - Do not add Goal calculations or imply Goal amounts are reserved cash.
   - Full Goal management remains in the existing Goals area.

### Second layer

- **Recent Transactions:** retain the existing Dashboard-provided recent
  transaction data and its existing order and count.
- **Account Details:** show as secondary Dashboard information; it must not
  compete with the three first-layer areas.

### Mobile presentation

- Use Summary First: prioritize Financial Overview, Budget Radar Summary,
  and Goal Summary, in that order, before secondary information.
- Keep all existing Goals in Goal Summary. Do not truncate to Top 3 or
  replace the list with a newly calculated aggregate.
- Collapse Recent Activity and Account Details by default in mobile mode.
  Their information and functionality remain available when expanded.
- Use progressive disclosure for secondary detail; do not remove content
  or alter its source data.
- This ordering identifies priority, not a promise that every item fits
  above the fold at every mobile height.

### Goal future direction

Goal funding may later support Manual Contribution and Linked Account
Allocation. A linked account does not mean its entire balance belongs to a
Goal. Future design must distinguish Account Balance, Goal Allocated Amount,
and Goal Spending Amount. This is a concept only; see
[DashboardV2GoalFundingConcept.md](./DashboardV2GoalFundingConcept.md).
It does not authorize implementation in Phase 0.

### Moved off the home page

- **Full Quick Add Form:** do not embed the full form on the Dashboard.
  Keep a Quick Add button as an entry point to the existing transaction
  entry flow.
- **Full Transaction Management:** retain transaction management in its
  existing area, not as a full management surface on the Dashboard.

## 3. Phase 0 Implementation Boundary

### Allowed

#### Frontend

- Refactor `DashboardPage` to implement the approved information hierarchy.
- Split presentation components where useful, without adding new data or
  financial behavior.
- Reorganize and refine Dashboard CSS and responsive presentation.
- In mobile mode, present the three first-layer areas before secondary
  content and collapse Recent Activity and Account Details by default.
- Keep the complete existing Goal list in Goal Summary; do not cap it at
  Top 3.
- Add a Total Assets mask in the Dashboard UI:
  - Start masked on Dashboard load.
  - Allow the user to reveal or mask the displayed value using an accessible
    control.
  - Limit this behavior to the Total Assets display on the Dashboard.
  - Do not persist or synchronize the preference as part of Phase 0.
- Replace the embedded full Quick Add form with a button that reuses the
  existing transaction-entry flow.
- Keep Recent Transactions, Account Details, and Goal Summary consistent
  with the approved hierarchy and existing response data.

### Prohibited

- Backend API changes, including endpoints, request parameters, or response
  behavior.
- DTO or API contract changes.
- Domain or financial calculation changes, including Goal calculations.
- Database schema changes or migrations.
- Spendable Amount calculation, display, placeholder, or proxy label.
- New Fixed Commitment Dashboard presentation or changed Budget Radar
  treatment of commitments.
- Privacy API, API filtering, all-site Privacy Mode, preference
  synchronization, or authorization/permission system.
- Changes to transaction CRUD, validation, submission behavior, or existing
  account and Goal behavior.
- Truncating the Goal Summary to Top 3, adding a new Goal aggregate, or
  introducing Goal funding behavior.

## 4. Acceptance Criteria

### Desktop — 1280px

- The three first-layer areas are visually distinct and easy to scan.
- Secondary information does not compete with the Financial Overview.
- No horizontal overflow, clipped labels, or obscured controls.

### Tablet — 768px

- The three first-layer areas remain clearly ordered and readable.
- Secondary sections remain accessible without displacing the primary
  overview.
- No horizontal overflow, clipped text, or inaccessible actions.

### Mobile — 390px

- The three first-layer areas stack cleanly and remain identifiable.
- The mobile information order is Financial Overview, Budget Radar Summary,
  Goal Summary, then secondary information.
- Recent Activity and Account Details are collapsed by default and remain
  accessible without deleting their content.
- Goal Summary still displays all existing Goals; the potential page-length
  risk is documented rather than addressed by truncating the list.
- Long Goal and transaction text wraps without covering values or controls.
- The Total Assets mask control and Quick Add button remain reachable.
- Fixed navigation, if present, does not cover content or controls.

### All viewports

- Within five seconds, users can understand the high-level financial
  overview without reading every section.
- The first layer contains no more than three main information areas:
  Financial Overview, Budget Radar Summary, and Goal Summary.
- Total Assets is masked by default and can be revealed or hidden by the
  user.
- Only the Dashboard Total Assets UI is masked; the UI does not claim that
  API data or other financial amounts are protected.
- Quick Add does not occupy the Dashboard's primary space and reuses the
  existing transaction-entry behavior.
- No value is presented as Spendable Amount or derived using new financial
  logic.
- Existing Budget Radar, Goal, account, and transaction meanings are
  preserved.
- No Goal funding behavior, Goal/account relationship, or Goal/transaction
  relationship is introduced.
- The Dashboard API request and response remain unchanged.

## Phase 0 Status

**APPROVED FOR PHASE 0 FRONTEND IMPLEMENTATION**

## Goal Funding Boundary

The future Goal funding concept is documented in
[DashboardV2GoalFundingConcept.md](./DashboardV2GoalFundingConcept.md) only.
Phase 0 remains UI-only for this concept: no `GoalAccount` entity,
`GoalTransaction`, database migration, API contract, or DTO is authorized.
