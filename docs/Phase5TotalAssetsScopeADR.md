# Phase 5 TotalAssets Scope ADR

Date: 2026-11-16

Status: Accepted

## Context

The Account entity contains `UserId`, `CurrentBalance`, `IsDeleted`, and `IsSystemAccount`. System accounts are associated with a user but are not user-owned asset accounts for the Dashboard `TotalAssets` summary.

The existing Account repository returns accounts for a user excluding soft-deleted accounts, but it does not exclude system accounts. The Dashboard aggregation therefore needs an explicit scope rule.

## Decision

Dashboard `TotalAssets` sums `CurrentBalance` only for user-owned, non-deleted, non-system accounts:

```text
UserId = current user
AND IsDeleted = false
AND IsSystemAccount = false
```

In other words, system accounts are excluded from `TotalAssets`.

## Scope and Constraints

This is a Dashboard read-model aggregation rule only. It does not change:

- The Account Domain Entity or its behavior.
- Account repository behavior or other Account queries.
- Database schema or migrations.
- Account creation, update, deletion, or balance behavior.
- Transaction flows or balance calculations.

The aggregation uses existing `Account.CurrentBalance` values. It does not derive balances from Transactions or add any other account-type filter.

## Consequences

- `TotalAssets` represents the sum of current balances for the current user's accounts where `IsDeleted` is false and `IsSystemAccount` is false.
- Existing system accounts remain available to their current consumers; only their inclusion in this Dashboard read-model aggregate is excluded.
