# Categories Read API Freeze Review

## Frozen

### Endpoint

- `GET /api/categories`
- `Authorization: Bearer <JWT>`
- No request body
- No query parameters
- Read-only

### Request

- Pagination: none
- Expense/Income filter: none
- Period filter: none
- Keyword: none
- Status/include-inactive: none

### Response

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "Dining",
    "icon": "🍽",
    "bucketType": 1,
    "createdAt": "2026-11-27T00:00:00"
  }
]
```

Empty result:

```json
[]
```

### Field Definition

| Field | Type | Meaning |
|---|---|---|
| `id` | Guid | Category identity. |
| `name` | string | Category display name. |
| `icon` | string | Existing category icon text/value. |
| `bucketType` | existing enum `1`–`5` | Living, Entertainment, Savings, Investment or Responsibility. |
| `createdAt` | DateTime | Category creation time. |

### Read Behavior

- User-scoped.
- Ordered by category name.
- No soft-delete field or lifecycle exists in the current Category entity.
- `bucketType` is exposed exactly as the existing Domain enum value; it is not converted to Expense/Income semantics.
- V4 frontend receives the dedicated DTO and maps it to a ViewModel.

### Frontend Dependency

- Categories page uses TanStack Query.
- Server state is not stored in the legacy `categoryStore`.
- Loading, empty and error/retry states are implemented.
- Existing prototype filter controls remain UI-only; expense/income category semantics are not claimed by v1.

## Deferred

The following are outside Category Read Model v1:

- Category CRUD
- Category create/update/delete UI
- Category management workflows
- Budget Category domain
- Category allocations
- Budget bucket calculations
- Reporting category rules
- Expense/Income category type
- Category default marker
- Monthly transaction counts
- Display order
- System category lifecycle
- Active/inactive status
- Icon catalog rules
- Category deletion/merge behavior
