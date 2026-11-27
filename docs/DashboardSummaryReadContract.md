# Dashboard Summary Read Model v1

Status:

**Frozen Contract Candidate**

Version:

**v1**

---

## Endpoint

`GET /api/dashboard/summary`

---

## Query Parameters

None.

This Summary v1 has no query parameters because:

- Summary Period is fixed to the current month.
- It does not support custom report periods.
- `startDate` and `endDate` are excluded.
- The caller does not select a period.

Header:

- `Authorization: Bearer <JWT>`

---

## Response Contract

```json
{
  "totalAssets": 0,
  "monthlyIncome": 0,
  "monthlyExpense": 0
}
```

The response is a fixed three-field read DTO shape. It does not expose the entity graph or legacy Dashboard composite DTO.

---

## Field Definition

### `totalAssets`

Meaning:

- Total assets based on Account Balance Sum.

Source:

- Account balance data.

Rule:

- Use eligible `Account.CurrentBalance` values.
- Eligible accounts are owned by the current user.
- Exclude deleted accounts.
- Exclude system accounts.
- Sum balances in the backend.
- This does not equal Net Worth.

Empty Behavior:

- Return `0` if no eligible account exists.

### `monthlyIncome`

Meaning:

- Income amount within the current month.

Period:

- This Month.

Rule:

- Include normal `Income` transactions in the current calendar month.
- Exclude `Transfer`.
- Calculate in the backend.
- Frontend must only display the value.

Empty Behavior:

- Return `0` if no eligible Income transaction exists.

### `monthlyExpense`

Meaning:

- Expense amount within the current month.

Period:

- This Month.

Rule:

- Include normal `Expense` transactions in the current calendar month.
- Exclude `Transfer`.
- Calculate in the backend.
- Frontend must only display the value.

Empty Behavior:

- Return `0` if no eligible Expense transaction exists.

---

## Non Goals

Dashboard Summary v1 does not include:

- `balance`
- `netWorth`
- liability
- investment valuation
- transfer summary
- recent activity
- budget
- goals
- category breakdown
- fixed commitments
- account list
- chart data
- custom period fields

---

## Calculation Rules

### Period

- All monthly values use the current month.
- The backend determines the current business month.
- No client `startDate` or `endDate` is accepted.

### Transfer

- Transfer does not affect `monthlyIncome`.
- Transfer does not affect `monthlyExpense`.
- Transfer is not a fourth summary field in v1.

### Assets

- Assets are calculated from the Account Balance Sum.
- Transfer between eligible accounts is a balance movement, not an independent asset addition or removal.
- Net Worth and liability calculations are out of scope.

---

## Error / Empty Behavior

No account or transaction data:

- `totalAssets = 0`
- `monthlyIncome = 0`
- `monthlyExpense = 0`

No special empty-object or null-array shape is required.

Authentication and transport errors follow the existing global API error contract. This document does not introduce Summary-specific error semantics.

---

## Contract Notes

- This contract is a candidate frozen by Domain Decision only.
- It is separate from `/api/dashboard/v3/summary`.
- It is separate from `/api/dashboard/v4/home`.
- It does not freeze legacy endpoint compatibility.
- It does not approve implementation.
