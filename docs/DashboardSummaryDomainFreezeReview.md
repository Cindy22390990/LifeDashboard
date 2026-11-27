# Dashboard Summary Domain Freeze Review

## Review Status

**No — Domain Freeze Not Achieved**

The repository has implementations and read patterns, but no single approved Dashboard Summary contract or calculation owner.

## Confirmed Rules

- Summary reads must be user-scoped.
- Soft-deleted transactions are excluded from existing V3/V4 transaction summary and home calculations.
- Existing account-sum implementations exclude deleted and system accounts.
- Existing monthly/report and transaction read totals treat `Income` and `Expense` separately.
- Existing monthly/report logic excludes `Transfer` from Income and Expense totals.
- V4 Home daily expense sums only `Expense` and excludes `Transfer`.
- V4 Accounts `totalAssets` is backend-calculated and frontend must not sum account items.
- Recent transaction candidates are ordered by `TransactionDate DESC, CreatedAtUtc DESC`.

These rules describe existing behavior, not an approved new Dashboard Summary.

## Deferred Rules

### Liability Handling

- No liability model or account field is confirmed.
- Deferred.

### Net Worth

- Not the same as account asset sum.
- Deferred until Assets minus Liabilities is approved.

### Investment Valuation

- Account balance is not market valuation evidence.
- Deferred.

### Transfer Classification

- Existing Income/Expense totals exclude Transfer.
- V4 Home separately recognizes transfers into Saving/Investment accounts for monthly progress.
- No Dashboard Summary transfer rule is frozen.

### Summary Period

- Today, month, custom range and year are all possible.
- No period is consistently selected across every summary field.

### Balance

- No approved meaning exists.
- Candidate meanings conflict:
  - Income minus Expense
  - sum of account balances
  - remaining budget

## Blockers

1. No approved meaning of Dashboard `balance`.
2. No approved meaning of `totalAssets` versus net worth.
3. No approved liability model.
4. No approved summary period.
5. No approved Income/Expense/Transfer rule for the proposed summary.
6. Existing `/api/dashboard/summary` and V4 APIs overlap.
7. V4 Home already owns daily spending and monthly progress.
8. V4 Accounts already owns account asset totals.
9. V4 Transactions already owns period Income/Expense totals.
10. Recent activity inclusion, limit and fields are unconfirmed.
11. The existing V3 Summary contract is separately scoped and is not evidence for a V4 Dashboard Summary freeze.

## Frontend Requirement Analysis

| UI Component | Required Data | Source | Freeze State |
|---|---|---|---|
| Hero Card | daily remaining, spent, limit | V4 Home API | Confirmed within Home contract |
| Income Summary | period Income | V4 Transactions/report semantics | Period blocker |
| Expense Summary | period Expense | V4 Transactions/report semantics | Period blocker |
| Asset Display | eligible account assets | V4 Accounts Read Model v1 | Confirmed as assets, not net worth |
| Recent Activity | recent transaction items | Transaction APIs/legacy summary | Blocked |
| Monthly Progress | saved/invested | V4 Home API | Confirmed within Home contract |

## Implementation Decision

**Do not implement Dashboard Summary Read API yet.**

Implementation is allowed only after approving:

- whether to reuse V4 Home/Accounts/Transactions or introduce a summary endpoint
- summary period
- `balance` meaning
- `totalAssets` versus net worth
- Income/Expense/Transfer rules
- recent activity inclusion, limit and fields
- relationship to legacy `/api/dashboard/summary` and `/api/dashboard/v3/summary`
