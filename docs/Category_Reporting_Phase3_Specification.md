# Category & Reporting 模組 Phase 3 重構規格書

檔案名稱：

Category_Reporting_Phase3_Specification.md


# 目標

本階段目的：

1. 將交易分類系統資料庫化。
2. 支援使用者自訂 Category。
3. 建立 Category 與 Transaction 的關聯。
4. 建立後端 Report Query Engine。
5. 提供月度收支分析資料。
6. 為未來圖表與財務分析功能建立基礎。


---

# Phase 3 開發限制

開始前必須遵守：

1. 不修改 JWT Token 架構。
2. 不修改 Login/Register 流程。
3. 不修改 UserId Isolation 設計。
4. 不修改 Account 商業邏輯。
5. 不破壞既有 Transaction 資料。
6. 不一次完成完整報表 UI。
7. 不在前端進行報表計算。
8. 不直接修改既有 Migration。
9. 每個 Phase 完成後必須驗證 Build 與資料隔離。
10. 所有 Report 查詢必須由後端計算。


開始修改前必須確認：

- Git working tree 狀態
- Migration 清單
- Database Schema
- Transaction Entity
- Account Entity
- User Entity
- DbContext
- Existing DTO


---

# 開發階段

Phase 3 分為四個階段：


---

# Phase 3-1 Category Domain 與 Database

## 目的

建立分類基礎能力。


## 完成

- Category Entity
- BucketType Enum
- Category Repository
- Category Service
- Category API


## Database

建立：

Categories


欄位：

|欄位|型態|
|-|-|
|Id|Guid|
|UserId|Guid|
|Name|string|
|Icon|string|
|BucketType|Enum|
|CreatedAt|DateTime|


限制：

- Category 屬於單一 User。
- 不允許跨 User 存取。
- Category 刪除不可影響 Transaction。


---

# Phase 3-2 Transaction Category Relationship


## 目的

建立 Category 與 Transaction 的正式關聯。


## 開發限制

本階段：

- 不刪除 Transaction.Category 舊欄位。
- 不修改 Transaction 核心流程。
- 不改變既有 API Contract。
- 不修改 UserId Isolation。


---

# Transaction Entity 調整


新增：

```csharp
public Guid? CategoryId { get; set; }

public Category? CategoryEntity { get; set; }

# Phase 3-3 Reporting Query Engine

## 目的

建立後端報表查詢能力。

本階段：

- 提供月度收入分析
- 提供月度支出分析
- 提供分類彙總
- 提供未來 Chart / Dashboard 使用資料來源

不包含：

- 前端圖表
- Dashboard UI
- 預測分析
- AI 財務建議


---

# 開發限制

本階段：

1. 不修改 JWT Token 架構。
2. 不修改 Login/Register。
3. 不修改 UserId Isolation。
4. 不修改 Account 商業邏輯。
5. 不修改 Transaction 建立流程。
6. 不新增 Migration。
7. 不在 Frontend 計算報表。
8. 不直接讀取其他 User 資料。


---

# Report API Design


## Endpoint

GET:
/api/reports/monthly?year={year}&month={month}


## Authorization

Required:


[Authorize]


UserId：

由 JWT Claim 取得。


禁止：

Request 傳入 UserId。


---

# Monthly Report Response


DTO:


MonthlyReportDto


欄位：


|欄位|型態|說明|
|-|-|-|
|Year|int|年份|
|Month|int|月份|
|TotalIncome|decimal|收入總額|
|TotalExpense|decimal|支出總額|
|Balance|decimal|結餘|
|Categories|List|分類統計|


Balance 計算：


TotalIncome - TotalExpense


---

# Category Breakdown


DTO:


CategoryBreakdownDto


欄位：


|欄位|型態|說明|
|-|-|-|
|CategoryId|Guid?|分類Id|
|CategoryName|string|分類名稱|
|BucketType|BucketType?|資金分類|
|Income|decimal|收入|
|Expense|decimal|支出|
|TransactionCount|int|交易數量|


---

# Query Rules


## User Isolation


所有查詢必須：


Transaction.UserId == currentUserId


禁止：

只使用 CategoryId 查詢。


---

# Date Range


月份查詢：

開始：


year/month/01 00:00:00


結束：


next month 01 00:00:00


使用：


= startDate
< endDate



避免月底日期問題。


---

# Transaction Type Rules


## Income

計算：


TransactionType.Income


## Expense

計算：


TransactionType.Expense


## Transfer

排除：


TransactionType.Transfer


原因：

帳戶轉移不是收入或支出。


---

# Category Grouping Rules


## 有 CategoryId


使用：


Category.Name
Category.BucketType


但是必須確認：


Category.UserId == Transaction.UserId


避免跨使用者資料。


---

## CategoryId == null


保留舊資料。


使用：


Transaction.Category


規則：

有文字：


使用舊分類名稱


無文字：


未分類


---

# Repository Responsibility


新增：


IReportRepository
ReportRepository


負責：

- EF Core Query
- Filter
- Group By
- Sum
- Projection


禁止：

- Controller 計算
- Service 載入全部 Transaction


---

# Service Responsibility


新增：


IReportService
ReportService


負責：

- 驗證 Year / Month
- 呼叫 Repository
- 組合 DTO


---

# Controller Responsibility


新增：


ReportsController


負責：

- Authorization
- 取得 UserId
- 接收 year/month


禁止：

- 報表計算
- SQL Query


---

# DI


Program.cs 新增：


IReportRepository
ReportRepository

IReportService
ReportService


---

# Phase 3-3 完成條件


完成：

- Monthly Report API
- Repository Query
- Service Layer
- DTO
- User Isolation
- Build 驗證


不包含：

- Frontend
- Chart
- Migration
- AI Analysis