# Dashboard V3 Backend Sprint 1 Specification

## Status

Sprint 1 已完成後端 Dashboard Summary Read Model 實作。此文件只描述目前已實作與凍結的 Sprint 1 範圍。

## Source of Truth

Sprint 1 的唯一架構規格依據為 `docs/DashboardV3BackendIntegrationPlan.md` V2.2。

---

# Sprint 1 Scope

本 Sprint 只實作 Dashboard Summary Read Model：

* Dashboard Summary DTO 與 Projection
* Total Assets
* Today Expense
* Recent Transactions

金額使用未格式化的純數值 `decimal`。日期使用 `DateOnly`。

## 明確排除

以下功能不屬於 Sprint 1：

* Monthly Calendar
* Day Detail
* Budget Summary
* Goal Summary
* Pending Summary
* Budget Radar 實作
* Goal Progress 實作
* Client 指定日期參數
* 時區轉換
* Multi-Currency
* API Integration Tests
* Frontend 修改

不新增任何 Sprint 1 未實作欄位的 placeholder、預設假資料或 Future Contract 欄位。

---

# Architecture Components

## Application

* `Features/Dashboard/Dtos/DashboardSummaryDto.cs`
* `Features/Dashboard/Dtos/RecentTransactionDto.cs`
* `Features/Dashboard/Models/DashboardSummaryProjection.cs`
* `Features/Dashboard/Models/RecentTransactionProjection.cs`
* `Features/Dashboard/Interfaces/IDashboardSummaryReader.cs`
* `Features/Dashboard/Queries/GetDashboardSummaryQuery.cs`
* `Features/Dashboard/Queries/GetDashboardSummaryQueryHandler.cs`

## Infrastructure

* `Repositories/DashboardSummaryReader.cs`

## Web API

* `Controllers/DashboardController.cs`

## Tests

* `LifeDashboard.Tests/Dashboard/GetDashboardSummaryQueryHandlerTests.cs`

---

# Projection Flow

```text
DashboardController
↓
GetDashboardSummaryQuery
↓
GetDashboardSummaryQueryHandler
↓
IDashboardSummaryReader
↓
DashboardSummaryReader
↓
DashboardSummaryProjection
↓
DashboardSummaryDto
```

責任邊界：

* `DashboardSummaryReader` 負責 EF Core query、projection 與 aggregate。
* `DashboardSummaryReader` 只回傳 `DashboardSummaryProjection`，不得回傳 DTO。
* `GetDashboardSummaryQueryHandler` 負責將 Projection mapping 成 DTO。
* `DashboardController` 負責 routing、authorization、JWT Claims、呼叫 Handler 與回傳 DTO，不執行業務計算。

Recent Transaction mapping 會保留 `TransactionType Type`：

* `DashboardSummaryReader` 將 `Transaction.Type` 投影到 `RecentTransactionProjection.Type`
* `GetDashboardSummaryQueryHandler` 將 `Projection.Type` mapping 到 `RecentTransactionDto.Type`

Projection Model 必須只使用純 C# 型別，不包含 Domain Entity、EF Core type 或 `IQueryable`。

---

# DTO Contract

## DashboardSummaryDto

只包含：

| 欄位 | 型別 |
| --- | --- |
| `TotalAssets` | `decimal` |
| `TodayExpense` | `decimal` |
| `RecentTransactions` | `IReadOnlyList<RecentTransactionDto>` |

## RecentTransactionDto

只包含：

| 欄位 | 型別 |
| --- | --- |
| `Id` | `Guid` |
| `Note` | `string` |
| `CategoryName` | `string` |
| `AccountName` | `string` |
| `Amount` | `decimal` |
| `Type` | `TransactionType` |
| `TransactionDate` | `DateOnly` |

禁止加入：

* `Title`
* `UserId`
* `IsDeleted`
* `CreatedAtUtc`
* 其他 internal or future fields

---

# Projection Models

## DashboardSummaryProjection

包含：

* `TotalAssets`
* `TodayExpense`
* `IReadOnlyList<RecentTransactionProjection> RecentTransactions`

## RecentTransactionProjection

包含：

* `Id`
* `Note`
* `CategoryName`
* `AccountName`
* `Amount`
* `Type`
* `TransactionDate`

Projection 不得包含 EF Core Entity 或 infrastructure type。

---

# Query Interface

`IDashboardSummaryReader` 提供唯一的 Dashboard Summary Projection 查詢能力：

```csharp
Task<DashboardSummaryProjection> GetSummaryAsync(
    Guid userId,
    CancellationToken cancellationToken);
```

限制：

* 不回傳 DTO
* 不接受 client date parameter
* 不引用 Infrastructure
* 不引用 EF Core
* 不包含 `IQueryable`

---

# Query Logic

## Total Assets

來源：`Account`

條件：

* `account.UserId == userId`
* `!account.IsDeleted`
* `!account.IsSystemAccount`

計算：

```text
SUM(CurrentBalance)
```

不得假設或使用不存在的 `IsActive` 欄位。

## Today Expense

來源：`Transaction`

條件：

* `transaction.UserId == userId`
* `!transaction.IsDeleted`
* `transaction.TransactionType == Expense`
* `transaction.TransactionDate == today`

不得使用金額正負判斷收入或支出。

## Recent Transactions

最多 5 筆。

排序：

1. `TransactionDate DESC`
2. `CreatedAtUtc DESC`

`CreatedAtUtc` 僅用於同一業務日期的穩定排序，不可用於交易日期篩選，也不可回傳給 DTO。

Projection 欄位僅包含：

* `Id`
* `Note`
* `CategoryName`
* `AccountName`
* `Amount`
* `Type`
* `TransactionDate`

---

# Infrastructure Implementation

`DashboardSummaryReader` 必須：

* 注入 `LifeDashboardDbContext`
* 實作 `IDashboardSummaryReader`
* 使用 `AsNoTracking()`
* 執行 EF Core Projection 與 aggregate
* 套用使用者隔離與軟刪除條件
* 回傳 `DashboardSummaryProjection`

不得直接回傳 `DashboardSummaryDto` 或 `RecentTransactionDto`。

---

# Business Date / Timezone

目前 Sprint 1 使用：

```text
DateOnly.FromDateTime(DateTime.UtcNow)
```

取得 Today Expense 的業務日期。

Sprint 1 不處理：

* User timezone
* Locale timezone
* UTC offset
* `DateTimeOffset`

未來可考慮確認使用者時區與正式 Business Date source，但不屬於本次實作。

---

# API Contract

所有 Dashboard endpoint 都需要 JWT Bearer Authorization。`UserId` 必須由 JWT Claims 取得。

## Existing API

保留既有 endpoint：

```text
GET /api/dashboard/summary
```

用途：

維持既有 Dashboard Summary Contract。

此 endpoint 保留既有 `currentDate` query parameter 行為及既有 response contract，不可刪除或破壞。

## New V3 API

新增 endpoint：

```text
GET /api/dashboard/v3/summary
```

規則：

* `JWT Bearer` authorization
* `UserId` 從 JWT Claims 取得
* 不接受 `date` query parameter
* 不接受 `currentDate` query parameter
* 回傳 V3 `DashboardSummaryDto`

HTTP 成功狀態：

* `200 OK`

未經授權時沿用現有 API authorization 行為。

---

# Dependency Injection

需註冊：

* `IDashboardSummaryReader` → `DashboardSummaryReader`
* `GetDashboardSummaryQueryHandler`

三者均依目前專案慣例使用 `Scoped` lifetime。

---

# Testing

Sprint 1 只新增 `GetDashboardSummaryQueryHandler` Unit Tests。

測試案例：

* Empty State：Reader 回傳空 projection，Handler 回傳零值與空交易集合。
* Projection Mapping：正確映射 `TotalAssets` 與 `TodayExpense`。
* Recent Transaction Mapping：正確映射六個 DTO 欄位。

不得新增：

* Controller Test
* API Integration Test
* EF Core Database Test
* Domain / Migration Test

---

# Out of Scope

本文件不新增未實作功能描述，並明確排除：

* Calendar
* Day Detail
* Budget Summary
* Goal Summary
* Pending Summary
