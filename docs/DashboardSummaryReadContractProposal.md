# Dashboard Summary Read Contract Proposal

Status:

**Proposal — Not Frozen**

This contract is analysis-only and must not be implemented until domain rules are frozen.

## Endpoint Proposal

`GET /api/dashboard/summary`

Header:

- `Authorization: Bearer <JWT>`

Candidate Query:

| Parameter | Proposed Status | Meaning | Open Question |
|---|---|---|---|
| `currentDate` | Legacy-compatible candidate | Base date for period/current-day calculations. | Should V4 Summary use server business date or caller date? |

No additional query parameters are proposed.

## Response Proposal

```json
{
  "businessDate": "2026-11-27",
  "totalAssets": 0,
  "income": 0,
  "expense": 0,
  "balance": 0,
  "recentActivity": []
}
```

This shape is not an approved API contract.

## Field Definitions

| Field | Meaning | Candidate Calculation Rule | Source | Open Questions |
|---|---|---|---|---|
| `businessDate` | Date basis for summary interpretation | No rule frozen | Server business date or `currentDate` | timezone; override policy |
| `totalAssets` | Eligible account asset sum | Sum non-deleted, non-system `CurrentBalance` | Accounts/Me Assets read logic | net worth; liabilities; eligible types |
| `income` | Period income | Sum normal `Income`; exclude Transfer | Report or Transaction read logic | period; refunds/reversals |
| `expense` | Period expense | Sum normal `Expense`; exclude Transfer | Report, Transaction or Home logic | period; fixed commitments |
| `balance` | Undefined | Candidate: Income-Expense, account sum, or remaining budget | No source uniquely defines it | approved meaning required |
| `recentActivity` | Recent transactions | User-owned, non-deleted, ordered by date/creation time | Transaction read | limit; fields; whether summary owns it |

## Proposal Constraints

- No field may calculate liabilities or net worth.
- No field may infer category/budget reporting rules.
- `balance` should not be exposed until its meaning is approved.
- Frontend must not sum account items or transaction lists until backend contract calculation is frozen.
- Existing V4 Home/Accounts/Transactions pages may already satisfy their own summary requirements and should be considered before introducing a duplicate endpoint.

## Fields Not Proposed

- liabilities
- net worth
- investment valuation
- goal totals
- account-type subtotals
- budget totals
- category breakdown
- chart series
- period enum
