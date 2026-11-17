# Phase 5 Dashboard and Budget Radar API Contract

Version: 1.3  
Status: Approved Read-Model Contract  
Source of truth:

- `Phase5ScopeFreezeADR.md`
- `Phase5ImplementationDecision.md`
- `Phase5ApiContractDecisionADR.md`
- `Phase5TotalAssetsScopeADR.md`

This document defines the externally visible read-model contract. It does not prescribe service, repository, or query implementation.

# 1. Dashboard API

## Endpoint

`GET /api/dashboard/summary`

The existing request contract remains unchanged. This endpoint is read-only and returns a dashboard read model.

## Phase 5 response additions

| Field Name | Type | Meaning | Source | Calculation Responsibility |
|---|---|---|---|---|
| `TotalAssets` | Number (decimal) | Total current balance of the current user's active, user-owned accounts; system accounts are excluded. This value is not transaction-derived. | Account | Backend sums `CurrentBalance` where `UserId` is the current user, `IsDeleted = false`, and `IsSystemAccount = false`. It must not recalculate account balances from transactions. |
| `MonthlyIncome` | Number (decimal) | Income total for the calendar month containing the requested `currentDate`. | Transaction | Backend filters to that calendar month and `TransactionType = Income`, excludes soft-deleted transactions, and returns the aggregate. |
| `MonthlyExpense` | Number (decimal) | Expense total for the calendar month containing the requested `currentDate`. | Transaction | Backend filters to that calendar month and `TransactionType = Expense`, excludes soft-deleted transactions, and returns the aggregate. |
| `RecentTransactions` | Array of existing transaction read-model items | The user's most recent transactions, with no more than 5 items. | Transaction | Backend excludes soft-deleted transactions, orders by existing `TransactionDate` descending, and limits the result to 5. Existing same-date ordering is retained; no additional tie-break rule or pagination contract is introduced. |

## Existing Goals response

The existing `Goals` response collection remains unchanged and continues to provide existing Goal information. Phase 5 does not add a separate `GoalsProgress` collection or recalculate Goal progress or status.

## Existing Budget response

The Dashboard response's `Budget` field continues to use the existing `RadarStateDto` read model. Additive fields approved for `RadarStateDto` may therefore also appear under `Dashboard.Budget`; in Phase 5, `RemainingLivingBudget` is exposed as `dashboard.budget.remainingLivingBudget`. This is shared read-model exposure, not a new Dashboard calculation or Budget business rule.

All other existing Dashboard response fields retain their names, types, meanings, and behavior. The fields listed above are additive.

# 2. Budget Radar API

## Endpoint

`GET /api/budget/radar`

The existing request contract remains unchanged. Budget Radar describes the current Payday payroll period containing the requested date; it does not use a calendar-month period.

## Phase 5 response addition

| Field Name | Type | Meaning | Source | Calculation Responsibility |
|---|---|---|---|---|
| `RemainingLivingBudget` | Number (decimal) | Existing remaining living budget for the current Payday payroll period. | Existing `BudgetSnapshot` | 由既有 BudgetSnapshot 計算流程產生，Phase 5 僅負責 expose 至 API response。 |

No new `TotalBudget` or `TotalSpent` response fields are authorized in Phase 5. Existing Budget Radar response fields remain unchanged.

`UsagePercentage` and `Status` are future/not implemented fields, not current response requirements. Phase 5 must not derive a percentage, introduce status thresholds, or infer a status.

# 3. Field Ownership

## Backend

The backend owns:

- Read-model aggregation and filtering.
- User scoping and calendar-month or Payday-period selection, as applicable.
- Exclusion of soft-deleted transactions from user-visible transaction results and transaction aggregates.
- Exposure of calculations that already exist in supported domain/read-model data.

The backend must not introduce new financial rules, recalculate Account balances, or recalculate Goal progress for this contract.

## Frontend

The frontend owns:

- Formatting and rendering values supplied by the API.
- Display formatting for dates, currency, and visualizations.

The frontend must not aggregate transactions or calculate Budget Radar values, including `RemainingLivingBudget` or any future percentage/status values.

# 4. Compatibility and Implementation Boundaries

## Read-only behavior

Both endpoints are read-only. Requests must not mutate persisted data or cause transaction, account-balance, or goal side effects.

## Soft-delete exclusion

Soft-deleted transactions must not appear in `RecentTransactions` or contribute to Dashboard monthly totals or transaction-derived Budget Radar aggregates.

## Schema freeze

Phase 5 must not add or modify Entities, database schema, or migrations. It must not change Transaction write flows, Account balance rules, or FixedCommitment domain behavior.

## Additive response changes

Phase 5 may add only the response fields listed in this contract. This permission is additive only:

- Do not remove, rename, retype, or change the meaning of existing response fields.
- Do not change existing request contracts or add API endpoints.
- Do not change unrelated API contracts.

## Existing tests

Existing tests remain frozen, except for the narrow DTO shape assertion updates explicitly permitted by `Phase5TestCompatibilityDecisionADR.md`. Business-behavior assertions and all unrelated tests must remain unchanged. If an approved response addition conflicts with a frozen business-behavior test, stop implementation and resolve the conflict through an ADR; do not weaken that test.

## Dependency on Phase 3B

Phase 5 Dashboard and Budget Radar must rely on Phase 3B Soft Delete behavior.

No Phase 5 code may:

- Bypass Transaction QueryFilter.
- Recalculate account balance from transactions.
- Modify Transaction delete flow.
- Modify soft-delete behavior or business-behavior tests.

End of Contract
