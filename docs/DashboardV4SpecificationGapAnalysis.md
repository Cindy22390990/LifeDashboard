# Specification Gap Analysis

**Basis:** `DashboardV4FeatureIntent.md`, `DashboardV4FeatureImplementationStatus.md`, and `DashboardV4FrontendIntegrationReadinessReview.md`.  
**Interpretation（推論）:** “Intent Complete” measures whether product behavior is sufficiently specified, not whether a diagram exists. “UI Complete” reflects the current implementation status; a skeleton is not a complete feature UI.

## Gap Analysis（推論）

| Feature | Intent Complete | UI Complete | API Ready | Missing |
|---|---|---|---|---|
| Home | No — absent from Feature Intent | Yes — current Home integration is marked Done | Yes | Approved Home intent, empty-data semantics, and confirmation of overview calculations |
| Transactions | Partial — screen and controls are depicted; behavior is unresolved | No — Skeleton | Yes — core read and Quick Create contracts are ready | Search/filter parameters, calendar semantics, totals vs pagination, ordering, and row actions |
| Accounts | Partial — summary and row actions are depicted | No — Skeleton | Yes for Me Assets read; account CRUD unconfirmed | Net-assets definition, balance rules, account create/edit/details contracts, and visibility persistence |
| Goals | Partial — summary, progress, add, and funding affordances are depicted | No — Skeleton | No — Planned | Goal lifecycle, progress calculations, contribution semantics, and scope of funding action |
| Reports | Partial — periods, charts, and CSV export are depicted | No — Skeleton | No — Planned | Metric formulas, period boundaries, historical asset data, export fields, and filtering rules |
| Budget | Partial — monthly and category summaries/actions are depicted | No — Skeleton | No — Planned | Available/reserved budget definitions, daily suggestion formula, category allocation rules, and status thresholds |
| Recurring Expenses | Partial — monthly totals, due items, payment status, and edit are depicted | No — Skeleton | No — Planned | Recurrence rules, payment/transaction relationship, due-date edge cases, and lifecycle actions |
| Categories | Partial — type tabs, counts, defaults, add/edit are depicted | No — Skeleton | Partial | System/user category rules, default scope, deletion/merge behavior, and required API operations |
| Settings | Partial — sections and controls are depicted | No — Skeleton | Partial | Persistence ownership, profile/preferences coverage, privacy/language behavior, export, security, and sign-out contracts |

## A. Source Coverage

- `DashboardV4FeatureIntent.md` contains eight diagrams: Transactions, Accounts, Goals, Reports, Budgets, Fixed Commitments, Categories, and Settings.
- Home is not depicted in that source. Its current UI and API readiness are recorded separately in the implementation status and frontend readiness review.
- Diagram amounts, dates, counts, percentages, status labels, and example names are visual examples only unless a product decision explicitly adopts them.

## B. API Design Readiness（推論）

- **推論 — Ready to proceed against existing contracts:** Home; Transactions core list/Quick Create; Accounts read-only asset summary/list.
- **推論 — Design only the missing contract portions:** Transactions search/filter/calendar semantics; Accounts create/edit/details; Categories; Settings.
- **推論 — Not ready for feature API design approval:** Goals, Reports, Budget, and Recurring Expenses until the product decisions in the specification are answered. Preliminary domain discovery may proceed, but must not be treated as an approved contract.

## C. Cross-Feature Decisions Required

1. **Open Question:** Add/approve Home intent and confirm the nine-feature navigation structure, including the future of existing Plan and Me routes.
2. **Open Question:** Define global date/timezone and currency conventions.
3. **Open Question:** Decide server-vs-client ownership for aggregates and report/budget calculations.
4. **Open Question:** Approve empty, loading, error, success, retry, and permission-denied patterns.
5. **Open Question:** Confirm the semantics of each depicted action before endpoint or mutation design.

## B. Readiness Recommendation（推論）

The visual intent is sufficient to continue UI skeleton work, but it is not a complete product specification for implementation. Home, Transactions core read/Quick Create, and Accounts read-only assets have usable existing API foundations. Full implementation of the other features should wait for the missing product decisions and API contracts listed above.
