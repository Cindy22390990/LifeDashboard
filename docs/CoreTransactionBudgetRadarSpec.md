Core Transaction & Budget Radar 重構規格書

版本：v1.4

狀態：
Phase 1 Transaction Atomicity ✅ Completed
Phase 2 FixedCommitment ✅ Backend CRUD Completed
Phase 3 Budget Radar ⏳ Backend Implementation In Progress
Phase 4 Dashboard UI Pending

Phase 3 Budget Radar Backend Implementation

Completed:

Added BudgetRadarService

Added RadarStateDto

Added BudgetRadar API

Added Transaction period query support

Added FixedCommitment payroll period query support

Added BucketType.Responsibility

Added PayrollPeriodMonth rule

Not Completed:

Database Migration for FixedCommitment

Integration Test

Frontend Dashboard UI

Production validation

Database Migration Status

Current:

FixedCommitment Entity exists

DbContext mapping exists

Migration not created

Migration Rule

Before creating migration:
Verify:

Entity definition

DbContext mapping

API contract

Restriction:
Do not create migration automatically.
Database integration test requires explicit approval.
Do not modify database schema only to fix calculation issues.

1. 模組目的

核心負責：

收入管理

支出管理

帳戶轉帳

固定責任

每日生活預算計算

系統目標：
將預算計算集中於 Backend。
Frontend 不負責任何預算邏輯。

2. Transaction 模組

Transaction Entity

欄位：

Id
UserId
AccountId
TargetAccountId
CategoryId
Amount
Type
TransactionDate
Note



Type:

Expense = 1
Income = 2
Transfer = 3



3. Transaction 原子性

所有交易必須符合：ACID

新增交易流程：

Begin Transaction
新增 Transaction
更新 Account Balance
SaveChanges
Commit



任何錯誤：Rollback

4. Transfer 規則

轉帳：
來源：AccountId
目標：TargetAccountId

流程：

檢查來源餘額
扣除來源
增加目標
建立 Transaction
Commit



不足：拋出 InsufficientFundsException

5. Fixed Commitment

用途：
管理固定支出。例如：房租、保險、分期、孝親費。

Entity:

FixedCommitment
Id
UserId
Name
MonthlyAmount
CommitmentType
StartMonth
EndMonth



有效期間包含開始與結束月份。EndMonth 為 nullable，null 代表永久有效。有值時不可早於 StartMonth。

6. Budget Radar 計算定義

BudgetRadar 負責計算目前薪資週期內使用者可使用生活預算。
所有預算計算必須由 Backend 完成。

Frontend 不允許自行計算：

薪薪週期

固定責任

剩餘預算

每日安全額度

Input:

UserId

CurrentDate

CurrentDate 由 API Query Parameter 提供。
原因：

支援測試不同日期

支援歷史預算查詢

避免依賴 Server Current Date

TimeZone:
目前統一使用 Asia/Taipei。

7. 預算週期與日期規則 (Period & Date Rules)

BudgetRadar 不使用自然月；週期由 SystemSetting.Payday 決定。薪資週期的起訖日期皆包含在交易查詢期間內。

Payday Boundary Rule

薪資週期起始日包含 Payday。

例如：
Payday = 5
CurrentDate = 2026-11-05
Budget Period: 2026-11-05 ~ 2026-12-04

CurrentDate = 2026-11-04
Budget Period: 2026-10-05 ~ 2026-11-04

Payday Invalid Date Rule

當 Payday 大於該月份最大日期時：
使用該月份最後一天。

Example:
Payday = 31
February: 2026-02-28
March: 2026-03-31

PayrollPeriodMonth Calculation

Definition:
PayrollPeriodMonth = BudgetPeriodStartDate.Year / Month

Example:
Payday = 5
CurrentDate = 2026-11-02
Budget Period: 2026-10-05 ~ 2026-11-04
BudgetPeriodStartDate: 2026-10-05
PayrollPeriodMonth: 2026-10

FixedCommitment 判斷:
StartMonth = 2026-10-01 => Included
StartMonth = 2026-11-01 => Not Included

8. 預算公式 (Budget Formula)

TotalLivingBudget

公式：
收入總額 - 固定責任總額

固定責任：
依 PayrollPeriodMonth 判斷有效期間內的 FixedCommitment MonthlyAmount 總和。
條件為：StartMonth <= PayrollPeriodMonth AND (EndMonth >= PayrollPeriodMonth OR EndMonth IS NULL)

Income Calculation Rule

收入只計算目前薪資週期內的收入。

查詢條件：
Transaction.Type = Income
AND TransactionDate >= BudgetPeriodStartDate
AND TransactionDate <= BudgetPeriodEndDate

不計算其他薪資週期收入。

RemainingLivingBudget

公式：
TotalLivingBudget - 本薪資週期已使用 Living Expense

Living Expense 定義：
Transaction.Type = Expense
AND Category.BucketType = Living

排除：

Transfer

Income

Responsibility

Entertainment

Savings

Investment

CategoryId = null

Negative Budget Calculation

If:
RemainingLivingBudget < 0

System behavior:

Keep actual negative value

DailySafeLimit may become negative

TodayRemaining may become negative

Frontend displays overspending state

Backend does not clamp value to zero.

9. 每日預算計算 (Daily Budget Calculation)

RemainingDays:
包含今天。
例如：CurrentDate = 2026-11-02，Budget Period: 2026-10-05 ~ 2026-11-04
RemainingDays: 包含 2026-11-02
計算：BudgetPeriodEndDate - CurrentDate + 1

DailySafeLimit:
公式：RemainingLivingBudget / RemainingDays

TodaySpent:
公式：CurrentDate 當日 Living Expense 總額

TodayRemaining:
公式：DailySafeLimit - TodaySpent

Daily Remaining Rule

TodayRemaining 可以為負值。
負值代表今日支出超過每日安全額度。

Money Display Rule (Decimal 進位規則)

All currency output: Decimal(18,2)
Round: 2 decimal places

例如：10000 / 31 應在 DTO 輸出時回傳 322.58。

10. 未分類支出規則 (Uncategorized Expense Rule)

UncategorizedExpenseAmount:
計算目前薪資週期內：
Transaction.Type = Expense
AND CategoryId = null

包含所有未分類支出。
此金額：

不扣除 RemainingLivingBudget

不影響 DailySafeLimit

提供前端提醒使用者補分類

11. FixedCommitment 與 Transaction 關係

FixedCommitment 不自動建立 Transaction。
FixedCommitment 不與 Transaction 建立直接關聯。

原因：

使用者可能提前付款

使用者可能延期付款

不同責任付款日期不同
系統只管理責任，不模擬銀行扣款。

流程：
FixedCommitment -> BudgetRadar 計算時扣除 -> User 實際付款後自行建立一般 Expense Transaction

12. Responsibility Transaction Rule

Responsibility Transaction:

Type: Expense
Category: Required
Category.BucketType: Responsibility

BudgetRadar:
不列入 Living Expense 計算，避免重複扣除（因已由 FixedCommitment 扣除預算）。

FixedCommitment:
不與 Transaction 自動同步。

13. Budget Radar API Contract

GET: /api/budget/radar?currentDate=2026-11-02
Date format: yyyy-MM-dd
Authentication: JWT Required

UserId:
由 JWT Claims 取得。Client 不可傳 UserId。

Output (RadarStateDto 包含):

DailySafeLimit
TodaySpent
TodayRemaining
RemainingDays
TotalLivingBudget
UncategorizedExpenseAmount



CurrentDate Validation Rule

CurrentDate: Required

Invalid format: HTTP 400

CurrentDate is provided by Client.
Server does not replace missing value with DateTime.Today.

Budget Radar Error Response

400 Bad Request:

Missing CurrentDate

Invalid CurrentDate

404 Not Found:

User SystemSetting not found

401 Unauthorized:

Missing JWT

14. Budget Radar Layer Responsibility (層級權責劃分)

Controller

Responsible:

Receive CurrentDate query

Get UserId from JWT Claims

Return HTTP response

Service

Responsible:

Calculate payroll period

Calculate PayrollPeriodMonth

Calculate TotalLivingBudget

Calculate RemainingLivingBudget

Calculate DailySafeLimit

Repository

Responsible:

Query required data only

No business calculation

15. 資料存取規則 (Budget Radar Data Access Rule)

BudgetRadar 不允許載入全部 Transaction 後於 Memory 計算。

Transaction Repository Query Contract

BudgetRadar requires:

Input:

UserId

StartDate

EndDate

TransactionType

BucketType

Query Rules:
Living Expense:
WHERE UserId = current user
AND TransactionDate BETWEEN StartDate AND EndDate
AND Type = Expense
AND Category.BucketType = Living

Uncategorized:
WHERE UserId = current user
AND TransactionDate BETWEEN StartDate AND EndDate
AND Type = Expense
AND CategoryId IS NULL

FixedCommitment Repository Query Contract

Input:

UserId

PayrollPeriodMonth

Query Rules:
WHERE UserId = current user
AND StartMonth <= PayrollPeriodMonth
AND (EndMonth >= PayrollPeriodMonth OR EndMonth IS NULL)

16. 禁止事項

禁止：

Transaction 與 Account 更新分開儲存（必須在同一 DB Transaction）。

React 計算預算。

固定責任重複計入生活支出。

17. 開發順序

Phase 1: Transaction 原子化
Phase 2: Fixed Commitment CRUD
Phase 3: Budget Radar 預算雷達引擎與 API
Phase 4: Frontend Dashboard UI 串接

18. 測試要求

必須測試：

Transaction

Income 建立與餘額同步

Expense 建立與餘額同步

Transfer A → B

Balance rollback (InsufficientFundsException 或 SaveChanges 失敗)

Budget Radar

Payday 前一天、發薪日當天及發薪日後

跨月薪資週期的起訖邊界

Payday Invalid Date (如 2/31 轉 2/28)

FixedCommitment 在薪資週期月份內有效、尚未開始及已結束

Responsibility Transaction 不影響 Living Budget

未分類 Transaction 不列入 Living Budget (UncategorizedExpenseAmount 必須正確)

無剩餘生活預算情境

Budget Radar Integration Test Example

為了確保測試與預期結果一致，Integration Test 必須使用以下標準假資料來驗證公式邏輯：

Test Data Setup:

Income Transaction (本週期內) = 12000

FixedCommitment 1 (本週期有效) = 1000

FixedCommitment 2 (本週期有效) = 500
(Total FixedCommitment = 1500)

Living Expense Transaction (本週期內) = 3000

RemainingDays 假設 = 3

Expected Result Calculation:

TotalLivingBudget = 12000 - 1500 = 10500

RemainingLivingBudget = 10500 - 3000 = 7500

DailySafeLimit = 7500 / 3 = 2500

請勿遺漏任何一筆 FixedCommitment，以免導致 DailySafeLimit 的斷言 (Assert) 失敗。