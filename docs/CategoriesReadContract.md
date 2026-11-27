# Category Read Model v1

Implementation Status:

- Implemented
- Endpoint: `GET /api/categories`
- Status: Frozen
- Frontend: Integrated
- Version: v1

## Endpoint

`GET /api/categories`

- JWT Bearer required.
- User-scoped.
- Read only.
- No request body.
- No pagination.
- No category CRUD fields or mutations.
- Existing Category CRUD methods remain outside this read contract.

## Query

No query parameters in v1.

The UI and reviewed documents do not confirm:

- expense/income category filtering
- period-based usage count
- inactive category filtering
- keyword search
- display-order sorting

Therefore the endpoint returns the user's existing category records without inferring those features.

## Response

Fixed read response:

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

The response is a dedicated read DTO rather than direct entity serialization.

## Field Definition

### `id`

- Name: `id`
- Type: Guid
- Purpose: stable category identity used by transaction/category selectors.
- Source: `Category.Id`
- Required: Yes

### `name`

- Name: `name`
- Type: string
- Purpose: category display name.
- Source: `Category.Name`
- Required: Yes

### `icon`

- Name: `icon`
- Type: string
- Purpose: display icon/text.
- Source: `Category.Icon`
- Required: Yes
- The allowed icon catalog remains unconfirmed; no new icon validation or catalog is introduced.

### `bucketType`

- Name: `bucketType`
- Type: existing `BucketType` enum numeric value
- Purpose: expose the category's existing stored bucket assignment.
- Source: `Category.BucketType`
- Required: Yes
- Existing values:
  - `1` Living
  - `2` Entertainment
  - `3` Savings
  - `4` Investment
  - `5` Responsibility

This field is not treated as Expense/Income transaction category type.

### `createdAt`

- Name: `createdAt`
- Type: DateTime ISO string
- Purpose: preserve existing category read metadata.
- Source: `Category.CreatedAt`
- Required: Yes

## Exclusions

V1 does not include:

- expense/income category type
- display order
- default category indicator
- monthly transaction count
- category status/inactive flag
- pagination
- category CRUD
- budget allocation fields
- reporting aggregates

## Open Questions

- Should category read data use a wrapped `{ items: [] }` response in the future?
- Does `bucketType` belong to transaction categories or to a separate budget/bucket domain?
- Is an expense/income category type required at all?
- Should `createdAt` remain public in a future V4 contract or be removed as UI-unneeded metadata?
