# Dashboard Summary API Contract

> **File Path**: `docs/DashboardSummaryApiContract.md`

## Document Status

* **Version**: v1.0
* **Feature**: Dashboard V3 Summary
* **Backend Source**:
  * `GetDashboardSummaryQueryHandler`
  * Dashboard Summary DTOs

---

## Purpose

此文件定義 Dashboard V3 Summary API 的資料契約，作為 Frontend Dashboard V3 消費 API Response 與執行 Mock Data → API Data 遷移時的欄位約束依據。

**本 Contract 僅涵蓋**：
* Dashboard Summary
* Account Summary
* Recent Transaction Summary

**本 Contract 不包含**：
* Calendar
* Budget Radar
* Goal
* Fixed Commitment

---

## Response Overview

**Endpoint**: `GET /api/dashboard/v3/summary`

**Response Example**:

```json
{
  "totalAssets": 0,
  "todayExpense": 0,
  "businessDate": "2026-11-24",
  "accounts": [],
  "recentTransactions": []
}
```

### Dashboard Summary Fields

| Field | Type | Nullable | Description |
| :--- | :--- | :---: | :--- |
| `totalAssets` | `decimal` | No | Dashboard 總資產 |
| `todayExpense` | `decimal` | No | `businessDate` 當日支出總額 |
| `businessDate` | `string` (`YYYY-MM-DD`) | No | Backend UTC 業務日期；Frontend 不轉換時區 |
| `accounts` | `DashboardAccountDto[]` | No | 帳戶列表 |
| `recentTransactions` | `RecentTransactionDto[]` | No | 最近交易紀錄 |

---

## Account Summary Contract

**DTO**: `DashboardAccountDto`

### Fields

| Field | Type | Nullable | Status | Description |
| :--- | :--- | :---: | :--- | :--- |
| `id` | `Guid` | No | Existing | Account Id |
| `name` | `string` | No | Existing | 帳戶名稱 |
| `balance` | `decimal` | No | Existing | 帳戶餘額 |
| `type` | `number` | No | Legacy | 原 Enum 數字值；保留相容性 |
| `accountType` | `string` | No | New | Canonical Account Type |

### Account Type

Frontend 應優先消費 `accountType`，而非 `type`。

保留 `type` 之原因：
* Backward compatibility
* Existing client compatibility

#### Canonical Values

| Value | Description |
| :--- | :--- |
| `Cashflow` | 日常現金流 |
| `Saving` | 儲蓄 / 預備金 |
| `Investment` | 長期投資 |

#### Example

```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "name": "薪轉帳戶",
  "balance": 35000,
  "type": 1,
  "accountType": "Cashflow"
}
```

---

## Recent Transaction Contract

**DTO**: `RecentTransactionDto`

### Fields

| Field | Type | Nullable | Status | Description |
| :--- | :--- | :---: | :--- | :--- |
| `id` | `Guid` | No | Existing | Transaction Id |
| `amount` | `decimal` | No | Existing | 金額 |
| `note` | `string` | Yes | Updated | 備註 |
| `categoryName` | `string` | No | Existing | 分類名稱 |
| `accountName` | `string` | No | Existing | 帳戶名稱 |
| `type` | `number` | No | Legacy | 原 Enum 數字值；保留相容性 |
| `transactionType` | `string` | No | New | Canonical Transaction Type |
| `transactionDate` | `string` (`YYYY-MM-DD`) | No | Existing | 交易日期 |

### Transaction Type

Frontend 應優先消費 `transactionType`，而非 `type`。

#### Canonical Values

| Value | Description |
| :--- | :--- |
| `Expense` | 支出 |
| `Income` | 收入 |
| `Transfer` | 轉帳 |

#### Example

```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "amount": 250,
  "note": "午餐",
  "categoryName": "餐飲",
  "accountName": "現金",
  "type": 1,
  "transactionType": "Expense",
  "transactionDate": "2026-11-24"
}
```

---

## Backward Compatibility Policy

### Deprecated Fields

以下欄位僅為向後相容保留，不建議 Frontend 新功能使用：

| Field | Replacement |
| :--- | :--- |
| `type` | `accountType` / `transactionType` |

### Frontend Rule

* New UI component 一律使用 canonical string field（例如：`transaction.transactionType`、`account.accountType`）。
* 禁止於新功能使用 `transaction.type` 或 `account.type`。

---

## Null Handling

* **`note`**：型別為 `string | null`，Frontend 必須明確處理空值（例如：`transaction.note ?? ""`）。

---

## Integration Checklist

移除 Mock Data 前必須完成以下確認：

* [ ] API endpoint available
* [ ] DTO schema confirmed
* [ ] `accountType` mapping verified
* [ ] `transactionType` mapping verified
* [ ] Nullable fields handled
* [ ] Loading state implemented
* [ ] API error state implemented

---

## Future Contract Improvements

以下領域不包含於目前版本，應建立獨立 Contract 以避免與 Dashboard Summary 耦合：

* Budget Radar Contract
* Goal Progress Contract
* Fixed Commitment Contract
* Calendar Contract