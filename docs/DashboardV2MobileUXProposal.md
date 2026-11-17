# Dashboard V2 Mobile UX Proposal

## Status

**Approved Direction — 2026-11-17**

The product owner approved Summary First, with Recent Activity and Account
Details collapsed in mobile mode. This document records that direction;
implementation remains subject to the approved Phase 0 scope.

## 1. Current Problem

At a 390px viewport, the Dashboard uses a single-column layout. Its content
is presented in this order:

1. **Financial Overview** — Total Assets, Monthly Income, and Monthly
   Expense.
2. **Budget Radar Summary** — Payday-period remaining budget and today's
   remaining allowance.
3. **Goal Summary** — a list of all existing Goals and their progress.
4. **Recent Activity** — recent transaction rows.
5. **Account Details** — account names, types, and balances.
6. **Quick Add** — an entry button in the Dashboard header.

The narrow layout avoids horizontal overflow, but stacking every section
creates a long page. The Dashboard header and Financial Overview take space
before the Budget Radar and Goal summaries appear. In addition, the Goal
Summary renders every Goal, and the Recent Activity and Account Details
sections render their available lists. Important summaries can therefore be
separated by substantial scrolling, and secondary details add to the page
length.

The current three primary areas are visually stacked rather than condensed
into a mobile-first summary. The page may be responsive in width without
being sufficiently concise in information hierarchy.

## 2. Mobile Design Principle

Within five seconds of opening the Dashboard, a user should be able to
identify:

1. **Overall financial status** from the existing Financial Overview.
2. **Today's budget position** from the existing Budget Radar data.
3. **Important Goal progress** from the existing Goal data.

Users should not need to read every account, transaction, or Goal to get
this high-level view. Detailed information should remain available through
progressive disclosure or existing feature areas.

### Preserve financial meaning

- The existing `todayRemaining` is a Budget Radar daily allowance. It may
  be summarized as today's remaining budget allowance, but must not be
  described as cash available to spend or Spendable Amount.
- `remainingLivingBudget` belongs to the Payday period. Do not label it as
  a calendar-month budget value.
- Monthly Income and Monthly Expense remain calendar-month values.
- Total Assets retains its existing meaning and Phase 0 UI-only mask.
- Goal progress is not reserved cash. Do not create a new Goal aggregate or
  financial calculation to make the mobile summary shorter.

The phrase “今日可支配狀態” therefore requires careful interpretation. With
the current contract, the Dashboard can show the existing daily budget
allowance, not establish how much cash is actually available. A cash
availability concept remains outside this proposal and Phase 0 scope.

## 3. Proposed Mobile Layout

The product approved Option A as the mobile direction. Options B and C
remain comparison alternatives and are not selected.

### Option A — Summary First

Prioritize a compact status summary before detailed lists.

**First screen**

- Total Assets, masked by default, with the existing reveal control.
- Budget Radar's Payday-period remaining budget and today's remaining
  allowance, with their meanings and period labels.
- The complete Goal Summary, showing all existing Goals using existing data.

**Following content**

- Monthly Income and Monthly Expense remain part of Financial Overview and
  retain calendar-month context.
- Recent Activity and Account Details remain available in collapsed
  sections.
- Quick Add remains an entry button, not an embedded form.

The approved decision is to keep every existing Goal visible in the Goal
Summary; do not reduce it to Top 3, select a single Goal, or calculate an
aggregate. Goal count growth can make the mobile page long. Goal
pagination, collapse, or summary modes are future considerations only and
require a separate product decision.

### Option B — Accordion

Show the three approved primary areas first:

- Financial Overview
- Budget Radar Summary
- Goal Summary

The primary areas could show their headline values while details remain
collapsed. Accounts and Transactions could be placed in separately
expandable sections.

If the three primary areas are fully collapsed, users must tap to see their
values; that would weaken the five-second goal. A version that keeps
headline values visible and collapses only supporting details would better
preserve the intended quick scan.

### Option C — Bottom Sheet / Secondary Navigation

Keep the Dashboard focused on concise summaries. Place detailed information
in the existing feature areas:

- Account Details → Accounts
- Recent Activity → Transactions
- Full Goal details → Goals

A bottom sheet could provide a short-lived detail view without leaving the
Dashboard. Secondary navigation would instead take the user to the existing
feature area. These are distinct interaction choices within this option
and would need product selection.

Quick Add remains a button that reuses the existing transaction-entry flow.
This option should not create a new transaction form or change submission
behavior.

## 4. Trade-off Analysis

| Option | Information completeness | Development cost | User understanding speed | Future extensibility |
|---|---|---|---|---|
| **A. Summary First** | High-level information stays on the Dashboard; detailed lists can follow below. All Goals remain visible, so the Goal section and page may grow. | Low to medium. Primarily affects component composition and responsive presentation. | Potentially fastest because key values are visible without expanding sections. Depends on clear labels and manageable page hierarchy. | Good. Summary and detail layers can be refined independently while using existing data. |
| **B. Accordion** | Details remain on the Dashboard and can be expanded in place. | Medium. Requires accessible disclosure controls, expanded/collapsed state, and spacing rules. | Fast only if headline values remain visible. Fully collapsed financial summaries require extra taps. | Good for adding or reorganizing detail sections, but too many accordions can become cumbersome. |
| **C. Bottom Sheet / Secondary Navigation** | Dashboard stays concise; full detail is available through overlays or existing feature pages. | Medium. Bottom sheets add overlay behavior and accessibility requirements; navigation requires linking to the existing routes. | Fast for top-level status. Finding detailed information may require a tap and, for navigation, leaving the Dashboard. | Strong separation of summary and feature-specific detail. Reuses existing areas if their routes support the required destinations. |

No option requires new financial formulas. Option A and B can use the
existing Dashboard response. Option C can also use existing routes and
responses if those destinations already expose the required detail; it must
not add API requests to the Dashboard merely to support a presentation
choice.

## 5. Recommendation

**Recommended direction: Option A — Summary First, with progressive
disclosure for secondary detail.**

This direction most directly addresses the long mobile page while keeping
the Dashboard's approved role as a financial status overview. The mobile
priority order remains Financial Overview, Budget Radar Summary, and Goal
Summary, with all existing Goals shown. Recent Activity and Account Details
are collapsed so they do not compete with those summaries.

The product direction is approved. The following implementation details
remain constrained by the approved scope:

- Display all existing Goals; do not truncate them to Top 3.
- Collapse Recent Activity and Account Details by default in mobile mode;
  retain their content and functionality.
- Keep Monthly Income and Monthly Expense, Goal amounts, and Budget Radar
  values labeled with their existing meaning and period.
- “Today's available status” may refer only to the existing daily budget
  allowance. It must not imply available cash or Spendable Amount.

If the business requirement is specifically to show cash available to
spend, the current data and approved Phase 0 scope do not support that
meaning. It must not be inferred or introduced through this mobile proposal.

### Goal list risk

Displaying all Goals preserves the confirmed product decision but can make
the mobile page long as the number of Goals increases. Future consideration
may include Goal pagination, a collapsible Goal list, or a summary mode.
None is approved for the current Phase 0 scope.

## 6. Implementation Impact

| Option | Components | CSS | Router | API / Backend |
|---|---|---|---|---|
| **A. Summary First** | Likely adjusts `DashboardPage`, `FinancialOverview`, `BudgetRadarSummary`, `GoalSummary`, `RecentActivity`, and `AccountDetails`. Keep all Goals; add mobile disclosure only for Recent Activity and Account Details. | Reorganize mobile ordering and add accessible collapsed states for the two secondary sections. | No change expected; Quick Add keeps its existing destination. | No change required. Use existing values and do not calculate a Goal aggregate or new status. |
| **B. Accordion** | Adds or adapts disclosure behavior around existing summary details and/or secondary sections. Existing presentation components should continue to render the same data. | Adds expanded/collapsed layouts, focus states, and mobile spacing. Controls must expose their state to assistive technology. | No change expected if disclosures remain on Dashboard. | No change required. |
| **C. Bottom Sheet / Secondary Navigation** | Dashboard summaries remain; secondary detail may be shown in a bottom-sheet component or replaced by navigation actions. Quick Add continues to reuse the existing flow. | Adds bottom-sheet layout and overlay styles if that sub-option is selected; otherwise styles summary cards and navigation actions. | No change if existing Accounts, Transactions, and Goals destinations can be linked directly. Router changes may be needed only if a required destination or navigation path does not already exist. | No change required when existing destination data is sufficient. Do not add a Dashboard API request or backend read model solely for navigation. |

### Backend confirmation

**No Backend change is required for any option as described.** The current
Dashboard response already supplies the financial overview, Budget Radar,
Goal, recent transaction, and account data used by the existing components.
This proposal does not authorize DTO, API, Domain, persistence, or
migration changes.

If later product decisions require a value not present in the existing
response, that is a separate API contract decision. It must not be
workarounded with frontend financial calculations.

## Approved Product Decisions

1. Use Summary First: Financial Overview, Budget Radar Summary, then Goal
   Summary.
2. Display all existing Goals; do not use Top 3.
3. Collapse Recent Activity and Account Details by default in mobile mode;
   preserve their content and functionality.
4. Do not describe the existing daily budget allowance as available cash
   or Spendable Amount.

## References

- [DashboardV2Phase0ApprovedScope.md](./DashboardV2Phase0ApprovedScope.md)
- [DashboardV2Phase0UISpec.md](./DashboardV2Phase0UISpec.md)
- [DashboardV2ImplementationPlan.md](./DashboardV2ImplementationPlan.md)
