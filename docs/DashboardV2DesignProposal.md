# Dashboard v2 Design Proposal

Status: Product and architecture proposal — no implementation decision approved  
Date: 2026-11-17

This document analyzes the current Dashboard and proposes a direction for a
future redesign. It does not change the Phase 5 API contract, define an
available-money formula, or select a persistence strategy for privacy
preferences.

## 1. Current State Analysis

The current page combines a financial summary, Goal visualization, Budget
Radar, account balances, recent activity, and a transaction-entry form. The
data flow is primarily one Dashboard summary request; the page reads
`summary.budget` rather than fetching a second Budget Radar model.

| Information | Source | User value | Current problem | Recommendation |
|---|---|---|---|---|
| Current date and Dashboard heading | `DashboardPage` local date formatting | Establishes the date context for the summary | Uses space without explaining which financial period applies to each figure | Retain a compact date and label each period explicitly |
| Total Assets | `DashboardSummaryDto.TotalAssets`; `DashboardService` aggregates active user accounts and excludes system accounts | Shows the value of the user's current account balances | Sensitive; combines account types and does not by itself mean immediately spendable money | Retain with a user-controlled visibility option; do not label it as available money |
| Monthly Income and Monthly Expense | `DashboardService` uses the existing monthly report repository for the calendar month | Shows recent cash-flow direction | Calendar-month totals do not explain the separate Payday budget period or current spendability | Retain as compact secondary context |
| Managed account count | `summary.accounts.length` | Indicates how many accounts are configured | Administrative rather than a direct answer to the user's main question | Move to account details or omit from the primary area |
| Recent Transactions | `DashboardService` and existing transaction read model; API supplies at most five | Explains recent changes to account balances | Adds a detailed list to an already dense first screen | Retain in details, below the primary financial status |
| Goal Pyramid: name, type, status, current and target amounts, progress | `summary.goals`, `Goal` entity, and `GoalPyramid` presentation component | Shows progress toward user goals | High visual and numeric density; Goal progress is not the same as money reserved for spending | Retain in a lower-priority detail area; do not treat Goal amounts as reserved cash without an approved rule |
| Today's remaining safe amount, today's spending, daily safe limit, and remaining days | `summary.budget`, produced through `BudgetRadarService`, `BudgetRadarAssembler`, and `BudgetSnapshot` | Provides a daily budget view for the current Payday period | “Safe amount” can be mistaken for total cash available; several related values compete for attention | Keep only the most useful daily context visible; make the period explicit |
| Total Living Budget and Remaining Living Budget | Existing `BudgetSnapshot` read model, exposed in `RadarStateDto` | Shows budget remaining in the current Payday period | `RemainingLivingBudget` is a period budget result, not a balance across accounts or a general “available money” amount | Retain with an explicit Payday-period label; never silently rename it to Available Money |
| Uncategorized expense amount | Existing `RadarStateDto` field | Helps users understand a budget input that is not categorized | Can distract from the primary question and is not an API status | Keep as secondary detail; display the supplied amount without deriving a status |
| Account names, types, and balances | `summary.accounts`; active user accounts returned by the existing account service | Lets users inspect individual account balances | Repeats asset information and may undermine a “hide total assets” preference if individual balances remain visible | Place in account details; decide whether the privacy toggle also masks these balances |
| Quick Add Transaction form | `DashboardPage`, category store, and transaction store | Allows immediate transaction entry | A full form materially increases Dashboard length and competes with the read-only overview | Keep the action easy to reach, but evaluate whether the full form belongs on the Dashboard |

### Existing data boundaries

- `TotalAssets` represents current balances of active, user-owned, non-system
  accounts. `AccountRepository` scopes accounts to the user and excludes
  deleted accounts; the Dashboard aggregation excludes system accounts.
- `BudgetSnapshot` describes a Payday period. It already exposes
  `RemainingLivingBudget`; that read-model value must not be assumed to equal
  available cash.
- `FixedCommitment` provides a monthly amount and active month range. The
  current Budget Radar includes applicable commitments in its existing
  snapshot flow, but the entity does not represent payment status or a held
  account balance.
- `SystemSetting` stores Payday and an `AllocationRatio` for living, savings,
  investment, and entertainment. The ratio is a preference/configuration
  value; it is not a ledger of amounts actually allocated or reserved.
- `Goal` stores target/current amounts, type, and status. A saving deposit
  currently records an account-to-account transfer and updates the Goal's
  current amount. A purchase Goal records an expense and completion state.
  The Goal amount therefore must not automatically be subtracted from account
  balances as a separate reservation; that could count the same funds twice.
- Transactions record Income, Expense, or Transfer and are already reflected
  in account balance behavior. A transfer changes where funds are held, not
  the user's total assets.

## 2. Dashboard v2 Information Architecture

The first screen should answer “How is my financial situation now?” without
presenting every available report at equal weight. Reorganize existing
information before adding more cards.

### Primary KPI area — at most three values

1. **Available Money / 可安心使用** — the intended lead KPI, but do not
   display a fabricated value or substitute `RemainingLivingBudget` until its
   domain meaning and calculation inputs are approved.
2. **Remaining Living Budget / 本期剩餘生活預算** — the existing API value,
   labeled as the current Payday period and visually distinguished from cash
   on hand.
3. **Total Assets / 總資產** — retain as an optional, user-controlled value
   with an explicit show/hide affordance.

If Available Money is not yet defined, the primary area should show only the
existing, correctly labeled values rather than suggesting that a budget
remainder is spendable cash.

### Secondary information

- Calendar-month Income and Expense, presented together with their period.
- A concise current Payday-period context, using existing budget fields only
  where they help explain Remaining Living Budget.
- A compact navigation/action to account details, rather than a second
  prominent list of balances.

### Detail information

- Recent transactions, retaining the API-provided order and item count.
- Goal progress and account-level balances.
- Less frequently needed budget inputs such as daily spending, remaining
  days, or uncategorized expense amount.
- Quick Add as an action; decide separately whether its complete form should
  remain embedded on the Dashboard.

The current Goals, account, and transaction details should not all be expanded
above the primary KPI area. This proposal does not add charts or new cards.

## 3. Total Assets Privacy Design

The codebase currently has `SystemSetting`, not a separate `UserSetting`
entity. The following options have different persistence and privacy
properties.

| Option | Advantages | Disadvantages | Clean Architecture impact |
|---|---|---|---|
| **A. Add `ShowTotalAssets` to existing `SystemSetting`** | Per-user preference can persist across sessions and devices; reuses the existing settings aggregate and API flow | Couples a presentation preference to financial settings; requires Entity, DTO/service/API changes and a migration; a UI mask does not remove the value from the API payload | Changes Domain, Application, Infrastructure, and possibly API contracts; appropriate only if cross-device preference is required |
| **B. Add a `DashboardPreference` entity** | Separates Dashboard presentation preferences from Payday and financial allocation settings; can grow to hold multiple Dashboard preferences | Adds a new entity/table and repository/service/API plumbing for a small initial preference; requires migration and default handling | Stronger separation of concerns but broader Clean Architecture surface and likely unnecessary unless more Dashboard preferences are planned |
| **C. Store preference in browser `localStorage`** | No backend, schema, or migration changes; simplest for a device-local display toggle | Does not sync across browsers/devices; clearing browser storage resets it; does not protect data from someone with browser access or from API/network inspection | Presentation-only state; least backend coupling, but explicitly device-scoped |

### Recommendation, not a decision

First decide whether the preference must follow the user across devices. If
it is only a local display convenience, Option C has the smallest impact. If
it must be account-wide, Option A is a plausible reuse point; Option B is
more appropriate only if a broader preference aggregate is planned. No option
is selected by this proposal.

Also decide the scope of hiding. The current Dashboard displays individual
account balances as well as Total Assets; hiding only the total still exposes
its components. A mask is a presentation privacy measure, not an
authorization boundary: the authenticated API currently returns the
TotalAssets value. If the requirement is to withhold the value from clients,
that is a separate API/security contract decision.

The hidden state should not leave the unmasked value available through
accessible text, copyable hidden content, or other visible Dashboard
components. The reveal control should be explicit and accessible.

## 4. Available Money Domain Design

Do not define a formula until the product has specified what “can safely use”
means, the relevant time period, and which balances and reservations count.

| Concept | Existing data | What it currently means | Open question before treating it as Available Money |
|---|---|---|---|
| Current Balance | `Account.CurrentBalance` for active user accounts; Dashboard Total Assets excludes system accounts | Current recorded balance per account, across account types | Which account types are spendable now? Should savings or investment accounts be included? |
| Reserved Money | No general reservation ledger or reserved-balance field was found | There is no single persisted amount representing all money users intend not to spend | What creates, releases, and audits a reservation? Is it an actual transfer, a commitment, or a planning-only amount? |
| Fixed Commitment | `FixedCommitment.MonthlyAmount` and active month range; used by the existing Payday Budget Radar flow | A recurring planned obligation included in the existing budget snapshot; no payment/settlement state | Is an unpaid commitment reserved until paid, and how should a paid transaction stop it being counted again? |
| Goal Allocation | `Goal.TargetAmount`, `CurrentAmount`, `GoalType`, and `Status`; deposits use account transfers | Goal progress is tracked, but there is no explicit per-period allocation or universal reserved amount | Does a saving Goal reserve money beyond the destination account balance, or is the transfer itself the reservation? Avoid duplicate counting |
| Budget Allocation | `SystemSetting.AllocationRatio` | Four configured percentages that sum to 100; current Dashboard and `BudgetSnapshot` do not turn them into tracked allocated amounts | Are ratios planning guidance, an actual allocation, or a constraint on spending? |
| Budget-period remainder | `BudgetSnapshot.RemainingLivingBudget` | Remaining living budget for the selected Payday period under existing Budget Radar rules | Should this remain separate from account cash, and how should it be communicated alongside Available Money? |

### Calculator and layer placement

- Do not create `AvailableMoneyCalculator` until the above decisions define
  its inputs, period, and treatment of reservations.
- If the rule becomes a stable domain policy that must be reused or enforced
  by multiple financial use cases, a pure Domain service may be justified.
- If it is a Dashboard-only, read-only composition of account, commitment,
  Goal, and budget read models, an Application-layer calculator/orchestrator
  is the more natural initial location. It can consume data from existing
  repositories/services without making the Domain depend on persistence.
- Infrastructure should own queries and persistence mapping, not the
  financial policy or final user-facing interpretation.

## 5. API Contract Impact

`availableMoney` is not part of the approved Phase 5 response. The Phase 5
contract describes `TotalAssets`, monthly totals, recent transactions, and
the existing Budget read model, including `budget.remainingLivingBudget`.

If Available Money is approved as a distinct KPI:

- Expose it as a new, additive Dashboard response field only after its meaning
  and calculation responsibility are specified.
- Expand the internal `DashboardSummaryReadModel` as needed, but an internal
  read-model change alone cannot provide the value to the frontend.
- Keep `Budget.RemainingLivingBudget` unchanged and separately named. Do not
  change its meaning to avoid adding a field.
- Decide whether a privacy preference only controls rendering or also
  affects response disclosure; the latter changes API behavior and needs an
  explicit contract decision.

An additive JSON property is generally less disruptive than removing or
retyping an existing property, but risks remain:

- Strict response-shape tests, generated clients, and consumers that reject
  unknown fields may need updates.
- A required frontend type deployed before a backend that emits the new field
  can expose missing-value behavior; deployment compatibility should be
  planned.
- Changing the meaning of `TotalAssets` or
  `Budget.RemainingLivingBudget`, or making existing fields conditional, is a
  higher compatibility risk than adding a separately defined field.
- The field definition must specify user scope, account scope, time period,
  update timing, negative values, and treatment of reservations before the
  API contract is amended.

## 6. Migration Risk

| Preference approach | Entity/schema change | Migration | Default decision |
|---|---|---|---|
| Add property to `SystemSetting` | Add a Boolean preference to the existing entity and map it through the settings contract | Required; existing rows need a deterministic value | `true` preserves current visible behavior; `false` is privacy-first but hides assets for existing users. Product must choose |
| Add `DashboardPreference` | Add a new user-scoped entity and define uniqueness/ownership rules | Required for a new table; decide how existing users receive a preference | Define behavior when a preference row is absent and whether rows are created lazily or for all users |
| Browser `localStorage` | No Domain entity or database schema change | Not required | Define first-use behavior and the consequence of cleared or unavailable browser storage |

For either backend option, confirm deployment order, migration rollback
expectations, and default behavior for existing users. Applying a preference
default is a product behavior change, not merely an EF implementation detail.

## 7. Implementation Roadmap

### Sprint 1 — Design only

- Approve the definition and time period of Available Money.
- Decide how current balances, fixed commitments, Goal deposits, and
  allocation ratios relate without double-counting.
- Decide whether Total Assets privacy is device-local or user-wide, whether
  account-level balances are also hidden, and the default visibility.
- Update/approve an API contract decision before implementation.
- Define acceptance examples for missing settings, negative or zero results,
  inactive accounts, and period boundaries.

### Sprint 2 — Backend

- Implement only the approved read-model policy and data sources.
- Choose Domain or Application calculation placement based on whether the
  rule is a reusable financial invariant or a Dashboard read-model
  composition.
- If selected, implement preference persistence and its migration/default;
  otherwise keep preference storage out of backend scope.
- Add focused unit, integration, and response-contract tests.

### Sprint 3 — Frontend

- Reorder existing information to the approved primary/secondary/detail
  hierarchy; avoid adding cards without a user need.
- Add the Total Assets reveal/hide control with accessible masking and
  behavior consistent with the chosen preference scope.
- Map any approved Available Money field directly from the API; do not
  reproduce the financial calculation in the browser.
- Keep Payday-period Remaining Living Budget clearly distinct from Available
  Money.

### Sprint 4 — Testing

- Test calculation boundaries and non-duplication for account balances,
  commitments, and Goal deposits.
- Test preference defaults, persistence scope, migration behavior, masking,
  and any API disclosure behavior.
- Verify API compatibility and existing Dashboard consumers.
- Perform authenticated runtime and responsive visual QA, including
  accessibility checks for the privacy toggle.

## Principal Risks and Unresolved Decisions

1. **Meaning and double-counting:** account balances, Goal progress, and
   fixed commitments describe different financial facts. Their relationship
   must be defined before a single Available Money value is defensible.
2. **Privacy scope:** masking Total Assets alone leaves account-level balances
   visible, and client-side masking does not hide the API value.
3. **Period and contract ambiguity:** Payday-period budget remainder and
   account balances have different scopes. An additive API field needs an
   explicit definition and compatibility plan.

## Sources Reviewed

- [Phase5DashboardApiContract.md](./Phase5DashboardApiContract.md)
- [Phase5ImplementationDecision.md](./Phase5ImplementationDecision.md)
- [DashboardController.cs](../LifeDashboard.Api/Controllers/DashboardController.cs)
- [DashboardService.cs](../LifeDashboard.Application/Services/DashboardService.cs)
- [DashboardSummaryReadModel.cs](../LifeDashboard.Application/Models/ReadModels/DashboardSummaryReadModel.cs)
- [BudgetRadarAssembler.cs](../LifeDashboard.Application/Services/BudgetRadarAssembler.cs)
- [Goal.cs](../LifeDashboard.Domain/Entities/Goal.cs)
- [Account.cs](../LifeDashboard.Domain/Entities/Account.cs)
- [Transaction.cs](../LifeDashboard.Domain/Entities/Transaction.cs)
- [FixedCommitment.cs](../LifeDashboard.Domain/Entities/FixedCommitment.cs)
- [BudgetSnapshot.cs](../LifeDashboard.Domain/ValueObjects/BudgetSnapshot.cs)
- [SystemSetting.cs](../LifeDashboard.Domain/Entities/SystemSetting.cs)
- [DashboardPage.tsx](../LifeDashboard.Web/src/presentation/pages/DashboardPage.tsx)
- [DashboardPage.css](../LifeDashboard.Web/src/presentation/pages/DashboardPage.css)
- [BudgetRadarPanel.tsx](../LifeDashboard.Web/src/presentation/components/BudgetRadarPanel.tsx)
- [dashboardSummary.ts](../LifeDashboard.Web/src/domain/dashboardSummary.ts)
- [budgetRadar.ts](../LifeDashboard.Web/src/domain/budgetRadar.ts)
- [dashboardSummaryStore.ts](../LifeDashboard.Web/src/application/stores/dashboardSummaryStore.ts)
- [budgetRadarStore.ts](../LifeDashboard.Web/src/application/stores/budgetRadarStore.ts)

## 8. Dashboard Information Hierarchy

The hierarchy below reorganizes the information already present on the
Dashboard. It does not imply new calculations or require every section to be
expanded at once.

| Section | Information grouped here | Why it belongs here |
|---|---|---|
| **Today's Overview** | Dashboard heading/date, the future Spendable Amount concept, Total Assets, and a compact monthly income/expense summary | This is the first stop for the user's broad question about their current financial situation. Distinct periods and meanings must remain clearly labeled; a budget remainder must not be presented as spendable cash. |
| **Budget** | Today's remaining amount, today's spending, daily safe limit, remaining days, total living budget, remaining living budget, and uncategorized expense | These values describe the current Payday budget context. Grouping them keeps budget interpretation together and prevents them from being mistaken for account balances. |
| **Goals** | Goal name, type, status, current amount, target amount, and progress | Goals express longer-term intentions, so they are useful context but should not compete with the immediate financial overview. Goal progress must not imply that the corresponding amount is separately reserved. |
| **Accounts** | Account names, types, and current balances; managed account count | These are account-level details that explain where recorded funds are held. They belong below the overview and must follow the selected privacy mode. |
| **Recent Activity** | Recent transactions, including date, type, account, description, and amount | Recent activity helps explain changes in balances and budget. It is supporting evidence, not the primary summary, so it belongs after the current-state sections. |
| **Quick Actions** | Quick Add Transaction and navigation to related management, such as categories | These actions help users act on the overview but are not financial status information. Keep them easy to find without allowing a full entry form to dominate the first screen. |

### Suggested reading order

1. Today's Overview
2. Budget
3. Quick Actions
4. Goals
5. Accounts
6. Recent Activity

The placement of Quick Actions may be adjusted for the product's most common
task, but it should remain visually distinct from financial KPIs.

## 9. Information Priority

Priority describes how prominently an element should appear, not whether the
information is useful. Every current Dashboard element is classified below.

### High priority

| Element | Why |
|---|---|
| Today's Overview heading and date context | Establishes where the user is and the date for which current-state information is being shown. |
| Spendable Amount, only after its meaning can be supported | It is the intended answer to the product's primary question. Until then, do not substitute another value or show a misleading placeholder as a real amount. |
| Remaining Living Budget with its Payday-period label | It is an existing useful budget value, but its period-specific meaning must be explicit. |
| Total Assets with the privacy control | It is a key financial summary, subject to the user's choice to reveal or hide it. |
| Quick Add Transaction action | It supports a frequent next step, while the full form itself need not have high visual priority. |
| Loading and error feedback | Users need to know whether the overview is being retrieved or could not be loaded; keep feedback clear but separate from financial content. |

### Medium priority

| Element | Why |
|---|---|
| Monthly Income and Monthly Expense | Useful cash-flow context, but calendar-month totals do not determine current spendability. |
| Today's spending, daily safe limit, and remaining days | Help explain today's budget context, but are secondary to the remaining budget figure. |
| Goal name, type, status, current amount, target amount, and progress | Supports planning, but is longer-term information and does not establish a reserved balance. |
| Account names, types, and individual balances | Useful for understanding where money is held, but detailed and potentially sensitive. |
| Recent transaction date, type, account, description, and amount | Helps explain recent changes; it is a detail list rather than a top-level KPI. |
| Navigation to account/category management | Useful actions, but should not displace the overview. |

### Low priority

| Element | Why |
|---|---|
| Managed account count | Administrative context that adds little to the immediate financial question. |
| Uncategorized expense amount | Useful for data quality and budget review, but not a primary status indicator; do not infer a warning or status from it. |
| Full Quick Add Transaction form | Useful when recording a transaction, but its fields and validation add substantial visual weight. Prefer a compact entry point unless product usage demonstrates the full form belongs on the home screen. |
| Product eyebrow and explanatory Dashboard subtitle | Provide branding and orientation, but should not compete with the financial summary. |
| Empty-state messages for accounts and recent transactions | Useful when a list has no entries, but only relevant in that state and should remain within its section. |

## 10. Privacy Mode Proposal — UX Only

Privacy Mode is a presentation behavior for the Dashboard. This proposal does
not specify how the preference is stored, delivered, or enforced.

- **Toggle location:** Place a clearly labeled show/hide control next to the
  Total Assets value in Today's Overview. The control should be easy to reach
  without being confused with account settings.
- **Default behavior:** Proposed default is visible, matching the current
  Dashboard behavior. This is a product choice that should be confirmed
  before implementation.
- **Hidden appearance:** Replace the Total Assets amount with `****` while
  retaining the label and layout, so the page does not jump when visibility
  changes. The control should clearly indicate whether values are currently
  shown or hidden and be usable with keyboard and assistive technology.
- **Consistent scope:** When Total Assets is hidden, any other visible value
  explicitly included in that privacy scope should also be masked. The initial
  scope must decide whether this includes individual account balances,
  monthly totals, budget amounts, Goal amounts, and transaction amounts;
  leaving component values visible can reveal financial information despite
  hiding the total.
- **Future extensibility:** The UX can grow from hiding Total Assets to a
  broader “hide money values” mode covering all monetary amounts throughout
  the Dashboard. Non-monetary details such as dates, names, transaction
  types, and counts can remain visible unless a later product decision says
  otherwise. Broader coverage should be introduced as an explicit scope, not
  assumed from the initial Total Assets control.

## 11. Spendable Amount Analysis

The question here is whether the current Domain Model can correctly determine
a general-purpose Spendable Amount. A model value may be reliable for its
existing meaning without being sufficient for this different concept.

| Existing concept | Classification for calculating Spendable Amount | Reason |
|---|---|---|
| `Goal` | **Cannot calculate reliably** | `CurrentAmount`, target, type, and status track progress, but do not say how much is reserved now. Saving deposits are transfers between accounts, so subtracting Goal progress from aggregate balances could count the same funds twice. |
| `FixedCommitment` | **Cannot calculate reliably** | Monthly amount and active month range identify planned recurring obligations, but the model has no paid/settled state or reservation amount. It cannot distinguish an unpaid future bill from an amount already paid and reflected in transactions. |
| `AllocationRatio` | **Cannot calculate reliably** | The ratios express configured allocation preferences, not actual cash allocations, transfers, or reserved balances. They do not establish which funds are available to spend. |
| `BudgetSnapshot` | **Cannot calculate reliably** | It reliably represents its existing Payday-period budget values, including `RemainingLivingBudget`, under current rules. A period budget remainder is not an account-balance or reservation measure. |
| Account balance (`CurrentBalance`) | **Cannot calculate reliably** | The current balance is a recorded account amount and supports the existing Total Assets read model. The model does not establish that every account is spendable, unlocked, settled, or available for the user's intended timeframe. |
| Current transactions | **Cannot calculate reliably** | Recorded transactions describe account activity and inform existing balance and budget behavior. By themselves they do not represent unpaid future obligations, pending activity, or a reservation lifecycle. |

### Unknown

The current model does not establish whether all financial activity that
affects immediate availability is represented as a posted transaction, or
how pending, uncleared, or scheduled activity should affect the concept. It
also does not define which account types a user considers spendable or how
obligations spanning Payday periods should be treated. Until these meanings
are specified, the completeness of any Spendable Amount input set is
**Unknown**.

Therefore, no Spendable Amount should be inferred by combining the existing
values. Existing values can continue to be shown with their own labels and
semantics.

## 12. Required Future Domain Changes

If the product requires a reliable Spendable Amount, the following domain
concepts are currently missing or not represented explicitly. This is a list
of conceptual gaps, not an implementation design.

| Missing concept | Why it is needed |
|---|---|
| **Reserved Money** | Distinguishes funds intentionally unavailable for ordinary spending from funds that are merely present in an account. |
| **Locked Balance** | Represents money that exists in a recorded balance but cannot currently be used, independently of a user's general intent. |
| **Goal Reserved Amount** | Clarifies whether a Goal has funds set aside now and prevents Goal progress from being confused with a separate cash reservation. |
| **Future Obligation Reservation** | Represents the amount and relevant period of an upcoming commitment when product rules say it should reduce spendability. |
| **Payment/settlement state** | Distinguishes an obligation that remains due from one already paid or settled, avoiding duplicate treatment alongside a transaction. |
| **Reservation lifecycle** | Defines when a reservation begins, changes, and is released so an amount does not remain reserved indefinitely or disappear without explanation. |
| **Spendable account scope** | Identifies which account funds qualify for immediate use, rather than assuming all recorded account balances have equivalent availability. |
| **Pending or uncleared activity meaning** | Establishes whether activity not yet reflected as a posted balance change affects availability and how it relates to transactions. |

These concepts require product and domain decisions before any calculation
can be specified.

## 13. Conceptual Wireframe

This wireframe illustrates grouping and relative order only. It does not
define visual styling, responsive behavior, calculations, or new API data.

```text
------------------------------------------------------------
Dashboard                                      [Privacy: Show]
Tuesday, November 17

TODAY'S OVERVIEW
Spendable Amount                 Not available until defined
Remaining Living Budget          (current Payday period)
Total Assets                     $****
Monthly Income                   Monthly Expense
------------------------------------------------------------
BUDGET
Today's spending | Daily safe limit | Remaining days
Total living budget | Uncategorized expense
------------------------------------------------------------
QUICK ACTIONS
[Add transaction]  [Accounts]  [Categories]
------------------------------------------------------------
GOALS
Goal name                 Progress / status
...
------------------------------------------------------------
ACCOUNTS
Account name / type                         Balance
...
------------------------------------------------------------
RECENT ACTIVITY
Date | Description / account | Type | Amount
...
------------------------------------------------------------
```

In the initial experience, do not show “Not available until defined” as a
literal KPI. It marks the intended future position in this conceptual
wireframe; until the domain meaning is approved, omit the Spendable Amount
value or clearly keep it out of the live Dashboard.

## Assumptions

- “Dashboard element” means each information group and action currently
  rendered by `DashboardPage` and `BudgetRadarPanel`, including date,
  metrics, account count, Goals, budget values, accounts, recent
  transactions, and the Quick Add form.
- “Default behavior” in the Privacy Mode section is a proposal, not an
  approved product decision.
- Existing model values retain their present meanings; this proposal does
  not reinterpret `RemainingLivingBudget`, Goal progress, or allocation
  ratios.

## Unanswered Questions

1. Should the initial privacy toggle hide only Total Assets, or also
   individual account balances and other monetary values?
2. Should privacy default to visible or hidden, and should the user's choice
   persist beyond the current Dashboard visit?
3. Which account types and time horizon should “Spendable Amount” describe?
4. Which commitments, Goal intentions, and pending or uncleared activity
   should count as unavailable funds, and how should already-paid amounts be
   recognized?
5. Should Quick Add remain a full embedded form, or become a compact action
   that opens a separate entry flow?

## Implementation Risks

1. **Misleading semantics:** Reusing `RemainingLivingBudget` or Total Assets
   as Spendable Amount could imply availability that the current models do
   not establish.
2. **Double-counting:** Goal progress, account transfers, fixed commitments,
   and recorded expenses can overlap unless reservation and settlement
   meanings are explicit.
3. **Incomplete privacy coverage:** Hiding only the headline total while
   showing account balances, transaction amounts, or other money values can
   defeat the user's expected privacy behavior.
4. **Priority overload:** Keeping all existing details and the full Quick Add
   form prominent would preserve the current density rather than achieve the
   proposed hierarchy.
