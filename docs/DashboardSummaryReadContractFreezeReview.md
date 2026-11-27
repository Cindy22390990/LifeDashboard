# Dashboard Summary Read Contract Freeze Review

## Frozen

- Endpoint: `GET /api/dashboard/summary`
- Query: none
- Response:

```json
{
  "totalAssets": 0,
  "monthlyIncome": 0,
  "monthlyExpense": 0
}
```

- Field Meaning:
  - `totalAssets`: eligible Account Balance Sum
  - `monthlyIncome`: current-month Income
  - `monthlyExpense`: current-month Expense
- Calculation Rules:
  - current month
  - no custom period
  - transfer excluded from Income and Expense
  - assets exclude deleted and system accounts
  - empty values are zero
- Total Assets does not mean Net Worth.
- `balance` is not a field.

## Deferred

- Net Worth
- Liability
- Investment valuation
- Custom period
- `startDate` / `endDate`
- Reports and report-specific period semantics
- Recent Activity / Activity API
- Budget
- Goals
- Category breakdown
- Fixed commitments
- Transfer summary/cash movement

## Implementation Permission

Contract Status:

**Ready for Implementation**

Implementation Status:

- Backend: **Completed**
- Tests: **Passed**

The frozen endpoint has been implemented without adding fields or changing Domain scope.
