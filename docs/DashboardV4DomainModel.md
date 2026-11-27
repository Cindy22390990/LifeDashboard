# Dashboard V4 Domain Model

**Status:** Draft — domain review required  
**Evidence sources:** `DashboardV4ProductSpecification.md`, `DashboardV4ApiContractPlan.md`, `DashboardV4BackendContract.md`, `DashboardV4SpecificationGapAnalysis.md`, and `DashboardV4FrontendIntegrationReadinessReview.md`.

## Overview

This document organizes domain concepts and relationships that are explicitly described by the current V4 product and API documents, while identifying reasonable but unapproved candidate models and unresolved questions.

This is a conceptual domain review only. It is **not** a database schema, API DTO specification, EF Entity definition, aggregate-root decision, migration plan, or Clean Architecture change. Fields described in API contracts are contract/read-model fields and must not automatically be treated as persisted entity properties.

Evidence categories:

- **Confirmed:** Explicitly described in one or more reviewed documents. “Confirmed” records what the documents say; it does not certify that production code or persistence matches them.
- **Candidate（推論）:** A possible conceptual model inferred from the Product Specification or API Contract Plan. Every candidate item is marked `（推論）`; none is an approved business rule.
- **Open Questions:** Information that the reviewed documents do not establish.

For the evidence count at the end, one bullet under an aggregate's Confirmed, Candidate, or Open Questions subsection counts as one item. Field tables, relationship diagrams, matrix rows, and conflict records are not included in that count.

## Domain Relationships

### Confirmed contract-level references

These are documented API/read-model or resolver references, not a physical schema:

```text
Transaction API contract
    ├── account reference ──> Account
    └── optional/TBD category reference ──> Category

UserPreference resolver
    ├── DefaultExpenseAccountId ──> Account
    └── DefaultCategoryId ──> Category

Planned Budget summary
    └── categories[] / categoryId ──> Category

Home read model
    ├── TransactionQuery ──> daily spending / today's transactions
    ├── BudgetService ──> daily spending limit / remaining amount
    └── AllocationService or Query ──> monthly saved / invested progress
```

### Candidate domain relationships（推論）

```text
Budget
    ├── category allocations（推論）──> Category
    ├── realized spending（推論）─────> Transaction
    └── reserved commitments（推論）──> Recurring Expense

Goal
    └── funding/contribution records（推論）──> funding source TBD

Report read model（推論）
    ├── aggregates（推論）──> Transaction
    ├── groups by（推論）───> Category
    └── historical values（推論）──> Account balance history TBD

Recurring Expense（推論）
    ├── payment occurrence（推論）──> payment/transaction behavior TBD
    └── account reference（推論）──> Account
```

The documents do not confirm a direct Account-to-Goal relationship, an automatic Recurring Expense-to-Transaction relationship, or a persisted Report aggregate. These must not be inferred from the diagrams alone.

## Aggregate

### Transaction

#### Purpose

**Confirmed:** Reviewed contracts describe transaction listing, Quick Create, full transaction creation, and financial summaries.  
**Candidate（推論）:** Treat the recorded financial activity as a Transaction domain concept; the aggregate boundary is not specified by the reviewed documents.

#### Confirmed

- The Backend Contract describes transaction contract fields including an identifier, amount, transaction type, transaction date, and nullable note; these are DTO/read-model statements, not a complete entity definition.
- Quick Create resolves the account, category, and business date in the backend; it accepts Expense or Income and rejects Transfer.
- Transaction list income/expense totals are backend-calculated, exclude transfers, and are not to be calculated from a partial frontend page.

#### Candidate

- A Transaction aggregate could represent a financial record with references to an Account and, optionally, a Category（推論）.
- Query filters and grouped daily totals could be read-model concerns rather than fields or behavior owned by the Transaction aggregate（推論）.

#### Open Questions

- Is Transaction the aggregate root, and which invariants must be enforced when creating, editing, or deleting a transaction?
- Are account/category references required for all transaction types, and how should a missing category be represented?
- Which date, amount precision, and note constraints are domain rules rather than API validation details?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| `id`, `amount`, `transactionType`, `transactionDate`, `note` | Confirmed contract/read-model fields | `note` is nullable; the contract does not define a complete entity schema. |
| `accountId`, `categoryId`, `accountName`, `categoryName` | Confirmed as referenced or optional/TBD contract fields | Exact response presence and nullability are unresolved. |
| Transaction-to-Account and optional Transaction-to-Category references | Candidate（推論） | Conceptual relationship; do not infer database foreign keys or deletion behavior. |

#### Relationships

- **Confirmed contract-level:** Quick Create uses resolved account/category values; transaction list contract may expose account/category references.
- **Candidate（推論）:** Transaction references one Account and optionally one Category; transaction data may feed Home, Budget, Reports, and category usage summaries.

#### Business Notes

- **Confirmed:** The Backend Contract assigns financial aggregation to backend query/services and limits frontend responsibility to display/formatting.
- **Open Question:** Transfer balance rules, deletion behavior, and category reassignment rules must be verified against an authoritative transaction contract; none is inferred here.

### Account

#### Purpose

**Confirmed:** The Me Assets read contract exposes account items and a total-assets summary.  
**Candidate（推論）:** Treat an account as a user's financial resource; the source documents do not define its full lifecycle or persistence shape.

#### Confirmed

- `MeAssetsDto` contains `totalAssets` and an `accounts[]` list; each account read item documents `id`, `name`, `accountType`, and `balance`.
- Documented account types are `Cashflow`, `Saving`, and `Invest`.
- The Me Assets calculation is user-scoped and excludes deleted and system accounts; the frontend must not sum account items to recreate `totalAssets`.

#### Candidate

- An Account aggregate could own account identity/type and expose a balance read model, with transaction references contributing to account history（推論）.
- The default quick-entry account is more appropriately modeled as a UserPreference reference than as an Account property（推論）.

#### Open Questions

- Are “total assets” and “net assets” synonymous in this product, and are liabilities represented?
- Are account create/edit actions in scope, and what are the allowed fields and balance-adjustment rules?
- What does the account “view details” action mean: account metadata, transaction history, or both?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| `id`, `name`, `accountType`, `balance` | Confirmed Me Assets contract fields | These fields describe a response item, not necessarily persisted property names. |
| `totalAssets` | Confirmed aggregate/read-model field | The contract defines it as a backend sum of eligible account balances; it is not an Account field. |
| Opening balance, status, visibility, default marker | Candidate（推論） | Not confirmed as Account fields; default selection is referenced through UserPreference. |

#### Relationships

- **Confirmed contract-level:** UserId scoping applies to the Me Assets query; Transaction contracts refer to accounts.
- **Candidate（推論）:** Account is referenced by Transactions and may be referenced by UserPreference as the default expense account.

#### Business Notes

- **Confirmed:** The account-type subtotals shown in the intent are not currently documented as fields in `MeAssetsDto`.
- **Confirmed:** Account balances and asset totals are backend-owned read values under the current Me Assets contract.
- **Open Question:** The account balance formula and account CRUD behavior are not defined by the reviewed documents.

### Category

#### Purpose

**Confirmed:** The Product Intent depicts expense/income category views and category actions; the Backend Contract does not define a Category API or full domain model.  
**Candidate（推論）:** Treat Category as a classification concept for transaction records.

#### Confirmed

- The transaction contract references `categoryId`/`categoryName` as optional or TBD fields; full transaction creation keeps `categoryId` optional.
- The Settings intent names `DefaultCategoryId`, and the Quick Create resolver documents a default-category resolution path.

#### Candidate

- A Category concept could include an identifier, display name, transaction type, and optional icon, with usage counts calculated for a selected period（推論）.
- A default category could be represented through UserPreference rather than a mutable `isDefault` flag on Category（推論）.

#### Open Questions

- Are categories user-owned, system-provided, or a mixture, and can system categories be edited?
- Is the default unique per user, transaction type, or another scope?
- How do category rename, archive/delete, or merge operations affect existing transactions?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| `categoryId`, `categoryName` | Confirmed as optional/TBD transaction contract references | No standalone Category response DTO is defined. |
| `DefaultCategoryId` | Confirmed preference/resolver reference | The exact preference API and persistence model are not defined. |
| Name, type, icon, default indicator, period usage count | Candidate（推論） | The intent depicts these concepts; it does not establish entity fields or validation rules. |

#### Relationships

- **Confirmed contract-level:** Transaction contracts can reference a category; UserPreference resolver can use a default category.
- **Candidate（推論）:** Category is referenced by Transactions and may be used in Budget allocations and category Reports.

#### Business Notes

- **Confirmed:** The intent shows expense and income category tabs.
- **Open Question:** The tabs do not establish canonical domain enum values or transfer-category behavior.
- **Candidate（推論）:** Monthly transaction count is better treated as a period aggregate than as a Category property.

### Budget

#### Purpose

**Confirmed:** A planned Budget summary API describes monthly budget and category progress values.  
**Candidate（推論）:** Treat Budget as a domain context for those values; no persisted Budget entity or aggregate boundary is confirmed.

#### Confirmed

- The Backend Contract documents a planned `GET /api/budgets/current` response with `totalBudget`, `totalSpent`, `remainingBudget`, `fixedCommitmentReserved`, and category summary items.
- The Backend Contract assigns budget totals and remaining-amount calculation to the backend BudgetService.

#### Candidate

- A Budget concept could be scoped to a period and contain a total limit plus per-category allocations（推論）.
- Budget read values could aggregate Transactions and fixed-commitment data without those records being owned by the Budget aggregate（推論）.

#### Open Questions

- What is the period identity and lifecycle for a Budget, including months with no configured budget?
- How do `totalSpent`, `remainingBudget`, and `fixedCommitmentReserved` relate mathematically?
- Are category allocations required, optional, or constrained to sum to the total budget?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| `totalBudget`, `totalSpent`, `remainingBudget`, `fixedCommitmentReserved` | Confirmed planned API read-model fields | These are contract fields, not confirmed persisted Budget properties. |
| `categories[]` with `categoryId`, `categoryName`, `limit`, `spent`, `remaining` | Confirmed planned API read-model fields | Empty arrays are documented as `[]`. |
| Period, allocation, daily suggested spend, remaining days, warning status | Candidate（推論） | Some appear in the intent; definitions and entity ownership are unresolved. |

#### Relationships

- **Confirmed contract-level:** Planned Budget summary includes category references and fixed-commitment reserved amount.
- **Candidate（推論）:** Budget summaries aggregate transaction spending, category allocations, and recurring/fixed commitments.

#### Business Notes

- **Confirmed:** The API is marked Planned; a response DTO does not establish a database table or aggregate root.
- **Open Question:** The intent's “今日建議可花” and reserved-expense presentation need definition before being treated as Budget invariants.

### Goal

#### Purpose

**Confirmed:** The intent depicts goal summaries/progress, and the Backend Contract describes a planned Goal summary API.  
**Candidate（推論）:** Treat Goal as a domain concept behind those views; its aggregate boundary is not specified.

#### Confirmed

- The Backend Contract documents a planned `GET /api/goals/summary` with `id`, `name`, `targetAmount`, `currentAmount`, and `progressPercent`; the current amount/progress are attributed to a `GoalFundingSummary` read model.
- The Product Intent depicts goal names/icons, target dates, current and target amounts, remaining amounts, progress, and add/add-funds actions; it does not define their business semantics.

#### Candidate

- A Goal concept could include a target date, optional icon, lifecycle status, and target amount in addition to the documented summary values（推論）.
- A funding/contribution record or read model may be needed to explain `currentAmount`; its relationship to Transaction or Account is not established（推論）.

#### Open Questions

- What are the goal lifecycle states and rules for completion, cancellation, or editing?
- Does “投入資金” create a transaction, a contribution record, or update another balance?
- How are current amount, progress, remaining amount, and overfunding calculated?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| `id`, `name`, `targetAmount`, `currentAmount`, `progressPercent` | Confirmed planned API read-model fields | `currentAmount` and `progressPercent` are described as GoalFundingSummary output. |
| Target date, icon, remaining amount, status | Candidate（推論） | Depicted or implied by the intent but absent from the documented Goal DTO. |
| Funding/contribution history | Candidate（推論） | No record schema or transaction relationship is confirmed. |

#### Relationships

- **Confirmed contract-level:** Goal summary uses a GoalFundingSummary read model for current amount and progress.
- **Candidate（推論）:** Goal may be related to funding/contribution records; a direct Account or Transaction relationship remains undecided.

#### Business Notes

- **Confirmed:** Budget and Goal are documented as separate bounded contexts.
- **Confirmed:** The planned summary DTO does not establish a Goal entity schema or mutation contract.
- **Open Question:** The existing UI name `Plan` must not be used to imply a combined backend Plan domain; confirm any future API boundary against the documented separation.

### Report

#### Purpose

**Confirmed:** The Product Intent depicts financial summaries and trends; the reviewed documents do not define a Report aggregate or API.
**Candidate（推論）:** Treat Reports as a query/read-model capability unless product/domain review establishes otherwise.

#### Confirmed

- The Product Intent depicts period selection, spending trend, income/expense comparison, category shares, net-asset trend, and CSV export.
- The Backend Contract assigns financial aggregation to backend services/queries and does not define a Reports endpoint or Report DTO.

#### Candidate

- Report may be a read-model/query boundary rather than a persisted domain aggregate（推論）.
- Report outputs may aggregate Transactions, Categories, and historical Account balance data（推論）.

#### Open Questions

- Which report metrics and date boundaries are required, and what are their formulas?
- What source data supports historical net-asset trends?
- What columns, filters, and security rules apply to export?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| Selected period and the named chart/summary outputs | Confirmed product-intent concepts | The diagram does not define an API shape or exact metric values. |
| Time-series points, category totals/shares, savings rate, export rows | Candidate（推論） | Candidate read-model fields; formulas and source-of-truth are unresolved. |

#### Relationships

- **Confirmed:** No domain entity relationship or persisted Report model is defined in the reviewed documents.
- **Candidate（推論）:** Report queries may depend on Transaction and Category data, plus Account balance history for net-asset trends.

#### Business Notes

- **Open Question:** A Report entity/table is not justified by the page alone; confirm persistence needs before modeling one.
- **Confirmed:** The Product Intent's example chart values and percentages are illustrative, not confirmed rules.

### Recurring Expense

#### Purpose

**Confirmed:** The intent depicts monthly recurring/fixed obligations, due dates, amounts, accounts, payment states, and actions.
**Candidate（推論）:** Keep a recurring definition distinct from any period-specific payment record until product rules are approved.

#### Confirmed

- The Product Intent depicts monthly recurring totals, paid/pending amounts and counts, due days, amounts, linked accounts, payment status, and add/edit/toggle actions.
- The planned Budget response includes `fixedCommitmentReserved`; it does not define a recurring expense entity, payment occurrence, or endpoint.

#### Candidate

- A recurring definition and a separate period-specific occurrence/payment concept may be needed to represent both recurrence and monthly paid state（推論）.
- A recurring item may reference an Account; a payment occurrence may create or link to a Transaction, but neither relationship is confirmed（推論）.

#### Open Questions

- Which recurrence frequencies and due-date adjustment rules are supported?
- Does marking an item paid create a Transaction, link an existing Transaction, or only change status?
- What are the lifecycle rules for pause, skip, edit, end, and historical occurrences?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| Name, amount, due day, linked account label, paid/pending presentation | Confirmed product-intent concepts | These are visible UI concepts, not a confirmed entity schema. |
| Frequency, active state, occurrence date, payment reference, status transitions | Candidate（推論） | The recurrence and payment model is not defined. |
| `fixedCommitmentReserved` | Confirmed planned Budget read-model field | It does not prove a relationship to a particular recurring entity. |

#### Relationships

- **Confirmed:** No Recurring Expense-to-Transaction or Recurring Expense-to-Account domain relationship is defined by the Backend Contract.
- **Candidate（推論）:** Recurring Expense may reference Account; an occurrence may relate to a Transaction and contribute to Budget reserved amounts.

#### Business Notes

- **Open Question:** A paid checkbox does not establish automatic transaction creation or balance-update behavior.
- **Confirmed:** The Budget reserved amount is a summary field in a planned API contract.
- **Open Question:** The reserved-amount formula and its relationship to recurring items remain undecided.

### Settings

#### Purpose

**Confirmed:** Settings depicts profile and preference controls, including defaults named `UserPreference`.
**Candidate（推論）:** Treat Settings as a UI grouping over preference/profile capabilities; it is not confirmed as one domain aggregate.

#### Confirmed

- The Settings intent names `UserPreference` and depicts a default expense account restricted to Cashflow plus a default expense category.
- The Backend Contract's Quick Create resolver uses default account/category preferences and validates account ownership, deletion, system status, and Cashflow type as documented.

#### Candidate

- A UserPreference concept could reference a default expense Account and a default Category（推論）.
- Theme, privacy display, and language may be persisted preferences or client/UI state; their storage boundary is not established（推論）.

#### Open Questions

- Which settings belong to backend UserPreference versus client-local UI state?
- Is default category selection global or scoped by transaction type, and how is a stale reference handled?
- Which profile, export, security, language, privacy, and sign-out operations are owned by this application?

#### Fields

| Field/concept | Evidence status | Notes |
|---|---|---|
| `DefaultExpenseAccountId`, `DefaultCategoryId` | Confirmed preference/resolver references | The complete UserPreference DTO/entity and settings API are not defined in the reviewed Backend Contract. |
| Default account Cashflow restriction | Confirmed documented resolver rule | Applies to the resolver behavior, not a general Account invariant. |
| Theme, privacy visibility, language, profile fields | Candidate（推論） | Depicted in the intent; persistence and field contracts are TBD. |

#### Relationships

- **Confirmed contract-level:** UserPreference default references are used by Quick Create to resolve Account and Category.
- **Candidate（推論）:** UserPreference may be a separate user-scoped aggregate or preference record; profile/security data may be owned by another identity boundary.

#### Business Notes

- **Open Question:** Settings is not confirmed as a single aggregate root.
- **Confirmed:** Resolver validation is documented for Quick Create.
- **Open Question:** Resolver validation does not define the full Settings update contract or all preference lifecycle rules.

## Domain Dependency Matrix（推論／契約參照）

“Used By” entries marked **（推論）** are candidate read-model dependencies, not confirmed ownership.

| Domain | Depends On | Used By |
|---|---|---|
| Transaction | Account reference (Confirmed contract); optional/TBD Category reference (Confirmed contract) | Home read model (Confirmed); Budget and Report queries（推論）; Category usage counts（推論） |
| Account | User-scoped Me Assets contract (Confirmed); transaction account reference (Confirmed/TBD) | Me Assets and Quick Create defaults (Confirmed); Reports historical balances（推論） |
| Category | Transaction category reference (Confirmed/TBD); UserPreference default reference (Confirmed) | Transactions and planned Budget category summaries (Confirmed contract); category Reports（推論） |
| Budget | Category references and fixed-commitment reserved summary (Confirmed planned contract); Transaction spending（推論） | Home daily-limit calculation (Confirmed contract); Budget page (Confirmed intent) |
| Goal | GoalFundingSummary read model (Confirmed planned contract); funding source TBD | Goals page (Confirmed intent); Home inclusion is an Open Question |
| Report | Transaction/Category/Account historical data（推論） | Reports page/export (Confirmed intent) |
| Recurring Expense | Account and payment occurrence relationships（推論） | Budget reserved summary mapping（推論）; recurring-expense page (Confirmed intent) |
| Settings | UserPreference references to Account and Category (Confirmed contract/intent) | Quick Create resolver (Confirmed); Settings page (Confirmed intent) |

## Domain Readiness（推論）

“Ready” means ready to freeze and implement the domain/API contract, not merely that a screen or read DTO exists.

| Domain | Specification | API | Ready |
|---|---|---|---|
| Transaction | Partial — pagination, search, filters, and create flow need decisions | Existing read/Quick Create; documented contract conflicts remain | Partial — scoped API design only |
| Account | Partial — net assets, subtotals, details, and CRUD unclear | Me Assets read documented; write contract absent from reviewed contract | Partial — read model only |
| Category | Partial — defaults, ownership, and lifecycle unclear | Partial status reported; no endpoint in Backend Contract | No |
| Budget | Partial — calculations and allocation constraints unclear | Planned summary API only | No |
| Goal | Partial — lifecycle and funding semantics unclear | Planned summary API only | No |
| Report | Partial — metric definitions and period rules unclear | No Reports endpoint documented | No |
| Recurring Expense | Partial — recurrence/payment lifecycle unclear | No recurring contract documented; reserved amount appears in planned Budget DTO | No |
| Settings | Partial — persistence boundary and operation scope unclear | Preference work is referenced; full Settings API not documented | No |

## Evidence Count

Counts below use only the top-level bullets in each aggregate's `Confirmed`, `Candidate`, and `Open Questions` subsections. They do not count field rows, relationships, or conflicts.

| Evidence category | Count |
|---|---:|
| Confirmed | 18 |
| Candidate（推論） | 16 |
| Open Questions | 24 |

## Specification Conflicts

The following conflicts or material coverage mismatches are preserved for review; this document does not select an authoritative side.

| Conflict source | Conflict content | Affected Domain | Suggested confirmation |
|---|---|---|---|
| `DashboardV4BackendContract.md` vs `DashboardV4FrontendIntegrationReadinessReview.md` and `DashboardV4ApiContractPlan.md` | Transactions list response in Backend Contract has `totalIncome`, `totalExpense`, and `items`; Readiness Review additionally states `totalCount`, `page`, and `pageSize`. The API Contract Plan records the mismatch. | Transaction | Backend/API owner confirms the source-of-truth request and response DTO, then updates the contract documents together. |
| `DashboardV4BackendContract.md` vs `DashboardV4ProductSpecification.md` and `DashboardV4ApiContractPlan.md` | Backend Contract says `keyword` fuzzy-matches Note; product intent calls for searching note or category. Category matching is not documented in the API contract. | Transaction, Category | Product confirms search semantics; API owner defines matching, filtering, and response behavior. |
| Accounts visual intent / Product Specification vs `DashboardV4BackendContract.md` and API Contract Plan | Intent shows account-type subtotals and labels total assets as net assets; `MeAssetsDto` documents `totalAssets` and account items but does not define type subtotals or liabilities/net-worth semantics. | Account | Product defines the displayed measures; backend owner confirms whether the read model needs explicit type totals and how each is calculated. |
| Budget visual intent / Product Specification vs `DashboardV4BackendContract.md` | Backend Contract defines `remainingBudget = totalBudget - totalSpent` and a separate `fixedCommitmentReserved`; the intent says fixed expenses are reserved/already deducted from monthly income and also shows available budget and daily suggested spend. Their relationship is not defined. | Budget, Recurring Expense | Product and backend jointly approve the available/remaining/reserved formulas and align the Budget DTO descriptions. |
| `DashboardV4FeatureImplementationStatus.md` vs `DashboardV4BackendContract.md` and `DashboardV4ApiContractPlan.md` | Categories is marked API Partial, but the Backend Contract has no Categories endpoint; the API Contract Plan records no documented category DTO/endpoints. This may indicate undocumented implementation rather than an actual behavioral contradiction. | Category | Backend owner inventories existing category contracts and updates the source-of-truth contract/status document. |
| `DashboardV4BackendContract.md` vs `DashboardV4FrontendIntegrationReadinessReview.md` and `DashboardV4ApiContractPlan.md` | Backend Contract is Draft/Pending Architecture Review while Readiness Review is Accepted and marks core APIs Ready; API Contract Plan says Contract Freeze Pending. | All domains | Architecture owner confirms document authority, version/status, and required approval gate before freeze. |

## Contract Freeze Checklist

- [ ] Product Specification Complete
- [ ] Domain Model Complete
- [ ] API Contract Complete
- [ ] Backend Review Complete
- [ ] Frontend Review Complete
- [ ] Contract Freeze

**Current assessment:** Contract Freeze has not been reached. The Product Specification and API plan remain drafts, domain relationships and several aggregates are candidates, and the conflicts above require explicit product/backend/architecture decisions.
