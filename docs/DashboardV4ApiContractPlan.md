# Phase 4.0 Dashboard V4 API Contract Plan

**Status:** Draft — Contract Freeze Pending  
**Scope:** Transactions, Accounts, Categories  
**Purpose:** Record existing documented API boundaries, identify gaps against the product intent, and propose only candidate contract shapes for review. This document does not approve or implement new endpoints.

## Contract Status Labels

- **Existing / Documented:** Present in `DashboardV4BackendContract.md` or `DashboardV4FrontendIntegrationReadinessReview.md`.
- **Candidate / Proposed:** A possible contract shape inferred from product intent; not approved.
- **TBD / Open Question:** Requires product or architecture decision before contract freeze.

## Global Contract Constraints

- Existing backend contract rules require JWT authentication, current-user data isolation, and exclusion of soft-deleted records.
- Existing contract rules specify money as JSON numbers, business dates as `YYYY-MM-DD`, canonical enum strings, arrays as `[]` rather than `null`, and RFC 7807 ProblemDetails for errors.
- Frontend must display backend-calculated financial aggregates; it must not calculate account totals or transaction summaries from partial page data.
- The Backend Contract document is marked Draft/Pending Architecture Review, while the Readiness Review marks core Home/Transactions/Me APIs Ready. Resolve this document-status discrepancy before treating this plan as frozen.
- All candidate request and response shapes below are proposals only. No endpoint or field should be treated as approved solely because it appears in this plan.

---

## Feature: Transactions

### Endpoint

| Method and path | Status | Purpose / scope |
|---|---|---|
| `GET /api/transactions` | Existing / Phase 4.1 Implemented / Contract Frozen | Query a paginated transaction list and filtered income/expense summary. |
| `POST /api/transactions/quick` | Existing / Documented | Quick Create; resolves account, category, and business date in the backend. Candidate for the diagram's “新增交易” action; product flow is not yet confirmed. |
| `POST /api/transactions` | Existing / Referenced | Full transaction creation is referenced by the Backend Contract, but its request/response shape is not defined there. Use only if product selects a full-entry flow. |
| `GET /api/transactions/{id}`, `DELETE /api/transactions/{id}` | Existing / Referenced | Referenced as CRUD capabilities; details and delete behavior are outside the current visual intent and are not designed here. |

### Request

#### `GET /api/transactions`

| Parameter | Existing documented shape | Status / note |
|---|---|---|
| `year` | Optional integer; paired with `month` when `date` is absent | Documented. |
| `month` | Optional integer, 1–12; paired with `year` when `date` is absent | Documented. |
| `date` | Optional `YYYY-MM-DD`; takes precedence over `year`/`month` | Documented for calendar day selection. |
| `keyword` | Optional string; fuzzy matches `Note` | Supported; category-name matching remains an Open Question. |
| `type` | Optional `Expense` / `Income` / `Transfer` | Legacy Compatible. |
| `transactionType` | Optional `Expense` / `Income` / `Transfer` | Supported canonical parameter. |
| `accountId` | Optional `Guid` | Supported exact account filter. |
| `categoryId` | Optional `Guid` | Supported exact category filter. |
| `startDate` | Optional `YYYY-MM-DD` | Supported inclusive lower date bound. |
| `endDate` | Optional `YYYY-MM-DD` | Supported inclusive upper date bound. |
| `sortBy` | Optional `transactionDate` / `amount` / `note` | Supported allowlist. |
| `sortDirection` | Optional `asc` / `desc`; default `desc` | Supported. |
| `page` | Optional integer, default `1`, minimum `1` | Supported request pagination. |
| `pageSize` | Optional integer, default `20`, range `1`–`100` | Supported request pagination. |

`year` / `month` must be provided together when `date` is absent. `startDate` must not be later than `endDate`. If `type` and `transactionType` are both provided with different values, return `400 ProblemDetails`.

Authentication: `Authorization: Bearer <JWT>`; no request body.

#### `POST /api/transactions/quick`

**Existing documented request fields:** `amount`, `note`, `transactionType`.

| Field | Candidate contract | Status / note |
|---|---|---|
| `amount` | Decimal JSON number | Field is documented; requiredness, range, and precision validation must match the existing API implementation/specification. |
| `note` | String or null | Field is documented; nullability and length limits require confirmation. |
| `transactionType` | `Expense` or `Income` | Quick Create rejects `Transfer`; this is documented. |
| `accountId`, `categoryId`, `transactionDate` | Omitted; backend resolves defaults/business date | Documented behavior. Confirm whether the feature's “新增交易” action is in fact this Quick Create flow. |

#### `POST /api/transactions`

Request shape is referenced as the existing full Transaction CRUD contract and is **not restated or approved by this plan**. The Backend Contract notes `categoryId` is optional and transaction types include Expense, Income, and Transfer. Confirm whether the Transactions page requires this flow in addition to Quick Create.

### Response

#### `GET /api/transactions`

**Existing documented response:** `200 OK`, `TransactionListResponseDto`.

| Field | Existing documented shape | Status / note |
|---|---|---|
| `totalIncome` | Decimal; excludes transfers | Documented as backend-calculated for the filtered result set. |
| `totalExpense` | Decimal; excludes transfers | Documented as backend-calculated for the filtered result set. |
| `items` | Array of transaction items; empty result is `[]` | Documented; one page only. Default order is `TransactionDate DESC, CreatedAtUtc DESC, Id DESC`. |
| Item fields | `id`, `note`, `amount`, `transactionType`, `transactionDate`, `accountId`, `accountName`, `categoryId`, `categoryName` | Implemented response shape; `note`, account, and category fields are nullable/optional according to the item DTO. |
| `totalCount`, `page`, `pageSize` | Removed from the response | Final Phase 4.1 contract; request pagination is used instead. This resolves the historical Readiness Review mismatch. |

#### `POST /api/transactions/quick`

**Existing documented response:** created Transaction DTO; Readiness Review says it contains at least `id`, `amount`, and `transactionType`. Exact DTO, account/category resolution fields, status code, and nullability are **TBD** and require verification against the implemented contract before frontend Query work.

**Documented error case:** No valid Cashflow account returns `422 Unprocessable Entity` with ProblemDetails type `https://api.lifedashboard/errors/no-cashflow-account`. Other validation/error payload details follow the global error contract.

#### `POST /api/transactions`

Response is delegated to the existing full Transaction CRUD contract; the Backend Contract does not define the response schema here. Confirm whether it is needed for the page.

### Entity Dependency

- **Candidate domain entities:** `Transaction`, `Account`, and `Category`.
- **Documented relationships/data:** Transaction items can include account and category IDs/names; the Quick Create resolver supplies default account/category/date.
- **Candidate derived read data:** filtered period income/expense totals and daily net values. The backend must own financial aggregation; exact response fields and formulas need confirmation.
- **Ownership:** current-user scoping and soft-delete exclusion follow the existing global contract.

### Open Questions

1. Is the add action Quick Create, full transaction creation, or a choice of both?
2. Should `keyword` also match category names, or remain note-only as implemented?
3. Does the UI's “全部” transaction filter include `Transfer`?
4. Should Load More advance only on a full page or use an exact end-page signal?
5. What exact response fields and success status does Quick Create return?
6. Does the page need transaction detail, edit, or delete actions? They are not depicted in the intent.
10. Is the Backend Contract's Draft status or the Readiness Review's Ready status authoritative for this API?

---

## Feature: Accounts

### Endpoint

| Method and path | Status | Purpose / scope |
|---|---|---|
| `GET /api/accounts` | Existing / Phase 4.2 Implemented / Contract Frozen | Read Accounts Read Model v1 for total assets and active account items. |
| `GET /api/me/assets` | Existing / Documented | Read total assets and the active account list; current API foundation for the Accounts read view. |
| `POST /api/accounts` | Candidate / Proposed | Possible account creation endpoint for the diagram's “新增帳戶” action. No request/response contract is approved. |
| `PUT` or `PATCH /api/accounts/{id}` | Candidate / Proposed | Possible account edit endpoint. Method, editable fields, and response are TBD. |
| Account detail endpoint | TBD | “查看明細” could mean account profile or transaction history; do not assume a new endpoint until clarified. |

### Request

#### `GET /api/accounts`

- Header: `Authorization: Bearer <JWT>`.
- Query parameters: none.
- Request body: none.
- Response is user-scoped and excludes deleted/system accounts.

#### `GET /api/me/assets`

- Header: `Authorization: Bearer <JWT>`.
- Query parameters: none.
- Request body: none.
- This endpoint is documented as user-scoped and excludes deleted/system accounts.

#### Candidate account creation/editing

| Operation | Candidate request fields | Status / note |
|---|---|---|
| Create account | `name`, `accountType`; opening balance and any additional fields TBD | Candidate only. The visual intent does not define validation or opening-balance behavior. |
| Edit account | Fields TBD; likely a partial update if supported | Candidate only. Confirm immutable fields and balance-edit policy. |

### Response

#### `GET /api/accounts`

**Final Phase 4.2 response:** `200 OK`, `AccountsReadDto`.

| Field | Type | Status / note |
|---|---|---|
| `totalAssets` | Decimal JSON number | Final; backend sum of eligible account `CurrentBalance` values. |
| `items` | Array; empty result is `[]` | Final. |
| `items[].id` | Guid | Final. |
| `items[].name` | String | Final. |
| `items[].accountType` | `Cashflow` / `Saving` / `Invest` / `Unknown` | Final canonical V4 read values. Domain `Investment` maps to `Invest`. |
| `items[].balance` | Decimal JSON number | Final. |

No pagination, type filter, include-inactive flag or keyword parameter is part of v1.

#### `GET /api/me/assets`

**Existing documented response:** `200 OK`, `MeAssetsDto`.

| Field | Type | Status / note |
|---|---|---|
| `totalAssets` | Decimal JSON number | Documented as backend-calculated sum of active, non-system account balances. |
| `accounts` | Array; empty result is `[]` | Documented. |
| `accounts[].id` | Guid | Documented. |
| `accounts[].name` | String | Documented. |
| `accounts[].accountType` | `Cashflow` / `Saving` / `Invest` | Documented canonical values. |
| `accounts[].balance` | Decimal JSON number | Documented. |

**Gap against the intent:** The diagram also shows totals by account type and a “總資產 (淨資產)” label. The current response has one `totalAssets` value and account items, but does not document type-level totals or distinguish assets from net worth. Do not sum the returned account list in the frontend; decide whether the backend adds explicit aggregate fields or the product removes/changes those totals.

#### Candidate account create/edit response

- No response DTO or status code is defined by the Backend Contract for these operations.
- Candidate response could return the created/updated account and its current balance, but field set, balance calculation, and whether a read-after-write is required are **TBD**.

### Entity Dependency

- **Documented domain dependency:** Account and user-scoped Me Assets read model.
- **Documented Account fields:** `id`, `name`, `accountType`, `balance`.
- **Candidate fields from the intent:** default quick-entry account indicator and any account visibility preference. The default is associated with UserPreference in the Settings intent; it is not documented as an Account property.
- **Candidate relationships:** Account to its transactions; user to owned accounts. Detail-screen relationship is TBD.

### Open Questions

1. Is the page displaying total assets, net worth, or both? Define liabilities treatment if net worth is intended.
2. Should account-type subtotals be added to `MeAssetsDto`, or removed from the page?
3. Are account creation and editing supported by this phase? If yes, approve endpoint paths, request fields, validation, and response shapes.
4. Is opening balance editable, and how does changing it affect transaction history/balance?
5. What does “查看明細” mean: account information or transactions filtered by account?
6. How is the default quick-entry Cashflow account represented and updated?
7. Is the eye control local UI state or a persisted user preference?
8. Confirm whether `/api/me/assets` and `/api/accounts` should converge in a later phase; both are currently separate read contracts.

---

## Feature: Categories

### Endpoint

No Categories endpoint is defined in `DashboardV4BackendContract.md`.

| Method and path | Status | Purpose / scope |
|---|---|---|
| `GET /api/categories` | Existing / Phase 4.3 Implemented / Contract Frozen | Read the user's category list with the Category Read Model v1 fields. |
| `POST /api/categories` | Candidate / Proposed | Create a category. Request/response contract requires approval. |
| `PUT` or `PATCH /api/categories/{id}` | Candidate / Proposed | Edit a category. Method and editable fields require approval. |
| Set-default operation | TBD | The intent has a “設為預設” action; whether this updates UserPreference or a category resource is unresolved. |

### Request

#### `GET /api/categories`

- Header: `Authorization: Bearer <JWT>`.
- Query parameters: none.
- Request body: none.
- User-scoped category list.
- No Expense/Income filter is included.
- No period or transaction-count parameter is included.

#### Candidate `POST /api/categories`

Candidate body:

| Field | Candidate type | Status / note |
|---|---|---|
| `name` | String | Implied by category cards; length/uniqueness rules TBD. |
| `type` | `Expense` / `Income` | Implied by the UI tabs; allowed values TBD against backend enums. |
| `icon` | Optional string or catalog identifier | Icon is shown in the visual intent; storage format and allowed values are TBD. |

#### Candidate update and default selection

- Update body fields and partial/full replacement semantics are **TBD**.
- Default selection may be a UserPreference update rather than a Categories mutation. Its request shape and endpoint must be designed with Settings; do not assume a `isDefault` field on `Category`.

### Response

`GET /api/categories` returns `200 OK` with `CategoryReadDto[]`.

| Field | Final meaning | Status / note |
|---|---|---|
| `id` | Category identifier | Final. |
| `name` | Display name | Final. |
| `icon` | Display icon text | Final. |
| `bucketType` | Existing `1`–`5` enum value | Final; not relabeled as Expense/Income. |
| `createdAt` | Category creation time | Final for v1. |

Empty result is `[]`. Category CRUD response contracts remain outside this read freeze.

### Entity Dependency

- **Candidate domain entities:** `Category`, `Transaction`, and `UserPreference`.
- **Candidate relationships:** Transactions reference categories; UserPreference may reference a default category. Confirm whether a default is global, per transaction type, or otherwise scoped.
- **Candidate read data:** Current-month transaction count for each category. Confirm whether this is computed by the API and which transaction types/statuses are counted.
- **Ownership and lifecycle:** Define user ownership, system/default categories, soft deletion, and behavior for categories already referenced by transactions.

### Open Questions

1. Is `BucketType` a transaction category concept or a separate budget/bucket concept?
2. Are categories user-specific, system-provided, or both?
3. Is the default category unique per user, per transaction type, or per account?
4. Is default selection stored on Category or UserPreference?
5. What period defines “本月”, and how are transaction counts calculated?
6. What icon format/catalog is supported?
7. Can a category be deleted, archived, renamed, or merged when transactions reference it?
8. Are names unique, and if so, within which scope and category type?
9. What ordering, pagination, and empty-state behavior are required?
10. Which existing partial category APIs, if any, should be extended rather than replaced?

---

## Contract Freeze Gate

Do not begin Frontend Query implementation for these features until:

1. Each existing-versus-proposed endpoint is reviewed and its status explicitly accepted.
2. Transactions read pagination and search/filter implementation contract is resolved; remaining UI semantics are explicitly tracked as Open Questions.
3. Accounts aggregate fields and CRUD scope are decided.
4. Categories read endpoint ownership and Category Read Model v1 response shape are approved; Category CRUD/default/management semantics remain deferred.
5. Request/response nullability, error cases, and success statuses are recorded in the source-of-truth Backend Contract.
6. The Backend Contract document's Draft/Pending Architecture Review status is reconciled with the Readiness Review's Ready assertions.

**Current outcome:** This plan is a design input, not a frozen contract. No code, API, or Frontend Query work is included.
