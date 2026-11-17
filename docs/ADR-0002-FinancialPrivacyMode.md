# ADR-0002: Financial Privacy Mode

## Status

Proposed

## Date

2026-11-17

## Context

Dashboard V2 is intended to give users a concise view of their financial
situation. The current Dashboard also presents information that users may
not want visible to someone looking at their screen, including Total Assets,
individual account balances, monthly income and expense, budget values, Goal
amounts, and transaction amounts. This may happen in shared spaces, during
screen sharing, or simply because a user prefers not to display amounts at
all times.

Hiding only Total Assets may be insufficient. Account balances can reveal
the components of that total; income, expenses, budget values, Goal amounts,
and transaction amounts can each reveal financial circumstances on their
own. A privacy control whose scope is not clear may create a false sense
that financial information is hidden.

For purposes of this decision, Dashboard information is grouped as follows:

| Information class | Examples | Privacy consideration |
|---|---|---|
| **Visible financial information** | Total Assets, account balances, income, expense, remaining budget, Goal amounts/progress derived from amounts, transaction amounts, chart values and monetary summaries | These values directly reveal or allow estimation of a user's financial position or activity. A scope decision must say which of them a privacy mode affects. |
| **Hidden financial information** | The same financial values when a selected privacy mode is active | The user should not see the original value in the covered presentation. Whether labels, trends, percentages, accessible text, exports, or notifications are also hidden remains to be decided. |
| **Non-financial information** | Dates, transaction types, account or Goal names, counts, and other descriptive labels | These are not monetary values, but some may still be personally sensitive or indirectly reveal information. They should not automatically be treated as safe for every context. |

The current `DashboardPage` renders Total Assets, monthly income and expense,
account balances, recent transaction amounts, Goal data, and a Quick Add
form. `BudgetRadarPanel` renders several budget amounts. The current
`SystemSetting` contains user name, Payday, and allocation settings;
`SettingService` reads and updates those values and does not expose a privacy
preference. This ADR records product and architecture decision points only;
it does not design implementation details.

## Problem

The privacy scope and behavior have not been defined:

1. **Total Assets:** Should the headline total be hidden, and should its
   component account balances be hidden at the same time?
2. **Account Balance:** Should all accounts be treated alike, or can different
   account types have different privacy behavior?
3. **Income and Expense:** Should monthly totals and any other income/expense
   summary be hidden? Should the rule apply to all periods or only the
   Dashboard overview?
4. **Budget Remaining:** Should daily and Payday-period budget values,
   including daily spending and safe-limit amounts, be hidden with other
   financial amounts?
5. **Goal Progress:** Should current and target amounts be hidden? If a
   progress bar, percentage, status, or ordering is derived from amounts,
   does it also reveal information that should be concealed?
6. **Transaction Amount:** Should every transaction amount be hidden,
   including transfer amounts, recent activity, and any amount in an entry
   or detail view?
7. **Related visualizations:** Do charts, graphs, tooltips, scales, summaries,
   accessibility text, and labels reveal exact or approximate amounts even
   when the visible number is masked?
8. **Non-financial detail:** Names, dates, transaction categories, and
   descriptions may reveal sensitive context even without an amount. Should
   those be included in this feature's scope or handled separately?
9. **Scope across the product:** Is Privacy Mode a Dashboard presentation
   control, or should the same state affect Accounts, Transactions, Reports,
   exports, and notifications?
10. **Preference behavior:** Is the mode temporary, persisted on the device,
    or associated with the user's account and synchronized across devices?
11. **Disclosure boundary:** Is the goal to prevent casual viewing of a
    screen, or to prevent clients and users without authorization from
    receiving the underlying data? These require different protections.

## Options

### Option A — Hide only Total Assets

Mask the Total Assets headline while leaving other financial information
visible.

**Advantages**

- Simple and easy to explain as a narrowly scoped display control.
- Preserves access to account, budget, Goal, and transaction details.
- Limits the number of views and information groups whose behavior must be
  specified initially.

**Disadvantages**

- Individual account balances may reveal the total or its components.
- Monthly cash flow, budget amounts, Goals, and transaction history remain
  visible and may reveal the user's financial situation.
- Users may reasonably assume that a general “privacy” control hides more
  than one headline value.

**Risks**

- Creates a false sense of privacy if the control or feature is described
  broadly.
- A nearby account list or chart may make the hidden total easy to estimate.
- Different pages may show the same sensitive data without any indication
  that the control is intentionally limited.

### Option B — Hide all financial amounts globally

When enabled, conceal financial amounts throughout the product surfaces
covered by the decision, rather than only on the Dashboard.

**Advantages**

- Provides a more consistent and understandable privacy behavior across
  covered screens.
- Reduces direct exposure from account balances, totals, budgets, Goals, and
  transaction amounts appearing elsewhere in the product.
- Establishes a product-wide privacy expectation rather than a one-card
  exception.

**Disadvantages**

- Requires an explicit inventory of all monetary values and derived
  visualizations in every covered surface.
- Can make normal financial tasks difficult while enabled, including
  reviewing, entering, or comparing amounts.
- Must define whether entry forms, editing flows, reports, exports, and
  notifications follow the same rule.

**Risks**

- Incomplete coverage can leave visible values or indirect indicators and
  undermine user trust.
- Global behavior may be disruptive if enabled by default or difficult to
  disable when the user needs a value.
- “All financial amounts” may not cover derived percentages, rankings,
  chart shapes, text descriptions, or amounts embedded in notification
  content unless those are explicitly considered.

### Option C — User-configurable privacy levels

Offer named levels such as:

- **Basic:** hide Total Assets.
- **Strict:** hide all money values in the defined product scope.
- **Custom:** allow users to choose categories, such as accounts, income and
  expense, budget, Goals, and transactions.

These are examples for evaluation, not proposed final labels or behavior.

**Advantages**

- Gives users more control over the trade-off between privacy and
  convenience.
- Can support different privacy expectations without imposing one scope on
  everyone.
- Allows a gradual path from a narrow Total Assets control to broader
  coverage.

**Disadvantages**

- More choices make it harder for users to understand what is hidden.
- Requires clear descriptions, default behavior, and consistent category
  boundaries across product surfaces.
- Custom combinations increase the number of states that design, support,
  and quality assurance need to reason about.

**Risks**

- Users may configure a mode that appears strict while leaving revealing
  values or derived indicators visible.
- Inconsistent application of a category across pages can create gaps.
- More settings can increase onboarding and maintenance burden and make
  privacy behavior harder to predict.

## UX Decision Questions

The following user-experience decisions remain open:

- **Toggle location:** Should the control appear beside Total Assets, in the
  Dashboard header, in a global navigation area, or in a settings page? The
  control must be discoverable without suggesting it protects data from the
  API or from authorized users.
- **Scope:** Is the toggle Dashboard-only or a global setting affecting all
  product surfaces? If global, how will the user know which surfaces are
  covered?
- **Default state:** Should values initially be visible, matching current
  behavior, or hidden by default for a privacy-first experience? What should
  happen for existing users?
- **Persistence:** Should the choice last only for the current view/session,
  persist on the device, or persist with the user's account?
- **Cross-device behavior:** Should the preference sync across devices? If
  it does, how should changes on one device affect another device currently
  in use?
- **Hidden appearance:** Should a covered amount display as `****`, bullets
  such as `••••`, or only a label with no placeholder? A fixed mask preserves
  layout but may reveal less or more than desired depending on its length;
  labels-only may make it harder to scan. The chosen treatment should not
  expose digit count, value length, sign, or precision unless explicitly
  intended.
- **Charts and graphs:** Should plots, axes, legends, tooltips, totals, and
  accessibility descriptions be concealed or replaced? A chart shape can
  still reveal relative values even when labels are hidden.
- **Reveal behavior:** Is temporary reveal allowed, and should it require an
  explicit action each time? Should the revealed state automatically return
  to hidden after navigation or a time interval?
- **Non-financial details:** Should dates, names, categories, transaction
  descriptions, and counts stay visible, or should some be part of a broader
  privacy mode?
- **Accessibility:** How should screen readers and keyboard users understand
  that amounts are hidden without receiving the original value through
  accessible text?

## Domain Boundary Analysis

No storage or implementation choice is made by this ADR. The following
boundaries have different implications:

| Boundary | Trade-offs |
|---|---|
| **User Settings** | A user-level preference can conceptually represent a choice that follows the user across relevant product surfaces and possibly devices. It requires a defined account-wide scope and raises questions about synchronization and what happens when the user is signed in on a shared device. No separate User Settings aggregate is established by this decision. |
| **System Settings** | The existing `SystemSetting` is user-scoped in current service access and contains profile and financial configuration such as user name, Payday, and allocation. It is an available conceptual place to consider, but mixing a presentation/privacy preference with financial planning settings may weaken responsibility boundaries. Its current service contract does not include privacy behavior. |
| **Frontend-only preference** | A client-side preference can be limited to a browser/device and avoid making a product-wide account preference. It may be convenient for a temporary display choice, but it does not automatically synchronize, may reset when local browser state is cleared, and is not a security control over data already delivered to the client. |

The appropriate boundary depends on whether the requirement is a transient
screen behavior, a device preference, or a user-wide product preference.
That product scope should be decided before choosing a storage boundary.
This ADR does not design tables, classes, or persistence.

## Security Analysis

Privacy Mode must not be described as a security or authorization boundary
unless the underlying data exposure is also addressed.

- **UI hiding** changes what is rendered in a covered interface. If the
  authenticated API response already contains the values, a user or process
  with access to the client may still inspect network responses, application
  state, or other client-side representations. Masking a number on screen
  does not remove it from the response.
- **API protection** controls whether a caller is authorized to receive
  particular data. If a requirement is to prevent a user or client from
  receiving a value, that needs server-side authorization and response
  disclosure rules, not only a UI toggle. This is distinct from protecting a
  screen from casual observation.
- **Data encryption** protects data in particular storage or transport
  contexts, depending on how it is applied and managed. Encryption does not
  decide which authenticated user may see a value, and it does not prevent
  the application from displaying data after authorized decryption. It is
  not a substitute for UI privacy or API authorization.

The product must clarify whether this feature addresses casual viewing,
account-wide data disclosure, or both. Any API exposure change would require
its own explicit contract and security decision; it is not approved here.

## Impact Analysis

Potential impact if privacy behavior is approved:

| Surface | Potential impact |
|---|---|
| **Dashboard** | Could affect Total Assets, monthly income/expense, budget values, Goal data, account balances, recent transaction amounts, and visualizations. The Dashboard needs one consistent indication of the active mode and clearly defined coverage. |
| **Accounts** | Account lists, account details, and balances may reveal the Total Assets components. The same scope must determine whether balances, totals, and any related charts are masked. |
| **Transactions** | Transaction amounts may appear in lists, details, forms, transfer summaries, and history. The decision must distinguish displaying stored transaction history from entering or editing a new amount. |
| **Reports** | Aggregates, category totals, trends, axes, and percentages can disclose exact or approximate financial information even if individual amounts are hidden. |
| **Export** | A visual privacy mode may not affect downloaded or shared files. The product must decide whether exports follow the mode, remain complete by design, or have a separate confirmation/privacy control. |
| **Notifications** | Push, email, in-app, and lock-screen notifications may expose amounts outside the application. Their behavior may need separate rules because users can see them without opening the app. |

## Recommendation

Do not implement Financial Privacy Mode yet. First approve its purpose and
scope: casual on-screen concealment versus limiting data disclosure, the
financial categories and surfaces covered, and whether the behavior is
temporary, device-specific, or user-wide. No option is selected by this ADR.

Before development, confirm at least:

1. Whether the selected scope is Total Assets only, all money values, or
   configurable levels.
2. Which account, income, expense, budget, Goal, transaction, chart, export,
   notification, and other product values are included.
3. The toggle location, default state, label, hidden appearance, and any
   temporary reveal behavior.
4. Whether the preference is session-only, device-local, or synchronized
   across devices.
5. Whether the goal is only UI concealment or also reduced API disclosure;
   decide any API/security changes separately.
6. How non-financial details and derived indicators are treated.

## Exit Criteria

Implementation should not begin until all of the following are satisfied:

- Privacy scope is approved, including treatment of Total Assets, account
  balances, income, expense, budget values, Goal progress, and transaction
  amounts.
- UX behavior is approved, including toggle location, default, mask style,
  reveal behavior, accessibility, and chart/graph treatment.
- Storage and persistence scope is approved, including whether the
  preference is temporary, device-local, or user-wide and whether it syncs
  across devices.
- Cross-page behavior is approved for Dashboard, Accounts, Transactions,
  Reports, exports, and notifications.
- The security goal is explicit: UI hiding, API authorization/disclosure, or
  both. Any API exposure decision has a separately approved contract.
- Derived values and indirect disclosures, including chart shapes,
  percentages, accessible text, and value length, have defined behavior.
- Acceptance scenarios cover default behavior, existing users, signed-in
  multiple devices, hidden/revealed states, and navigation between covered
  surfaces.

## Assumptions

- The current Dashboard displays the financial amounts enumerated in this
  ADR, including values provided by Dashboard and Budget Radar data.
- The current `SystemSetting` and `SettingService` do not expose a Financial
  Privacy Mode preference.
- UI concealment and API authorization are separate requirements and may
  require separate decisions.
- “Financial information” includes monetary values and can include derived
  indicators that reveal approximate amounts.

## Unanswered Questions

All questions in “Problem” and “UX Decision Questions” remain open. In
particular, the privacy scope, default behavior, persistence boundary,
cross-page coverage, hidden representation, chart behavior, and API
disclosure goal require product and security decisions.

## Implementation Risks

1. **False sense of privacy:** A Total Assets-only toggle may leave account,
   budget, Goal, or transaction amounts visible.
2. **Coverage gaps:** A global mode may miss amounts in charts, tooltips,
   accessible text, exports, or notifications.
3. **Security confusion:** Users may believe UI masking removes values from
   API responses when it does not.
4. **Preference mismatch:** Device-local state may not follow users across
   devices; synchronized state may unexpectedly reveal or hide values on a
   shared device.
5. **Usability cost:** A broad default-hidden mode or overly complex custom
   settings may obstruct routine financial tasks.
6. **Sensitive indirect disclosure:** Names, descriptions, trends, counts,
   or amount formatting may reveal information even when numeric values are
   masked.
