# Dashboard V2 API Contract

## Status

Proposed

## Date

2026-11-17

## Context

The current Dashboard combines many kinds of information without a clear
priority. Important financial values compete with details and quick-entry
controls, sensitive values are always present in the API response and visible
in the current UI, and there is no approved definition for Spendable Amount.

Dashboard V2 should organize information so a user can understand their
current financial situation, while preserving the distinction between
account facts, budget-period values, plans, and activity. This document
proposes API responsibilities and evolution boundaries; it does not approve
an implementation, select a privacy strategy, or define a Spendable Amount
calculation.

The current read models are:

- `GET /api/dashboard/summary`, represented by
  `DashboardSummaryDto`, which includes Budget, Accounts, Goals, Total Assets,
  monthly Income and Expense, and Recent Transactions.
- `GET /api/budget/radar`, represented by `RadarStateDto`, which exposes the
  existing Budget Radar values for the Payday period containing the
  requested date.

The Dashboard service composes the existing Budget Radar response with
accounts, goals, monthly totals, and up to five recent transactions. Monthly
totals use the calendar month containing `currentDate`; Budget Radar uses
the Payday payroll period containing `currentDate`. These periods are
different and must remain distinguishable.

The current response already contains sensitive financial values. Hiding
those values in a user interface does not remove them from an API response.
The scope and security purpose of Financial Privacy Mode remain unresolved
in [ADR-0002-FinancialPrivacyMode.md](./ADR-0002-FinancialPrivacyMode.md).

## Dashboard Information Architecture

The following levels define information priority and API data categories,
not the exact UI layout or number of cards.

### Level 1: Primary Information

Information intended to help a user quickly understand the current financial
picture:

- **Current financial overview:** existing Total Assets, clearly identified
  as an account-balance aggregate and not as Spendable Amount.
- **Monthly situation:** existing calendar-month income and expense,
  labeled with the applicable month.
- **Budget status:** existing Budget Radar values, labeled as applying to
  the current Payday period and kept distinct from account balances.
- **Spendable Amount:** a future candidate only. It must not be included
  until its meaning, calculation, account scope, and reservation rules are
  approved under [ADR-0001-SpendableAmount.md](./ADR-0001-SpendableAmount.md).

The API should provide existing facts and approved read-model values needed
for this level. The frontend may format them but must not derive new financial
meaning from them.

### Level 2: Supporting Information

Information that helps explain or contextualize the primary view:

- **Recent transactions:** existing API-provided items, order, and count.
- **Goals:** existing Goal information, retaining its domain meaning and
  not implying that Goal progress equals reserved cash.
- **Fixed commitments:** a potential supporting information group. The
  current Dashboard Summary response does not provide commitment details;
  the current Budget Radar service uses applicable commitments in its
  existing budget flow, but `RadarStateDto` does not expose those details.
  Exposing commitment details requires a separate contract decision.

### Level 3: Secondary Information

Detailed or historical information that does not need to be treated as a
primary Dashboard answer:

- Account-level names, types, and balances.
- Historical transactions and reports beyond the existing recent
  transaction slice.
- Detailed Goal and Fixed Commitment information where supported by their
  respective contracts.

The current Dashboard Summary already includes Accounts and Goals, and
Recent Transactions is limited to five items. Historical information and
Fixed Commitment details are not thereby authorized as new Dashboard
response fields. Their future exposure and source contracts remain separate
decisions.

## API Responsibilities

### API should provide

- **Raw financial facts** from supported domain data, scoped to the current
  user and subject to existing data access rules.
- **Existing domain/read-model calculations** that already have approved
  semantics, such as Total Assets, calendar-month totals, and the existing
  Budget Radar fields.
- **Periods and context needed to interpret values**, either through the
  existing request date and documented contract semantics or through
  separately approved response data. The API must not silently conflate
  calendar-month values with Payday-period values.
- **User preferences required for rendering** only if a future privacy
  decision establishes that the preference is user-scoped and should be
  provided by the API. A device-local or temporary preference does not
  automatically belong in the API.
- **Existing transaction and Goal fields** according to their current
  contracts, without inventing new financial interpretation.

Calculated values belong on the backend when their business or read-model
semantics have been approved. The API must not produce a value merely
because it is convenient for a Dashboard widget.

### API should not provide

- Frontend display decisions, layout, component hierarchy, or formatting.
- UI color choices, display strings, warning messages, or presentation-only
  status labels.
- New Budget Radar `Status`, warning, threshold, or percentage values
  unless separately specified and approved. The Phase 5 contract explicitly
  does not authorize them.
- Spendable Amount before the business and domain rules are approved.
- A reinterpreted `RemainingLivingBudget`, renamed Total Assets, or a
  frontend-ready substitute that conflates unlike concepts.
- Fixed Commitment details or historical data in Dashboard Summary merely
  because those concepts are useful to the proposed information hierarchy;
  each addition requires a defined source, scope, and contract.

Domain-owned fields such as the existing Goal status remain distinct from
UI status labels. Existing fields keep their current contracts; this
proposal does not add or redefine them.

## Privacy Mode Contract

No privacy behavior is selected by this proposal. The response and UI
boundary depends on whether the requirement is casual visual concealment or
limiting which data a client is allowed to receive.

| Option | Possible contract behavior | Advantages | Trade-offs and risks |
|---|---|---|---|
| **A. API returns full data; frontend masks display** | Keep the response complete for the authorized caller; the frontend hides covered values according to the selected privacy preference. | Keeps read data and display behavior separate; avoids response-shape variation based on a display choice. | The original values remain available in the network response and client state. This is only UI concealment, not API protection, and must not be represented as preventing data access. |
| **B. API applies privacy filtering** | The API uses an approved preference or authorization policy to avoid returning some covered values. | Can reduce exposure to clients that should not receive the values; makes the disclosure boundary server-enforced if backed by proper authorization policy. | Requires an approved data-disclosure rule and a contract for how unavailable values are represented. It can affect clients, caching, response compatibility, and existing consumers. A preference toggle alone is not equivalent to authorization. |
| **C. Hybrid** | The API returns only data permitted by an approved disclosure policy; the frontend may additionally mask values for on-screen privacy. | Separates server-side disclosure control from convenience masking; can support screen privacy without claiming that it protects the API. | Requires clear ownership of each rule and consistent behavior across clients. If API filtering is driven by a user preference, clients may still need presentation behavior, and response completeness can vary. |

Before any option is chosen, the product must decide:

- Which data is covered: Total Assets, Account balances, Income, Expense,
  Budget values, Goal amounts/progress, Transaction amounts, and derived
  visualizations.
- Whether the preference is temporary, device-local, or user-wide, and
  whether it synchronizes across devices.
- Whether the goal is screen privacy, reduced API disclosure, or both.
- How hidden, unavailable, or omitted values are represented in any future
  response contract, if API filtering is selected.
- Whether exports, reports, notifications, and other clients follow the
  same policy.

UI masking must not be described as API protection. A masked value that is
still present in the response is still disclosed to that client.

## Spendable Amount Boundary

**The current API must not include Spendable Amount.**

There is no approved business rule defining what is spendable, no
reservation semantics, and no approved calculation. Account balances,
Budget Remaining, Goal amounts, Fixed Commitments, Allocation Ratio, and
transactions have distinct meanings and can overlap. Combining them would
encode assumptions and could double-count or omit funds.

The following boundaries apply:

- Do not derive Spendable Amount from Total Assets or Account balances alone.
- Do not treat `Budget.RemainingLivingBudget` as Spendable Amount. It is an
  existing Payday-period budget value.
- Do not subtract Goal progress or Fixed Commitment amounts as reservations
  without approved rules for transfers, payment/settlement, timing, and
  release.
- Do not expose a placeholder, estimate, or renamed field as if it were an
  approved Spendable Amount.
- Reconsider a new response field only after the Spendable Amount decision
  and required domain concepts are approved. Any future field must have a
  defined meaning, account scope, period, update timing, negative-value
  behavior, and relationship to reservations.

See [ADR-0001-SpendableAmount.md](./ADR-0001-SpendableAmount.md) for the
decision space and unresolved business questions.

## Current API Migration Plan

This is a future contract direction, not an instruction to change the
current endpoints or DTOs.

### Dashboard Summary

- Preserve the existing `GET /api/dashboard/summary` request contract and
  existing `DashboardSummaryDto` response fields, meanings, and behavior.
- Continue to treat Total Assets, Monthly Income, Monthly Expense, Recent
  Transactions, Goals, Accounts, and the nested Budget as separate read
  facts with their existing scopes.
- Keep calendar-month income/expense separate from the Payday-period Budget
  values.
- Any future additions for privacy preferences, fixed commitment details,
  or approved Spendable Amount require an explicit contract decision. Do not
  assume an endpoint, field name, data shape, or preference delivery method
  in advance.

### Budget Radar

- Preserve the existing `GET /api/budget/radar` request contract and
  `RadarStateDto` fields and meanings.
- Continue to describe the budget period as the Payday payroll period
  containing the requested date.
- Keep `RemainingLivingBudget` as the existing Budget Snapshot value; do
  not rename or reinterpret it as Spendable Amount.
- Do not add a UI status, warning, threshold, or percentage to the API based
  on this proposal.

### Compatibility and sequencing

- Treat any future response additions as additive only after approval; do
  not remove, rename, retype, or change the meaning of current fields under
  this proposal.
- If an approved privacy decision changes what the API returns, define
  compatibility behavior for existing consumers before implementation.
- Keep request changes, endpoint changes, and response changes subject to
  their own contract review. This document does not approve new endpoints.
- Align future frontend integration with the approved response semantics;
  frontend code must not recreate backend/domain financial calculations.

## Open Questions

### Dashboard information

- Which Level 1 information should appear by default, given the Primary
  Information examples and the limit of three primary KPIs in the design
  proposal?
- Which existing budget values should be primary versus supporting detail?
- Should fixed commitment details be included in Dashboard Summary, and
  what business meaning and period should they carry?
- Should account and historical information remain in existing secondary
  contracts or be exposed through an approved Dashboard read model?

### Privacy scope

- Should privacy affect only Total Assets, all money values, or
  user-configurable categories?
- Should the API return full values for frontend masking, filter them, or
  use a hybrid policy?
- Is the requirement casual screen concealment, server-side data
  minimization, or both?
- Where is the preference owned, how does it persist, and should it sync
  across devices?
- How should hidden values, charts, derived indicators, exports, and
  notifications behave?

### Spendable Amount

- What is the approved business meaning and time horizon?
- Which account types and balances participate?
- Does Goal progress or a Goal deposit create a reservation, and how is
  double-counting prevented?
- Should Fixed Commitments reserve money, and how are payment, timing, and
  release recognized?
- How do pending transactions, transfers, and Budget Remaining relate to
  the amount?

## Exit Criteria

Implementation of the Dashboard V2 API contract can start only after:

- This API contract is reviewed and approved, including Level 1/2/3 data
  boundaries and current versus future fields.
- The Financial Privacy Mode decision is approved, including scope,
  preference ownership, persistence, cross-page behavior, and whether API
  disclosure changes.
- The Spendable Amount decision is approved, including its meaning,
  calculation ownership, account inclusion, period, reservation semantics,
  and treatment of Goals and Fixed Commitments.
- Response compatibility and migration sequencing are agreed for any
  approved additions or privacy-based filtering.
- Acceptance scenarios define expected API data and privacy behavior without
  requiring frontend-derived business calculations.

## Assumptions

- The existing Phase 5 contract remains the source of truth for current
  Dashboard and Budget Radar behavior until a future contract is approved.
- Current `DashboardSummaryDto` includes the nested `RadarStateDto`, Accounts,
  Goals, Total Assets, monthly totals, and Recent Transactions.
- Current `RadarStateDto` exposes existing budget values, including
  `RemainingLivingBudget`; the service obtains the budget period from
  `SystemSetting.Payday`.
- Fixed Commitments affect the current Budget Radar flow but their details
  are not currently exposed by `RadarStateDto`.
- Privacy behavior and Spendable Amount semantics remain unresolved under
  their respective Proposed ADRs.

## Unanswered Questions

The questions in “Open Questions” remain unresolved. The primary blockers are
which Dashboard data belongs in each information level, whether privacy is
presentation-only or changes API disclosure, and the complete business
definition of Spendable Amount.

## Risks

1. **Contract overreach:** Treating proposed information hierarchy examples
   as authorization to expose new data could expand API scope unintentionally.
2. **Semantic conflation:** Reusing Budget Remaining, Total Assets, Goal
   progress, or commitments as Spendable Amount could mislead users.
3. **Privacy leakage:** Frontend masking without API filtering leaves values
   available to the client; incomplete global filtering can still expose
   derived or secondary values.
4. **Compatibility breakage:** Conditional or differently shaped responses
   for privacy modes may affect existing consumers and require a carefully
   approved compatibility plan.
5. **Period confusion:** Calendar-month totals and Payday-period budget
   values can be misread if their scopes are not explicit.

## References

- [DashboardV2DesignProposal.md](./DashboardV2DesignProposal.md)
- [ADR-0001-SpendableAmount.md](./ADR-0001-SpendableAmount.md)
- [ADR-0002-FinancialPrivacyMode.md](./ADR-0002-FinancialPrivacyMode.md)
- [Phase5DashboardApiContract.md](./Phase5DashboardApiContract.md)
