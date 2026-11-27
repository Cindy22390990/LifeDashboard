# Dashboard V4 Product Specification

**Status:** Draft — product decisions required  
**Primary visual source:** `docs/DashboardV4FeatureIntent.md`  
**Scope:** Home, Transactions, Accounts, Goals, Reports, Budget, Recurring Expenses, Categories, and Settings

## Evidence Labels

- **A. Source Intent** — content explicitly depicted or written in `DashboardV4FeatureIntent.md`.
- **B. Product Interpretation（推論）** — a product interpretation derived from the diagrams or current V4 implementation; it is not an approved business rule.
- **C. Open Questions** — a decision or requirement that needs product confirmation.

The intent document contains eight feature diagrams and does not include Home. The requested nine-feature scope therefore includes Home as a separate specification based on the current V4 Home implementation and the integration readiness review, not on that visual source. Amounts, dates, counts, and percentages shown in the diagrams are treated as illustrative examples, not required seed data or business rules.

Implementation and API-readiness notes below are explicitly marked as current-status evidence from the companion status/readiness documents. They describe the baseline and are not additional product requirements.

## 1. Product Overview

### Product Goal

- **Source Intent:** The document presents screens for transactions, accounts, goals, reports, budgets, fixed commitments, categories, and settings.
- **推論:** Provide a single personal-finance experience for reviewing cash flow and assets, managing financial organization, and configuring user preferences.
- **Open Question:** Confirm the primary product outcome and whether the V4 Home screen is the canonical entry point for all nine features.

### Target User Experience

- **Source Intent:** The diagrams show a page title, date/profile affordances, summary cards, grouped content, and visible actions or filters.
- **推論:** Users should be able to move between finance features, understand the current period at a glance, and distinguish data from empty, loading, and error states.
- **Open Question:** Confirm supported devices, accessibility requirements, localization, and whether the diagrams' date/profile/menu controls are mandatory.

### Navigation Structure

- **Source Intent:** Each of the eight diagrams shows a menu affordance and a feature title. No route map or menu behavior is specified.
- **推論:** Navigation should include Home and the eight requested feature areas: Transactions, Accounts, Goals, Reports, Budget, Recurring Expenses, Categories, and Settings. Existing V4 `Plan` and `Me` routes require a product decision about their future role to avoid duplicate destinations.
- **Open Question:** Confirm navigation hierarchy, labels, active state, mobile behavior, and whether Plan/Me remain visible alongside the feature routes.

## 2. Feature Specifications

## Feature: Home

### Purpose

- **Source Intent:** Home is not included in `DashboardV4FeatureIntent.md`.
- **推論:** Present a concise daily and monthly financial overview and provide entry points to common tasks.

### User Story

- **推論:** As a user, I want to see my current daily and monthly financial overview so that I can understand today's position and decide what to do next.

### UI Structure

- **Source Intent:** Not specified in the Feature Intent document.
- **推論 — Existing V4 implementation:** Home currently presents a daily decision hero, monthly progress, and today's transactions, with loading and error/retry states.
- **推論 — Candidate actions:** Navigate to feature pages and retry a failed Home request.

### Components

- **推論 — Existing:** `HomePage`, `DailyDecisionHero`, `MonthlyProgressCard`, `TodayTransactionList`, and `BottomNavigation`.
- **推論 — Existing shared shell:** `DashboardV4Navigation`, `DashboardCard`, `MetricCard`, `ProgressBar`, `FilterChip`, and `FeatureComingSoon`.
- **New（推論）:** No additional component is required to describe the current Home scope; any new chart or action component requires a confirmed requirement.

### Data Requirement

- **推論 — Existing contract fields documented in the Readiness Review:** `businessDate`, `dailySpendingLimit`, `dailySpent`, `dailyRemaining`, monthly progress, and `todayTransactions[]`.
- **推論 — Candidate relationships:** Home summary values are derived from transactions and other financial read models; the exact calculation ownership must remain defined by the backend contract.

### Interaction

- **推論:** Load Home data, retry on request failure, and navigate to other V4 features. No additional Home-specific filtering or editing behavior is specified by the Feature Intent.

### State

- **推論:** Loading, loaded/success, error with retry, and an empty-today-transactions state should be distinguishable. Whether a missing/zero daily limit is an empty state or valid data requires confirmation.

### API Dependency

- **推論 — Current status:** Ready and integrated according to the V4 implementation status and Readiness Review (`GET /api/dashboard/v4/home`).
- **Open Question — Pending:** Confirm any new Home data or actions beyond the existing contract before extending the API.

### Open Questions

- Is Home in the approved nine-feature product scope even though it is absent from the source diagrams?
- What should the daily decision hero display when there is no spending limit or no transaction data?
- Which date, time zone, and currency formatting rules apply?
- Are monthly progress and daily limit values purely server-calculated?

## Feature: Transactions

### Purpose

- **Source Intent:** Search and review transaction records with period, type, and account filters; show monthly income, expense, and net totals.
- **推論:** Support finding and reviewing transactions without implying unshown edit/delete behavior.

### User Story

- **推論:** As a user, I want to search and filter transactions and review their period totals so that I can understand and locate my activity.

### UI Structure

- **Source Intent:**
  - Header with page title, date, menu, and profile affordances.
  - Search input for note or category.
  - Previous/next month control, transaction-type filters, account filter, calendar affordance, and add-transaction action.
  - Income, expense, and net summary cards.
  - Transactions grouped by date with a daily net amount and a load-more/page indicator.
- **推論:** Responsive layout may collapse summary cards and filter controls into stacked sections; detailed breakpoints are not specified.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `MetricCard`, `FilterChip`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Search input, month selector, account selector, calendar/date-range picker, transaction row, date-group header, and pagination/load-more control.

### Data Requirement

- **推論 — Candidate entity:** `Transaction`, with candidate fields `id`, amount, transaction type, note, transaction date, account reference, and optional category reference.
- **推論 — Candidate related entities:** `Account` for account name/type; `Category` for category label; period totals and daily net values may be server aggregates or client-derived values. Ownership and calculation rules require contract confirmation.

### Interaction

- **Source Intent:** Search note/category; move between months; filter all/expense/income and by account; open a calendar; add a transaction; load more results.
- **推論:** Search and filters should update the displayed result set and period totals consistently. Whether filters combine with AND/OR semantics and whether calendar selection changes the month or selects an arbitrary range is undecided.

### State

- **推論:** Loading list and summary; empty period; no search matches; API error with retry; loaded transaction groups; pagination loading/end state; pending search/filter state.

### API Dependency

- **推論 — Current status:** Core read and Quick Create APIs are marked Ready in the Readiness Review (`GET /api/transactions`, `POST /api/transactions/quick`).
- **Open Question — Pending:** Confirm query parameters and server behavior for note/category search, month/date filtering, account/type filtering, calendar selection, ordering, and pagination. The diagram does not show transaction edit/delete.

### Open Questions

- Is search server-side, and which fields and matching rules are supported?
- What is the default period and how are month boundaries/time zones defined?
- Does the calendar select a single day, a date range, or navigate to a month?
- What page size, ordering, and load-more behavior are required?
- Are totals based on all filtered results or only the loaded page?
- What does selecting a transaction do? Are edit/delete actions in scope?

## Feature: Accounts

### Purpose

- **Source Intent:** Show total/net assets, balances grouped by Cashflow, Saving, and Invest account types, and account actions.
- **推論:** Help users review account balances and manage accounts.

### User Story

- **推論:** As a user, I want to see my balances by account and account type so that I can understand where my assets are held.

### UI Structure

- **Source Intent:**
  - Total/net-assets card with a visibility control and an add-account action.
  - Summary totals for Cashflow, Saving, and Invest.
  - Account groups with account name/type and balance.
  - Account rows show a default quick-entry account label where applicable, plus view-details and edit actions.
  - Empty states for Saving and Invest groups with add-account actions.
- **推論:** The visibility control may conceal monetary values; the effect and persistence of that preference require confirmation.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `MetricCard`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Asset summary, account-type group, account row, balance-visibility control, empty account-type state, and account create/edit form.

### Data Requirement

- **推論 — Candidate entity:** `Account`, with candidate fields `id`, name, account type, balance, and a default-expense-account indicator.
- **推論 — Candidate relationships:** Accounts are associated with transactions; balances and total assets may be computed or supplied by a read model. Whether account types have different balance rules is not specified.

### Interaction

- **Source Intent:** Add an account; view account details; edit an account; toggle balance visibility; identify the default quick-entry account.
- **推論:** Account detail navigation should preserve a way back to the Accounts page. Create/edit validation and confirmation behavior are not defined.

### State

- **推論:** Loading asset summary and account groups; empty groups by account type; error with retry; loaded balances; create/edit pending, success, and validation-error states if account mutations are approved.

### API Dependency

- **推論 — Current status:** Me Assets read contract is Ready according to the Readiness Review (`GET /api/me/assets`).
- **Open Question — Pending/Partial:** The current readiness evidence supports reading assets/accounts, but does not establish account create/edit APIs or the visibility preference contract.

### Open Questions

- Does “總資產 (淨資產)” mean the same metric, or are assets and net worth distinct?
- Which account types are supported and how are balances calculated?
- Can accounts be archived/deleted, and what happens to linked transactions?
- What fields and validation rules are required to create/edit an account?
- What does “查看明細” open?
- Is balance hiding local UI state or a persisted user preference?
- How is the default quick-entry account selected and constrained?

## Feature: Goals

### Purpose

- **Source Intent:** Summarize active goals and cumulative saved amount/achievement rate; list goals with target dates, current/target amounts, progress, remaining amount, and an add-funds action.
- **推論:** Help users track progress toward named financial goals.

### User Story

- **推論:** As a user, I want to see progress and target dates for my goals so that I can understand how close each goal is to completion.

### UI Structure

- **Source Intent:**
  - Summary cards for active-goal count and cumulative saved/target progress.
  - Add-goal action.
  - Goal cards with name/icon, target date, current and target amounts, percentage/progress bar, remaining amount, and add-funds action.
- **推論:** Goal creation/editing and the add-funds flow would require separate forms or dialogs, but neither is specified.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `MetricCard`, `ProgressBar`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Goal card, goal creation/edit form, and add-funds flow component if that action is approved.

### Data Requirement

- **推論 — Candidate entity:** `Goal`, with candidate fields `id`, name, optional icon, target amount, current amount, target date, and status.
- **推論 — Candidate related concept:** A contribution/funding record may be needed to explain changes to current amount, but the source does not establish a `GoalContribution` entity or its relation to transactions.

### Interaction

- **Source Intent:** Add a goal and invoke add-funds from a goal card.
- **推論:** Goal detail, editing, completion, cancellation, and contribution history are possible interactions but are not specified and must not be assumed.

### State

- **推論:** Loading goals; no goals; error with retry; loaded progress; pending/success/error states for any approved goal mutation.

### API Dependency

- **推論 — Current status:** Planned/Not Ready in the Feature Implementation Status; the Readiness Review places Goal/Plan after earlier integration steps.
- **Open Question — Pending:** Goal read/write contract, contribution semantics, and aggregate calculation rules.

### Open Questions

- What qualifies a goal as active, completed, paused, or archived?
- How are progress and remaining amounts calculated, including overfunding?
- Does “投入資金” create a transaction, a goal contribution, or only update a goal balance?
- Is goal funding in scope for the first Goals release?
- What are the rules for editing, deleting, completing, or reopening a goal?
- Are target dates optional, and how are past-due goals presented?

## Feature: Reports

### Purpose

- **Source Intent:** Compare spending trends, income and expenses, category spending proportions, and net-asset trends over selectable periods.
- **推論:** Help users inspect historical financial patterns and export report data.

### User Story

- **推論:** As a user, I want to review financial summaries over a selected period so that I can understand changes in my spending and assets.

### UI Structure

- **Source Intent:**
  - Period selector for week, month, recent six months, and year.
  - CSV export action.
  - Cards for daily spending trend, income/expense comparison and savings rate, category spending share, and net-asset trend.
- **推論:** Charts need labels, accessible summaries, and an empty-data presentation; exact chart forms are illustrative.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `MetricCard`, `FilterChip`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Period selector, chart components, report legend/accessible data table, and export action/feedback.

### Data Requirement

- **推論 — Candidate inputs:** Transactions with amount, type, date, category reference, and account reference; account balance snapshots or equivalent data for net-asset history.
- **推論 — Candidate aggregates:** Daily spending series, income/expense totals, savings-rate numerator/denominator, category totals/shares, and periodic net-asset values. The formulas and source of truth are not specified.

### Interaction

- **Source Intent:** Select a report period and export CSV.
- **推論:** Changing a period should refresh all report cards consistently. Export format, columns, and whether export respects all active filters require confirmation.

### State

- **推論:** Loading report sections; empty period/no transactions; partial chart data; error with retry; loaded charts; export pending/success/failure.

### API Dependency

- **推論 — Current status:** Planned/Not Ready in the Feature Implementation Status.
- **Open Question — Pending:** Reporting endpoints/read models, supported periods, aggregation definitions, historical asset data, and export contract.

### Open Questions

- What exact date ranges do “本週”, “本月”, “近半年”, and “本年” represent?
- How are income, expense, savings rate, and net assets calculated?
- Which time zone and currency rounding rules apply?
- Is net-asset history available at the required granularity?
- Which columns and data format should CSV export contain?
- Should reports include transfers, refunds, or uncategorized transactions?

## Feature: Budget

### Purpose

- **Source Intent:** Show monthly available budget, daily suggested spending, remaining days, fixed expenses reserved, and category budget progress.
- **推論:** Help users monitor spending against monthly and category-level limits.

### User Story

- **推論:** As a user, I want to compare spending with my budget so that I can understand remaining availability during the selected period.

### UI Structure

- **Source Intent:**
  - Previous/next month selector and edit-total-budget action.
  - Summary card with available budget, suggested daily spending, days remaining, used percentage, and fixed expenses reserved.
  - Category progress list with remaining amount, used percentage, status label, and add-category action.
- **推論:** Status colors and progress-bar treatment should have accessible text equivalents; threshold meanings are not established by the diagram.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `MetricCard`, `ProgressBar`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Month selector, budget summary, category budget row, total-budget form, and category budget form.

### Data Requirement

- **推論 — Candidate entities:** Monthly `Budget` and per-category budget allocation, related to a `Category`; transactions provide actual spending, and recurring expenses may contribute to a reserved amount.
- **推論 — Candidate fields/aggregates:** Period, budget amount, category allocation, spent amount, remaining amount, reserved fixed expenses, and days remaining. Their definitions and relationships require confirmation.

### Interaction

- **Source Intent:** Change month; edit total budget; add a category; review category progress.
- **推論:** Editing allocations and recalculating progress should be atomic from the user’s perspective, but save timing and validation are not specified.

### State

- **推論:** Loading selected month; no budget configured; no category allocations; error with retry; loaded budget; save pending/success/validation error.

### API Dependency

- **推論 — Current status:** Planned/Not Ready in the Feature Implementation Status.
- **Open Question — Pending:** Budget read/write contract and definitions for available, spent, reserved, remaining, and suggested daily spending.

### Open Questions

- Is budget monthly only, and what happens when navigating to a month with no budget?
- Does “available budget” include or exclude reserved fixed expenses?
- How is “今日建議可花” calculated, and how are remaining days counted?
- Are category budgets required to sum to the total budget?
- What determines “安全”, “警告”, and “超支” thresholds?
- Can category allocations exceed the total budget or be left unallocated?
- How are refunds, transfers, and uncategorized expenses treated?

## Feature: Recurring Expenses

### Purpose

- **Source Intent:** Show monthly recurring totals, paid and pending amounts/counts, and a list of recurring items with due day, amount, linked account, payment status, and edit action.
- **推論:** Help users track recurring obligations and their status for a month.

### User Story

- **推論:** As a user, I want to review recurring expenses and their payment status so that I can see what has been paid and what is pending.

### UI Structure

- **Source Intent:**
  - Summary cards for monthly total, paid amount/count, and pending amount/count.
  - Add-recurring-expense action.
  - Monthly list with paid checkbox, name, due day, amount, linked account, payment status, and edit action.
- **推論:** The diagram’s “明天到期” text implies relative due-date messaging, but notification/reminder behavior is not specified.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `MetricCard`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Recurring-expense row, add/edit form, month summary, and payment-status control.

### Data Requirement

- **推論 — Candidate entity:** Recurring expense/fixed commitment, with candidate fields `id`, name, amount, recurrence frequency, due day, account reference, and active state.
- **推論 — Candidate related concept:** A period-specific payment/occurrence record may be needed to distinguish a recurring definition from whether it has been paid this month. The source does not establish this model.

### Interaction

- **Source Intent:** Add an item, edit an item, and toggle its paid checkbox.
- **推論:** Toggling paid may record a payment or only update a status; the UI action cannot determine that domain behavior.

### State

- **推論:** Loading the selected period; no recurring items; no pending items; error with retry; loaded list; add/edit/payment update pending, success, or validation error.

### API Dependency

- **推論 — Current status:** Planned/Not Ready in the Feature Implementation Status.
- **Open Question — Pending:** Recurring definition, occurrence/payment, and status update contracts.

### Open Questions

- Which recurrence frequencies are supported beyond monthly?
- Does marking an item paid create a transaction, link an existing transaction, or only change status?
- How are due dates adjusted for short months, weekends, or holidays?
- Are payment reminders or automatic transactions in scope?
- How are recurring items edited, paused, skipped, or ended?
- Which summary period and timezone determine “本月” and “明天到期”?

## Feature: Categories

### Purpose

- **Source Intent:** Separate expense and income categories; display category name/icon, default status, monthly transaction count, and controls to add, edit, or set a default.
- **推論:** Let users organize transaction classifications and identify a preferred category.

### User Story

- **推論:** As a user, I want to review and manage categories so that transactions can be classified consistently.

### UI Structure

- **Source Intent:**
  - Expense/income category tabs and add-category action.
  - Category cards with icon, name, default indicator, current-month transaction count, edit action, and set-default action.
- **推論:** The interaction design for system/default categories and category deletion is not described.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `FilterChip`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Category card, add/edit form, and confirmation flow for any destructive operation if approved.

### Data Requirement

- **推論 — Candidate entity:** `Category`, with candidate fields `id`, name, icon, type (expense/income), and default indicator.
- **推論 — Candidate relationships:** Categories are referenced by transactions; monthly transaction count is a period aggregate. A user preference may identify a default category, but scope and per-type behavior are unresolved.

### Interaction

- **Source Intent:** Switch expense/income categories; add; edit; set a category as default.
- **推論:** Switching category type should select a corresponding list; whether default selection is unique globally or per transaction type must be confirmed.

### State

- **推論:** Loading selected type; empty category list; error with retry; loaded categories; add/edit/default update pending, success, and validation-error states.

### API Dependency

- **推論 — Current status:** Partial in the Feature Implementation Status.
- **Open Question — Pending:** Confirm which category read/write operations, default behavior, and monthly usage aggregates are covered by existing APIs.

### Open Questions

- Are system categories editable, deletable, or only user categories?
- Is one default category allowed per user, per transaction type, or per account?
- Does the monthly count include all transactions or only the selected category type?
- How are categories with existing transactions renamed, merged, or deleted?
- Are icons selected from a fixed catalog or user-provided?
- Are Uncategorized transactions represented by a category record or a null category?

## Feature: Settings

### Purpose

- **Source Intent:** Manage personal profile, quick-entry defaults, appearance/privacy/language, data export, account security, and sign-out.
- **推論:** Provide a consolidated place to manage user-level preferences and account actions.

### User Story

- **推論:** As a user, I want to manage my profile, preferences, and account controls so that the application behaves according to my needs.

### UI Structure

- **Source Intent:**
  - Personal profile card with edit action.
  - Quick-entry defaults for an expense account limited to Cashflow and a default expense category; the diagram labels this section `UserPreference`.
  - Appearance/privacy controls for dark mode, hiding monetary amounts, and display language.
  - Data export in CSV/JSON, account security/password management, and sign-out.
- **推論:** Sections may require separate save/confirmation flows; the diagram does not specify persistence timing.

### Components

- **推論 — Existing shared components:** `DashboardV4Navigation`, `DashboardCard`, `FilterChip`, `FeaturePageHeader`, and `FeatureComingSoon`.
- **New（推論）:** Profile editor, account/category preference selectors, theme/privacy/language controls, export action/feedback, security entry point, and sign-out confirmation if required.

### Data Requirement

- **Source Intent:** The quick-entry defaults section names `UserPreference` and shows default expense account/category.
- **推論 — Candidate entities:** User/profile and `UserPreference`; candidate preference fields include default expense account ID and default category ID. Theme, privacy, and language may be persisted preferences or local settings; this is undecided.
- **推論 — Candidate relationships:** Default account/category references must resolve to valid user-owned records; validation and ownership rules must follow the approved backend contract.

### Interaction

- **Source Intent:** Edit profile; choose default account/category; toggle dark mode and privacy display; choose language; export data; manage security/password; sign out.
- **推論:** Preference changes need explicit save behavior and feedback. Export and security actions may leave the page or invoke a system flow; their behavior is not specified.

### State

- **推論:** Loading profile/preferences; unavailable or empty preference options; error with retry; loaded settings; save pending/success/validation error; export pending/success/failure; sign-out confirmation.

### API Dependency

- **推論 — Current status:** Partial in the Feature Implementation Status; the Readiness Review identifies existing UserPreference work but does not establish full coverage for all settings sections.
- **Open Question — Pending:** Confirm profile, preference read/write, export, security, theme/privacy/language persistence, and sign-out contracts. Do not infer that every visual control has a backend endpoint.

### Open Questions

- Which settings are persisted server-side, device-side, or only for the current session?
- Are default expense account and category mandatory, optional, or cleared when their records become invalid?
- Is the account selector restricted to active user-owned Cashflow accounts?
- Does hiding amounts apply to every V4 page and persist across devices?
- Which languages are supported?
- What data is included in CSV/JSON export, and how is sensitive data protected?
- Is password/account security managed in this application or an external identity provider?
- What confirmation and post-sign-out navigation are required?

## Cross-Feature Product Decisions

- **Open Question:** Confirm the nine-feature scope and add an approved Home intent, since the current visual source covers only eight feature pages.
- **Open Question:** Define global date/timezone and currency/rounding conventions.
- **Open Question:** Approve navigation destinations and the role of existing Plan/Me routes.
- **Open Question:** Define shared loading, empty, error, retry, success, and permission-denied behavior.
- **Open Question:** Confirm which visual numbers are sample-only and ensure no example value is treated as a product default.
- **Open Question:** Approve state ownership per feature: server data, transient page UI state, and any cross-page UI state.

