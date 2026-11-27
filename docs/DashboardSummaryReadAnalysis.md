# Dashboard Summary Read Analysis

## Scope

This document analyzes existing dashboard summary sources only. It does not approve implementation, add endpoints, or freeze domain rules.

## Backend Existing Sources

| Source | Location | Current Responsibility | Status |
|---|---|---|---|
| Legacy Dashboard Summary API | `LifeDashboard.Api.Controllers.DashboardController.GetSummary` | Requires `currentDate`; combines budget radar, accounts, goals, assets, monthly totals and recent transactions. | Existing; overlaps newer V4 APIs. |
| V3 Summary Read Model | `/api/dashboard/v3/summary` via `GetDashboardSummaryQueryHandler` | Returns V3 summary fields for account and recent-activity read models. | Existing; V3-specific. |
| V4 Home API | `/api/dashboard/v4/home` via `V4HomeController` | Returns business date, today's spending limit/spent/remaining, monthly saved/invested progress and today's transactions. | Existing V4 read contract. |
| V4 Accounts Read Model | `/api/accounts` | Returns eligible account `totalAssets` and account items. | Existing read model v1. |
| V4 Transactions Read Model | `/api/transactions` | Returns filtered all-result income/expense totals and page items. Transfer excluded from both totals. | Existing read model v1. |
| Report Repository | `IReportRepository` / `ReportRepository` | Calculates monthly Income/Expense totals and category breakdown. | Existing calculation logic. |
| Budget Radar | `BudgetRadarQueryRepository` / `DashboardService` | Supplies budget/radar aggregates. | Existing; not interchangeable with asset or income. |
| Dashboard Summary Reader | `Infrastructure.Repositories.DashboardSummaryReader` | Calculates V3 account assets, today's Expense and recent transactions. | Existing; separate from V4. |
| Dashboard Summary Service | `Application.Services.DashboardService` | Combines budget, accounts, goals, assets, monthly totals and recent transactions. | Existing legacy composition. |

## Data Source Mapping

| Summary Field | Source | Current Logic | Status |
|---|---|---|---|
| Total Assets | `Account.CurrentBalance` via legacy, V4 Accounts and V4 Me Assets | Sum non-deleted, non-system accounts. | Can be identified as account-sum candidate; net-worth status unresolved. |
| Income | `ReportRepository.GetMonthlyTotalsAsync` / V4 Transaction reader | Sum `TransactionType.Income` for the selected period; exclude Transfer. | Existing normal period logic; period/field reuse unresolved. |
| Expense | Report/Transaction readers and V4 Home reader | Sum `TransactionType.Expense`; exclude Transfer. | Existing normal period/daily logic; dashboard period unresolved. |
| Balance | Multiple existing meanings | Monthly `income - expense`, account balances, remaining budget or `dailyRemaining`. | Blocked; semantics conflict. |
| Recent Activity | Transaction read/order in legacy, V3 and V4 readers | Usually filter by user/non-deleted and order `TransactionDate DESC, CreatedAtUtc DESC`; limit 5 in summary implementations. | Blocked on whether a summary owns the list and exact fields. |
| Monthly Saved | V4 Home reader | Sum Transfer into destination Saving accounts. | Existing for Home; not a Dashboard Summary asset rule. |
| Monthly Invested | V4 Home reader | Sum Transfer into destination Investment accounts. | Existing for Home; not a Dashboard Summary asset rule. |

## Asset Definition

### Option A — Account Balance Sum

Advantages:

- Uses existing persisted `CurrentBalance`.
- Matches V4 Accounts and V4 Me Assets.
- Avoids rebuilding balances from transaction history.

Disadvantages:

- Is not net worth unless liabilities are absent or separately combined.
- Negative/overdraft account semantics are unresolved.
- System-account and deleted-account eligibility must remain consistent.

Impact:

- A Summary using this rule must reuse the same eligible-account filter as Accounts Read v1.

### Option B — Transaction Derivation

Advantages:

- Could reconstruct balance movement from opening balance and transactions.

Disadvantages:

- Initial balance, transfer, deleted history and account update rules are unresolved as an authoritative asset formula.
- No existing formula is validated for the full account asset set.

Impact:

- Requires additional domain decisions before freeze.

### Option C — Other

No other asset or valuation source is confirmed.

## Net Worth Definition

Deferred:

- Assets
- minus Liabilities
- investment market valuation
- separate liability account representation

V4 Accounts Read Model v1 explicitly excludes liabilities and net worth. A Dashboard Summary must not label account sum as net worth without a product decision.

## Transaction Calculation Rule

### Income

- Existing Report and V4 Transaction logic sums normal `Income`.
- Existing logic excludes `Transfer`.
- Refund/reversal semantics are not established.

### Expense

- Existing Report and V4 Transaction logic sums normal `Expense`.
- V4 Home daily logic sums `Expense` and excludes `Transfer`.
- Fixed commitment payment may be a transaction, but no summary rule is frozen.

### Transfer

- Transfer is not counted as Income or Expense in existing totals.
- V4 Home separately uses incoming transfers to Saving/Investment accounts for monthly progress.
- A Dashboard Summary must not infer Transfer behavior for cash flow until the period and field rules are approved.

## Existing Data Source Conflicts

1. `GET /api/dashboard/summary` already exists and requires `currentDate`.
2. Legacy summary mixes Budget, Accounts, Goals and Transactions.
3. V4 has dedicated Home, Accounts and Transactions read models.
4. V3 and V4 summary field names/shapes differ.
5. Dashboard Summary may duplicate V4 Home and Accounts totals.
6. `docs/DashboardSummaryApiContract.md` documents a V3-only `/api/dashboard/v3/summary` contract with `totalAssets`, `todayExpense`, `businessDate`, `accounts` and `recentTransactions`; it explicitly excludes Budget, Goal, Fixed Commitment and Calendar.