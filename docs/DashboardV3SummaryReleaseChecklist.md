# Dashboard V3 Summary Release Checklist

> Scope: Dashboard V3 Summary Production Integration  
> Source of Truth:
> - `docs/DashboardV3BackendProductionReadinessReview.md`
> - `docs/DashboardSummaryApiContract.md`
> - `docs/DashboardV3BackendIntegrationImplementationSpec.md`
> - `docs/NullableContractAudit.md`

## 1. Decision

- [ ] Production Readiness Review conclusion is `READY`
- [ ] No P0 / Blocking Gap is present
- [ ] Backend and frontend changes are limited to the Summary Scope
- [ ] Backend and frontend deployments use compatible builds

## 2. API Contract

### Endpoint

- [ ] `GET /api/dashboard/v3/summary`
- [ ] JWT Bearer authentication remains required
- [ ] No client date query parameter is required

### Summary Fields

- [ ] `totalAssets`
- [ ] `todayExpense`
- [ ] `businessDate`
- [ ] `accounts`
- [ ] `recentTransactions`

### Account Fields

- [ ] `accountType` is consumed for UI mapping
- [ ] Legacy `type` is retained but not used by new UI behavior
- [ ] `Cashflow` maps to the frontend Cash type
- [ ] `Saving` maps to the frontend Bank type
- [ ] `Invest` / `Investment` maps to the frontend Investment type
- [ ] Unknown values map to `Unknown`

### Recent Transaction Fields

- [ ] `transactionType` is consumed for UI mapping
- [ ] Legacy `type` is retained but not used by new UI behavior
- [ ] `Expense` maps to the Expense transaction type
- [ ] `Income` maps to the Income transaction type
- [ ] `Transfer` maps to the Transfer transaction type
- [ ] Unknown values map to `Unknown`
- [ ] `transactionDate` is required and uses `YYYY-MM-DD`
- [ ] `note` supports `string | null`
- [ ] `null`, `""`, and whitespace-only notes fall back to a transaction-type translation

### Date Contract

- [ ] `businessDate` uses `YYYY-MM-DD`
- [ ] `transactionDate` uses `YYYY-MM-DD`
- [ ] Frontend does not use `new Date("YYYY-MM-DD")`
- [ ] Frontend date parsing remains local date-safe

## 3. Backend Pre-flight Smoke Checks

### New User Empty Data

- [ ] Call `GET /api/dashboard/v3/summary` with a new account
- [ ] Response is `200 OK`
- [ ] `totalAssets` is `0`
- [ ] `todayExpense` is `0`
- [ ] `accounts` is `[]`
- [ ] `recentTransactions` is `[]`

### Nullable Note

- [ ] A transaction with a database `Note` of `NULL` is serialized as `"note": null`
- [ ] The API does not return `500`
- [ ] Frontend renders a transaction-type translation fallback

### Authentication and Errors

- [ ] Missing token returns `401 Unauthorized`
- [ ] Expired token returns `401 Unauthorized`
- [ ] Frontend retains the existing authentication flow
- [ ] Unexpected server errors use the existing error response strategy
- [ ] No stack trace or internal secret is exposed

### Runtime Configuration

- [ ] CORS allowlist matches the frontend production origin
- [ ] JWT Issuer matches the production environment
- [ ] JWT Audience matches the production environment
- [ ] The deployed API contains the latest Dashboard V3 Summary contract

### Backend Database Compatibility

- [ ] Production database migration status is verified
- [ ] Dashboard Summary query works against the production schema
- [ ] Required Account fields exist
- [ ] Required Transaction fields exist
- [ ] Nullable columns match the Dashboard Summary API contract

### API Response Shape Verification

- [ ] A production API response sample is captured
- [ ] The production response matches `DashboardSummaryApiContract`
- [ ] Collection fields do not unexpectedly return `null`
- [ ] Enum values match canonical `accountType` and `transactionType` values
- [ ] `note` returns `null` or a string as required
- [ ] `businessDate` and `transactionDate` use `YYYY-MM-DD`

## 4. Frontend Production Path

- [ ] `AuthApp` injects `apiDashboardDataSource`
- [ ] `ApiDashboardDataSource` calls `DashboardV3SummaryService`
- [ ] `DashboardV3SummaryService` calls `/api/dashboard/v3/summary`
- [ ] The existing Axios instance retains JWT authorization
- [ ] The summary request supports `AbortSignal`
- [ ] No production import or fallback references `mockDashboardData`
- [ ] No production use references `MockDashboardDataSource`
- [ ] No production use references `/api/dashboard/summary`
- [ ] Legacy DTO fields are not used for Summary UI mapping

### Frontend Runtime Configuration

- [ ] Production API base URL is verified
- [ ] Production environment variables are correct
- [ ] The production build does not contain a development API endpoint

## 5. Frontend Runtime Behavior

### Loading and Retry

- [ ] Loading state displays a skeleton
- [ ] `retry()` exists
- [ ] Unmount aborts the active Summary request
- [ ] `AbortError` / canceled requests do not enter error state
- [ ] Retry reissues the Summary API request

### Empty State

- [ ] `accounts: []` renders the account empty state
- [ ] `recentTransactions: []` renders the transaction empty state
- [ ] No exception or undefined access occurs
- [ ] The dashboard remains renderable with zero totals

### Error State

- [ ] HTTP `500` displays the Dashboard Error UI
- [ ] Network failure displays the Dashboard Error UI
- [ ] Timeout displays the Dashboard Error UI
- [ ] The Error UI provides a Retry action
- [ ] No fallback to Mock data occurs
- [ ] HTTP `401` remains governed by the existing Auth flow

### Privacy

- [ ] Hero `totalAssets` uses the Privacy Formatter
- [ ] Hero `todayExpense` uses the Privacy Formatter
- [ ] Account balances use the Privacy Formatter

## 6. Pending Features

- [ ] Budget Radar remains `Pending`
- [ ] Goal Progress remains `Pending`
- [ ] Fixed Commitment remains `Pending` / excluded
- [ ] `remainingBudget` remains `null`
- [ ] No synthetic Budget, Goal, Calendar, or Fixed Commitment data is introduced

## 7. Validation

### Build

- [ ] `npm run build` succeeds
- [ ] TypeScript compilation succeeds
- [ ] Vite production build succeeds

### Testing

- [ ] `npm test` script status is documented
- [ ] Web unit-test infrastructure status is documented
- [ ] Web E2E infrastructure status is documented
- [ ] Testing infrastructure is not inferred as passing when tests cannot run

## 8. Release Stop Conditions

### P0 Release Blocker

Stop integration or rollback if any of the following occurs:

- [ ] `GET /api/dashboard/v3/summary` is unavailable
- [ ] The API or DTO contract is incompatible with the frontend
- [ ] Authentication configuration is incompatible or authentication fails
- [ ] Runtime exceptions or crashes occur in the Summary integration
- [ ] The production bundle or production path contains an active Mock fallback
- [ ] The production path calls the legacy `/api/dashboard/summary` endpoint
- [ ] Empty summary arrays return `null`
- [ ] Canonical enum fields are missing or incompatible
- [ ] `note: null` causes a frontend exception or backend `500`
- [ ] `transactionDate` or `businessDate` is not `YYYY-MM-DD`

### P1 Release Risk

Document and assess each of the following before release:

- [ ] Validation evidence is incomplete
- [ ] Test infrastructure status is not documented
- [ ] Operational configuration evidence is missing
