# Accounts Read Model v1

Implementation Status:

- Implemented
- Endpoint: `GET /api/accounts`
- Status: Frozen
- Frontend: Integrated
- Version: v1

## Endpoint

`GET /api/accounts`

- JWT Bearer required.
- User-scoped.
- Read only.
- No request body.
- No pagination.
- No account CRUD fields or mutations.

## Query

V1 不需要 query parameters。

Reasons:

- Accounts page 目前只需完整可用帳戶清單及型別分組。
- 沒有 spec 要求 paginated accounts。
- 不存在已確認的 inactive/status lifecycle，因此不新增 `includeInactive`。
- 不存在已確認的 category 或 display-order contract，因此不新增無基礎的 filter。
- keyword 不是目前已確認需求。

If future product decisions add filters, they require a contract update; v1 does not infer them.

## Response

```json
{
  "totalAssets": 0,
  "items": []
}
```

This is a dedicated read DTO. It does not expose `Account` entity properties.

## Field Definition

### `totalAssets`

- Name: `totalAssets`
- Type: decimal JSON number
- Purpose: 顯示目前可用帳戶的資產總額。
- Source: backend sums `CurrentBalance` from eligible accounts.
- Required: Yes
- Calculation scope:
  - same user
  - `IsDeleted == false`
  - `IsSystemAccount == false`
- v1 explicitly does not define liabilities or net-worth treatment.
- Frontend does not calculate this value from `items`.

### `items`

- Name: `items`
- Type: `AccountReadItemDto[]`
- Purpose: 為 Accounts Page 提供帳戶清單及 account-type 分組來源。
- Source: backend account read query/projection.
- Required: Yes
- Empty: `[]`
- No pagination metadata.

### `items[].id`

- Name: `id`
- Type: Guid
- Purpose: account identity.
- Source: `Account.Id`
- Required: Yes

### `items[].name`

- Name: `name`
- Type: string
- Purpose: 顯示帳戶名稱。
- Source: `Account.Name`
- Required: Yes

### `items[].accountType`

- Name: `accountType`
- Type: canonical string
- Purpose: 將帳戶分為 Cashflow、Saving、Invest。
- Source: `Account.Type`
- Required: Yes
- Values:
  - `Cashflow`
  - `Saving`
  - `Invest`
- Unknown enum values must map to `Unknown` if a forward-compatible projection is needed.
- This dedicated endpoint canonicalizes domain `Investment` to V4 contract value `Invest`; it does not expose `"Investment"` as the canonical read value.

### `items[].balance`

- Name: `balance`
- Type: decimal JSON number
- Purpose: 顯示帳戶目前餘額。
- Source: `Account.CurrentBalance`
- Required: Yes

## Exclusions

V1 excludes:

- `initialBalance`
- `allowNegativeBalance`
- `isSystemAccount`
- inactive/status fields
- display order
- category
- default quick-entry marker
- liabilities
- net-worth calculation
- Account CRUD
- pagination
- `type`, `includeInactive`, `keyword` query parameters

## Open Questions

- Is `totalAssets` the same as net worth, or must liabilities be added later?
- What is the official canonical display name for domain `Investment`: `Invest` in V4 remains the contract decision.
- Are account-type subtotals required in the response, or may frontend group `items` only for display?
- Are inactive or archived accounts required after lifecycle rules are confirmed?
- Is account detail navigation based on account ID or transaction history?
