# Dashboard V3 API-backed UI Verification

## Summary

`/dashboard-v3` is currently wired to `apiDashboardDataSource`. The API mapper does not import or substitute mock data. Nullable metrics are handled without fabricating values:

- `remainingBudget: null` is rendered as `—`.
- `monthlyIncome` and `monthlyExpense` are not currently rendered by the V3 page.

The shared `/dashboard-v3` page currently renders account and recent-transaction data. The live snapshot shows account type `Cash` (localized as `現金`) and Income, Expense, and Transfer transactions.

Two presentation gaps were found:

1. Bank, Investment, and Unknown account types have no matching account-type translation keys in the checked English and Traditional Chinese locale files, so the widget falls back to the raw values `bank`, `investment`, and `unknown`.
2. Unknown transactions map to `Unknown` internally, but the recent-transactions widget displays the transaction's category name rather than an explicit Unknown type label.

## Nullable metrics

| Field | API mapper value | V3 UI behavior | Result |
|---|---|---|---|
| `monthlyIncome` | `null` | Not referenced by `DashboardV3Page` or `HeroStats` | Not rendered; no zero/mock substitute |
| `monthlyExpense` | `null` | Not referenced by `DashboardV3Page` or `HeroStats` | Not rendered; no zero/mock substitute |
| `remainingBudget` | `null` | `HeroStats` renders `—` when the amount is null | Correctly indicates unavailable data |

`monthlyIncome`, `monthlyExpense`, and `remainingBudget` are nullable in the V3 [DashboardViewModel](../LifeDashboard.Web/src/application/dashboard/models/DashboardViewModel.ts). The API mapper explicitly sets all three to `null` because the summary API does not provide these metrics.

## Account type display

The mapper converts Backend enum values to the V3 AccountType values. `AccountSummaryWidget` looks up `accounts.types.<type>` and uses the raw type string when a translation is missing.

| Backend AccountType | V3 AccountType | Display verification |
|---|---|---|
| Cashflow | Cash | Traditional Chinese locale displays `現金`; confirmed in the shared page |
| Saving | Bank | No `accounts.types.bank` translation in the checked locales; falls back to `bank` |
| Investment | Investment | No `accounts.types.investment` translation in the checked locales; falls back to `investment` |
| Unknown / unrecognized value | Unknown | No `accounts.types.unknown` translation in the checked locales; falls back to `unknown` |

The mapping itself handles numeric and string enum values. Only Cash was present in the current live account data; Bank, Investment, and Unknown were verified from the mapper and widget code, not from live records.

## Transaction type display

The API mapper handles numeric and string enum values. The widget applies the transaction type CSS class and icon; for known types, it displays the corresponding translated type through `categoryKey`.

| Transaction type | Mapper/UI behavior |
|---|---|
| Income | Income type label, upward icon, positive amount sign |
| Expense | Expense type label, downward icon, negative amount sign |
| Transfer | Transfer type label, transfer icon, no forced amount sign |
| Unknown / unrecognized value | Maps to `Unknown`, uses a generic bullet icon and no forced amount sign; displays the API `categoryName`, not an explicit Unknown label |

The live page currently displays Income, Expense, and Transfer rows. Unknown transaction display was verified from the code path only; no Unknown transaction was present in the live data.

## API-only and mock dependency audit

The active route wiring is:

`AuthApp` → `apiDashboardDataSource` → `DashboardV3SummaryService` → `mapDashboardV3Api` → V3 ViewModel

- `AuthApp` injects `apiDashboardDataSource` for `/dashboard-v3`.
- `ApiDashboardDataSource` gets the summary from the API service and maps that response.
- `dashboardV3ApiMapper` has no mock imports or fake-data fallback.
- `MockDashboardDataSource`, its fixture, and the separate mock mapper still exist in the repository, but the V3 route does not reference them.
- `summary.accounts ?? []` and `summary.recentTransactions ?? []` are defensive empty-list handling, not fake data. Missing lists produce empty states.
- Transaction title fallback uses fields from the API transaction (`note`, `categoryName`, `accountName`); it does not supply mock content.

**Conclusion:** no active Mock dependency or fake-data fallback was found in the `/dashboard-v3` API data path. Mock support files remain available but are disconnected from this route.

## Files checked

- [AuthApp](../LifeDashboard.Web/src/AuthApp.tsx)
- [DashboardV3Page](../LifeDashboard.Web/src/presentation/pages/DashboardV3Page.tsx)
- [HeroStats](../LifeDashboard.Web/src/presentation/components/dashboard-v3/HeroStats.tsx)
- [AccountSummaryWidget](../LifeDashboard.Web/src/presentation/components/dashboard-v3/AccountSummaryWidget.tsx)
- [RecentTransactionsWidget](../LifeDashboard.Web/src/presentation/components/dashboard-v3/RecentTransactionsWidget.tsx)
- [TransactionItem](../LifeDashboard.Web/src/presentation/components/dashboard-v3/TransactionItem.tsx)
- [ApiDashboardDataSource](../LifeDashboard.Web/src/infrastructure/dashboard/api/ApiDashboardDataSource.ts)
- [dashboardV3ApiMapper](../LifeDashboard.Web/src/infrastructure/dashboard/mappers/dashboardV3ApiMapper.ts)
- [Traditional Chinese locale](../LifeDashboard.Web/src/i18n/locales/zh-TW.json)
- [English locale](../LifeDashboard.Web/src/i18n/locales/en.json)

No program files were modified. No build or test was run for this read-only verification.
