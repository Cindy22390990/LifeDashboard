# Dashboard Summary Domain Decision

## Decision 1 - Asset Definition

Question:

What is the Domain Meaning of Total Assets?

### Option A - Account Balance Sum

Source:

- V4 Accounts Read Model
- V4 Me Assets Read Model
- existing legacy Dashboard Summary implementations

Advantages:

- Uses persisted `Account.CurrentBalance`.
- Matches the frozen Accounts Read Model v1 rule.
- Avoids reconstructing asset balances from transaction history.
- Gives one reusable backend calculation source.

Disadvantages:

- Is not Net Worth unless liabilities are absent or separately handled.
- Negative balance and overdraft semantics remain outside this decision.
- Eligibility must consistently exclude deleted and system accounts.

### Option B - Net Worth

Formula:

`Assets - Liabilities`

Advantages:

- Represents a fuller financial position.
- Separates asset value from liabilities.

Disadvantages:

- No liability model or liability account type is confirmed.
- No investment valuation model is confirmed.
- The current Account Read Model v1 explicitly excludes Net Worth and liabilities.

Final Status:

Decision:

**Accepted**

Rule:

For Dashboard Summary v1, `Total Assets` means the backend sum of eligible account balances:

- same user
- `IsDeleted == false`
- `IsSystemAccount == false`
- source value: `Account.CurrentBalance`

This is an asset-balance sum, not Net Worth.

## Decision 5 - Summary Period

### Option A - Today

Dashboard Usage:

- Fits a daily decision view.
- Matches V4 Home `dailySpent` and `dailyRemaining`.

API Contract Impact:

- Could return today's Income and Expense.
- Existing V4 Home already owns daily spending and today's activity.
- A summary endpoint would overlap a dedicated Home contract.

Frontend Complexity:

- Low for a single business-day view.
- Higher if the Dashboard also needs monthly cards.

Report Domain:

- Today's totals are dashboard operational metrics, not a report query responsibility.

### Option B - This Month

Dashboard Usage:

- Supports monthly financial-state presentation.
- Matches existing `MonthlyIncome` and `MonthlyExpense` concepts.
- Fits the Dashboard UI's monthly overview requirement.

API Contract Impact:

- Uses the current calendar month derived from the backend business date.
- Reuses existing monthly Report/Transaction calculation semantics.
- Does not require `startDate` or `endDate` in Summary v1.

Frontend Complexity:

- Low: frontend receives backend-calculated monthly values.
- Frontend must not calculate totals from partial page or account data.

Report Domain:

- Summary v1 uses this month only.
- Custom ranges and report queries remain Report Domain responsibilities.

### Option C - Custom Period

Dashboard Usage:

- Flexible for Dashboard reports or date-range views.

API Contract Impact:

- Requires explicit period parameters such as `startDate` and `endDate`.
- Increases contract ambiguity and timezone requirements.

Frontend Complexity:

- Higher due to period state, validation and range selection.

Report Domain:

- Custom-period calculations belong to Report Domain and should not be absorbed into Dashboard Summary v1.

Final Status:

Decision:

**Accepted**

Rule:

Summary Period = This Month

Reason:

- Dashboard Summary is used for monthly financial-state presentation.
- It does not undertake report query responsibility.
- Income and Expense are calculated for the current calendar month.
- Summary v1 does not add `startDate` or `endDate` query parameters.

## Decision 3 - Transfer Handling

Analyze the role of Transfer in Summary.

### Option A - Excluded

Meaning:

- Transfer does not affect Income.
- Transfer does not affect Expense.
- Transfer changes account balances by moving funds.

Advantages:

- Matches existing Report and V4 Transaction totals.
- Prevents double counting the same cash movement.
- Matches V4 Home daily spending, which excludes Transfer.

Disadvantages:

- Does not expose gross cash movement.
- May not satisfy a future cash-flow report.

### Option B - Cash Movement Only

Meaning:

- Show Transfer as a separate cash-movement dimension.
- Do not merge Transfer into Income or Expense.

Advantages:

- Preserves transfer visibility.
- Separates account movements from P&L-style totals.

Disadvantages:

- No approved Summary field or period exists.
- V4 Transfer display currently belongs to activity/transaction views.

### Option C - Included Separately

Meaning:

- Summary includes a dedicated Transfer field.

Advantages:

- Directly exposes transfer totals.

Disadvantages:

- No existing Summary contract defines transfer totals.
- Period and account-destination semantics remain unresolved.

Final Status:

Decision:

**Accepted**

Rule:

For Dashboard Summary v1 domain interpretation:

- Transfer is excluded from Income.
- Transfer is excluded from Expense.
- No Transfer total is included in Summary v1.
- Transfer does not change Total Assets by itself when moving funds between eligible accounts.
- Transfer-to-Saving or Transfer-to-Investment can remain a separate Home monthly-progress concept.
- Any future Transfer total must be implemented as an explicit, separately defined cash-movement field.

## Decision 4 - Summary Scope

### Included

- Total Assets, as Account Balance Sum
- Monthly Income
- Monthly Expense
- Transfer exclusion rules
- User isolation
- Soft-deleted transaction exclusion

### Excluded

- Net Worth
- Liabilities
- Investment valuation
- recent activity
- Balance field until it has one approved meaning
- Budget totals
- Goal totals
- category breakdown
- fixed commitment summary
- chart data
- Account-type subtotals

## Decision 6 - Balance Field

Analysis:

The repository currently uses several meanings of balance:

- Account Balance: a current persisted account `CurrentBalance`
- Available Balance: may refer to remaining budget or an account-specific spendable amount
- Cash Balance: not defined as a separate domain value
- Net Worth: would require Assets minus Liabilities

These meanings are not consistent across existing Dashboard data sources.

Adding a generic `balance` field would create ambiguity and allow the same name to represent account, period, budget or net-worth concepts.

Final Status:

Decision:

**Accepted**

Rule:

Summary v1 does not provide a `balance` field.

Summary v1 uses explicit fields:

- `totalAssets`
- `monthlyIncome`
- `monthlyExpense`

Reason:

- Avoiding a semantically ambiguous `balance` field prevents future Contract changes.
- `totalAssets`, `monthlyIncome` and `monthlyExpense` each describe a clearer domain meaning.
- The existing Account Balance, Available Balance and Net Worth concepts remain separate decisions and are not conflated.

## Domain Boundary Review

### Recent Activity

Judgment:

Recent Activity is not required for Dashboard Summary v1.

Future split:

- `Dashboard Activity Read API`

Reason:

- It is list/pagination content rather than an aggregate summary field.
- Existing implementations disagree on limit and exact item fields.
- V4 Transactions already owns transaction listing.

### Investment

Judgment:

Investment valuation is excluded from v1.

Investment account classification may be used for account grouping, but valuation or market-price changes are not part of the account-balance asset rule.

### Liability

Judgment:

Liability handling is excluded from v1.

No liability source or business rule is confirmed.

## Freeze Status

Domain Rules:

- **Ready**

Blocking Questions:

- None for the Dashboard Summary v1 domain decisions.
- Existing endpoint-selection and contract-format work can proceed separately from this document.
- Implementation remains prohibited until `DashboardSummaryReadContract v1` is defined and frozen.

## Contract Freeze Readiness

The following requirements are complete:

- Asset Definition: Accepted
- Summary Period: Accepted
- Transfer Handling: Accepted
- Summary Scope: Accepted
- Balance Field: Accepted

Therefore, this document permits entry into `DashboardSummaryReadContract v1`.

Implementation is still prohibited until the Contract is reviewed and frozen.
