# Accounts Read API Freeze Review

## Frozen

### Endpoint

- `GET /api/accounts`
- `Authorization: Bearer <JWT>`
- No request body
- No query parameters
- Read-only

### Request

- Query: none
- Pagination: none
- Type filter: none
- Include inactive: none
- Keyword: none

### Response

```json
{
  "totalAssets": 0,
  "items": []
}
```

### Field Definition

| Field | Type | Meaning |
|---|---|---|
| `totalAssets` | decimal | Eligible account balances summed by the backend. |
| `items` | array | Eligible account read items. |
| `items[].id` | Guid | Account identity. |
| `items[].name` | string | Account display name. |
| `items[].accountType` | `Cashflow` / `Saving` / `Invest` / `Unknown` | Canonical V4 account type. Domain `Investment` maps to `Invest`. |
| `items[].balance` | decimal | Account current balance. |

### Eligibility

The read model includes accounts that satisfy:

- `UserId == currentUserId`
- `!IsDeleted`
- `!IsSystemAccount`

### Totals

- `totalAssets` is backend-calculated.
- Frontend must not sum `items[]` to reconstruct `totalAssets`.
- `totalAssets` is not a net-worth claim.

### Frontend Dependency

- Accounts page uses TanStack Query.
- Server state is not stored in Zustand or `accountStore`.
- Component receives an Accounts ViewModel from the mapper layer.
- Loading, empty and error/retry states are implemented.

## Deferred

The following are outside Accounts Read Model v1:

- Account CRUD
- Create account API
- Update account API
- Delete account API
- Transfer rules
- Liability handling
- Net worth calculation
- Account status/inactive lifecycle
- Pagination
- Type filter
- Keyword search
- Include-inactive query
- Display order
- Account category
- Default quick-entry account marker
- Account detail endpoint
- Edit/delete UI behavior
