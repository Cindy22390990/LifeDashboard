# Dashboard V3 Feature Completion Audit

**Audit date:** 2026-11-24  
**Scope:** Read-only code and route inspection. No application code was changed.

## 1. Current V3 Completion Status

### Route analysis

| Route | Component | Data source | Status |
|---|---|---|---|
| `/dashboard` | `DashboardRoute` → `DashboardPage` | `useDashboardSummaryStore` → `DashboardSummaryService` → `GET /api/dashboard/summary?currentDate=...`; categories are fetched separately | Active legacy route and the post-login default. Uses the legacy API contract. Some legacy widgets still display illustrative data. |
| `/dashboard-v3` | `DashboardV3Page` | `AuthApp` injects `mockDashboardDataSource` → `MockDashboardDataSource` → `dashboardMapper` → `mockDashboardData` | Protected preview route, linked as “V3 Preview”. The current route does not use the V3 API. |

`ApiDashboardDataSource` and `DashboardV3SummaryService` exist, but `AuthApp` does not inject them into `/dashboard-v3`. Consequently, viewing the route currently makes no V3 Summary API request.

### Widget audit

| Widget | UI completion | Current route data source | Real API support in code | Current Mock use | API-mode state | Next step |
|---|---|---|---|---|---|---|
| `HeroStats` | UI present | Mock | `dashboardV3ApiMapper` maps `totalAssets` and `todayExpense`; this mapper is not active on the route | All displayed values are Mock on the current route. In API mode, `remainingBudget` is `null` and shown as `—`. | Core values ready; remaining budget unavailable | Switch the route to the API DataSource only after validating the API contract and remaining fields. |
| `AccountSummaryWidget` | UI present, list and empty state implemented | Mock | API mapper maps `accounts` to ViewModel items | Current route uses Mock accounts | Ready or empty | Resolve the account enum mapping: current backend enum JSON is numeric, while the API mapper currently maps non-string account types to `Unknown`. |
| `RecentTransactionsWidget` | UI present, list and empty state implemented | Mock | API mapper maps `recentTransactions` | Current route uses Mock transactions | Ready or empty | Connect the API DataSource; retain the backend transaction ordering and verify type mapping. |
| `BudgetRadarWidget` | UI present | Mock | No V3 Budget data; API mapper returns `pending` | Current route displays Mock budget values and Mock status/percentage | Pending | Keep as Pending in API mode until a supported Budget contract exists. |
| `GoalProgressWidget` | UI present | Mock | No V3 Goal Funding data; API mapper returns `pending` | Current route displays Mock goals | Pending | Keep as Pending until a Goal Funding summary contract exists. |
| `CalendarSection` | Interactive calendar UI present | Mock transactions transformed into calendar summaries/details | No V3 Calendar data; API mapper returns `pending` | Current route calendar and day details derive from Mock transactions | Pending | Keep as Pending until Calendar and day-detail APIs exist. |

The page also contains `ReportsPlaceholder`; it is explicitly a preview and not a connected dashboard feature.

**Completion assessment:** V3 has a substantial UI and an API mapping path, but the currently routed page is still Mock-backed. UI presence should not be counted as real-data completion.

## 2. Mock Dependency List

### Production-route dependencies

| File / component | Current Mock dependency |
|---|---|
| [`AuthApp.tsx`](../LifeDashboard.Web/src/AuthApp.tsx) | Injects `mockDashboardDataSource` into the protected `/dashboard-v3` route. |
| [`MockDashboardDataSource.ts`](../LifeDashboard.Web/src/infrastructure/dashboard/mock/MockDashboardDataSource.ts) | Calls `mapDashboardData(mockDashboardData)`. |
| [`mockDashboardData.ts`](../LifeDashboard.Web/src/infrastructure/dashboard/mock/mockDashboardData.ts) | Holds V3 account, transaction, calendar-source, budget, and goal fixture data. |
| [`dashboardMapper.ts`](../LifeDashboard.Web/src/infrastructure/dashboard/mappers/dashboardMapper.ts) | Converts the V3 fixture into the current ViewModel, including calendar summaries and day details. |
| [`GoalProgressWidget.tsx`](../LifeDashboard.Web/src/presentation/components/dashboard-v3/GoalProgressWidget.tsx) | Displays Mock goals when the current Mock DataSource is used. |
| [`BudgetRadarWidget.tsx`](../LifeDashboard.Web/src/presentation/components/dashboard-v3/BudgetRadarWidget.tsx) | Displays Mock budget data/status when the current Mock DataSource is used. |

### API-path fallback

[`dashboardV3ApiMapper.ts`](../LifeDashboard.Web/src/infrastructure/dashboard/mappers/dashboardV3ApiMapper.ts) still imports `mockDashboardData` and creates a Mock ViewModel before overriding some API-backed fields. It is not currently selected by `AuthApp`, but remains a Mock dependency in the API mapping path. API-mode Budget, Goal, and Calendar states are explicitly set to Pending.

### Legacy dashboard previews

| File / component | Current Mock dependency |
|---|---|
| [`GoalSummary.tsx`](../LifeDashboard.Web/src/presentation/components/dashboard/GoalSummary.tsx) | Reads `mockGoalFundingSummary` from `dashboardPreviewData.ts`; the UI itself labels this preview data. |
| [`BudgetRadarSummary.tsx`](../LifeDashboard.Web/src/presentation/components/dashboard/BudgetRadarSummary.tsx) | Uses an inline illustrative radar status and progress percentage. The displayed remaining amount comes from the legacy summary response. |

### Development-only and removal candidates

- **Development-only:** No separate development-only configuration or test-only isolation was found for `mockDashboardDataSource`; it is injected by the production application route.
- **Future removal candidates:** After API route cutover and feature parity, consider removing the V3 Mock DataSource/fixture/mapper path and the API mapper's Mock fallback. Retire the legacy Goal and Budget illustrative data only after their replacement or product deferral is explicit.
- **Not immediate deletion candidates:** The current V3 Mock fixture is still the active source for `/dashboard-v3`; removing it now would break the current preview route.

## 3. Pending Features

In the API DataSource path, the following V3 ViewModel sections are Pending:

- Budget Radar
- Goal Progress
- Calendar and day details
- Remaining budget

In the currently routed Mock preview, these same sections render fixture data instead. Thus their visible UI is not evidence that the feature is backed by a V3 API.

## 4. Backend Coverage

The V3 endpoint is `GET /api/dashboard/v3/summary`. The controller requires authorization and invokes the V3 query handler.

| Dashboard data | V3 endpoint support | Notes |
|---|---|---|
| `totalAssets` | Supported | Computed from eligible accounts. |
| `todayExpense` | Supported | Computed for the V3 `businessDate`. |
| `businessDate` | Supported | Returned by the summary response. |
| `accounts` | Supported | Account ID, name, type, and balance are returned. The current backend enum is serialized numerically; the frontend API mapper currently maps numeric account types to `Unknown`. |
| Transactions | Supported, limited | `recentTransactions` returns at most five records, including type and transaction date. This is not a full transaction-history endpoint. |
| `budgetRadar` / `remainingBudget` | Not supported by V3 Summary | The legacy summary endpoint supplies a Budget Radar read model, but V3 does not. |
| `goalProgress` / Goal Funding summary | Not supported by V3 Summary | The legacy response includes goals, but the legacy `GoalSummary` component currently renders a separate preview fixture rather than that response data. |
| `calendar` / day details | Not supported by V3 Summary | The current V3 calendar is derived from Mock transactions. |
| Monthly income / monthly expense | Not supported by V3 Summary | The legacy summary response provides these values; the V3 Summary contract does not. |

## 5. Legacy Dashboard Retirement Assessment

The legacy `/dashboard` route remains active, is the post-login destination, and is linked in the primary navigation. It uses the legacy dashboard service and summary endpoint.

| Legacy feature | V3 replacement status | Safe to delete? |
|---|---|---|
| Financial overview: total assets, monthly income/expense, available budget | Partial. V3 has total assets and today expense in its API mapping path; remaining budget is Pending, and monthly figures are not in the V3 Summary contract. The active V3 route is still Mock-backed. | No |
| Budget Radar | UI exists, but active V3 is Mock-backed and API mode is Pending. Legacy remaining budget is response-backed, while its radar visualization is illustrative. | No |
| Goal Summary | V3 has a Mock-backed widget; API mode is Pending. Legacy Goal Summary also uses a preview fixture rather than the legacy response goals. | No |
| Recent Activity | V3 UI and API mapping exist, but the active V3 route is Mock-backed. Legacy Recent Activity uses the legacy summary response and separately loaded categories. | No |
| Account Details | V3 UI and API mapping exist, but the active V3 route is Mock-backed. Legacy accounts come from the legacy summary response. | No |
| Quick Add Entry | No equivalent prominent quick-add action was identified in the V3 page; the legacy button navigates to transaction entry. | No |

**Recommendation:** Retain `/dashboard` and its components for now. V3 is not yet a safe replacement: its route is still Mock-backed, its API coverage is partial, and several visible V3 sections are Pending in API mode. Reconsider legacy removal only after V3 is switched to the API DataSource, API-backed feature parity or explicit Pending decisions are accepted, and the replacement flows are verified.
