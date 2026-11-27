# Accounts Read API Analysis

## Scope

本分析僅確認 Accounts Read Model v1 所需的既有來源，不設計 Account CRUD、不修改 Write Flow、不推論未確認的 Domain rule。

## Existing Account Sources

### Domain Entity

- `LifeDashboard.Domain.Entities.Account`
- Account 是資金帳戶 aggregate，包含：
  - `Id`
  - `UserId`
  - `Name`
  - `Type`
  - `InitialBalance`
  - `CurrentBalance`
  - `AllowNegativeBalance`
  - `IsSystemAccount`
  - `IsDeleted`
  - `CreatedAtUtc`
  - `UpdatedAtUtc`
  - `RowVersion`
- Account Type enum：
  - `Cashflow = 1`
  - `Saving = 2`
  - `Investment = 3`
- Domain 目前具備 `MarkAsDeleted()` 與系統帳戶不可刪除規則；本 Read v1 不重用或重新實作 CRUD rule。

### Database Table

- EF Core `DbSet<Account>`
- InitialCreate migration 建立 Account table 與欄位：
  - `Name`
  - `Type`
  - `InitialBalance`
  - `CurrentBalance`
  - `AllowNegativeBalance`
  - `RowVersion`
  - `IsSystemAccount`
  - `IsDeleted`
  - `CreatedAtUtc`
  - `UpdatedAtUtc`
- DbContext 設定 `Name` 最大 50 字元，Balance 精度為 18,2。
- 有 SQL Server `RowVersion`，測試環境忽略此欄位。

### Existing API

- Legacy `GET /api/Account`
  - 回傳 `IEnumerable<AccountDto>`
  - 使用者隔離
  - 排除 `IsDeleted`
  - 依 `CreatedAtUtc DESC` 排序
- Legacy account DTO exposes：
  - `Id`
  - `Name`
  - `Type`
  - `CurrentBalance`
  - `IsSystemAccount`
  - `AllowNegativeBalance`
- Legacy controller 同時包含 POST、PUT、DELETE。本 Read API v1 不修改此 controller，也不為帳戶頁重新暴露 CRUD 欄位或操作。
- V4 `GET /api/me/assets`
  - 專用 read model
  - 回傳 `totalAssets` 與可用帳戶 list
  - 排除 `IsDeleted` 和 `IsSystemAccount`
  - 帳戶依名稱排序
  - list item 使用 `id`、`name`、`accountType`、`balance`
- Existing V4 enum mapping uses `AccountType.ToString()`：
  - domain `Investment` currently becomes `"Investment"`
  - conflicts with Dashboard V4 Backend Contract canonical string `"Invest"`

### Frontend Usage

- V4 Accounts prototype is at `src/presentation/pages/dashboard-v4/accounts/AccountsPage.tsx`
  - currently only placeholder/Coming Soon UI
  - no mock account response
  - no TanStack Query integration
- Legacy V3/AccountPage uses:
  - `accountStore`
  - `AccountRepository.getAll()`
  - `/api/Account`
  - account CRUD and Zustand server state
- V4 `Me` already has `MeAssetsDto` and `meAssetsMapper`
- V4 accounts page must not reuse the legacy `accountStore` or CRUD repository.

## Dashboard Domain Model Requirements

### Existing Confirmed Requirements

From `DashboardV4DomainModel.md`:

- Account read fields:
  - `id`
  - `name`
  - `accountType`
  - `balance`
- Read model field:
  - `totalAssets`
- Account types:
  - `Cashflow`
  - `Saving`
  - `Invest`
- User scoping and soft-delete/system-account exclusion are confirmed for the V4 Me Assets contract.

### Frontend Needed Data

V1 只包含目前 Accounts Page 已確認需要的資料：

| Field | Purpose |
|---|---|
| `id` | Account identity and future detail/navigation key |
| `name` | 顯示帳戶名稱 |
| `accountType` | 分組為 Cashflow / Saving / Invest |
| `balance` | 顯示帳戶餘額 |
| `totalAssets` | 顯示頁面資產總額 |

Not included in v1：

- `initialBalance`
- `allowNegativeBalance`
- `isSystemAccount`
- status/inactive flag
- display order
- category
- default quick-entry marker
- liability/net-worth fields

## Current Unknowns

- Account 是否等於 Wallet 尚未以 product term 明確定義。
- 是否將 Investment 設為資產類別及是否包含所有 investment balances。
- Liability 是否存在以及是否納入 Account read model。
- Net worth 與 total assets 是否相同。
- Account Type `"Invest"` 與 domain enum `"Investment"` 的最終 canonical name at the new dedicated endpoint.
- Account 是否具有 active/inactive/status lifecycle。
- 是否需要 display order。
- account category 是否存在及是否應作為讀取欄位。
- account type subtitle totals 是否需由 Backend 回傳，而非由 frontend 分組。
- whether `allowNegativeBalance` belongs in a read contract or only mutation/configuration.
- whether an account detail endpoint will be added later.
