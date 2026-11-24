# Dashboard V3 Calendar Implementation Specification

> **Status**: Implementation Ready  
> **Scope**: Backend Read API + Frontend Calendar Integration  
> **Architecture Constraint**: Clean Architecture + API-only Dashboard V3  
> **Authority**: This document is the implementation contract for Calendar Feature.

---

# 1. Objective

完成 Dashboard V3 Calendar 功能，使使用者可以：

1. 任意選擇日期
2. 查詢指定日期交易明細
3. 查看指定日期收入與支出總額
4. 在月曆上顯示有交易紀錄的日期標記

本功能不修改：

- Transaction Domain Entity
- EF Core Migration
- Existing Dashboard Summary API
- Transaction CRUD Contract

Calendar 採用獨立 Read Query。

---

# 2. Architecture Boundary

## Backend Flow


Controller
|
Dashboard Calendar Query
|
Application Read Service
|
Repository / Query
|
Transaction Entity

禁止：

- Controller 直接操作 DbContext
- Calendar API 呼叫 Transaction CRUD API
- 修改 Domain Entity 配合 UI

---

## Frontend Flow


Calendar Component
|
useDashboardCalendar Hook
|
IDashboardDataSource
|
ApiDashboardDataSource
|
Backend API

禁止：

- Calendar state 放入 useDashboardData
- 日期切換重新載入 Dashboard Hero
- 引入 TanStack Query

---

# 3. Backend Implementation

## 3.1 New Endpoints

新增：


GET /api/dashboard/v3/calendar/day

用途：

取得指定日期交易資訊。

---

## Query Parameters

| Parameter | Type | Required | Example |
|-|-|-|-|
| date | DateOnly | Yes | 2026-11-24 |

Format:


YYYY-MM-DD

---

# 4. Calendar Day Response Contract


## DashboardDayDetailDto

```json
{
  "date": "2026-11-24",
  "totalIncome": 30000,
  "totalExpense": 1200,
  "transactions": []
}

Fields
Field	Type	Description
date	string	Business Date
totalIncome	decimal	Income total
totalExpense	decimal	Expense total
transactions	array	Transaction details


5. Day Transaction DTO
DayTransactionItemDto
Contract must match RecentTransactionDto.
{
"id": "uuid",
"note": "Lunch",
"categoryName": "Food",
"accountName": "Main Account",
"amount": 120,
"transactionType": "Expense",
"transactionDate": "2026-11-24"
}

Rules
Transaction Filter
Include:
IsDeleted == false
AND
TransactionDate == query date

Sorting
Order:
CreatedAtUtc DESC

Transaction Type Rule
Allowed values:
Expense
Income
Transfer

Do not expose numeric enum values.
6. Income / Expense Calculation
totalIncome
Include:
TransactionType == Income

Exclude:
Transfer
Deleted Transaction

totalExpense
Include:
TransactionType == Expense

Exclude:
Transfer
Deleted Transaction

Transfer
Transfer must:
- remain inside transactions[]
- not affect totalIncome
- not affect totalExpense
7. Calendar Month Summary API
Endpoint:
GET /api/dashboard/v3/calendar

Purpose:

提供月曆日期標記。
Query
Parameter	Type
year	int
month	int


Example:
/calendar?year=2026&month=11

8. Month Response
{
 "year":2026,
 "month":11,
 "days":[
   {
    "date":"2026-11-01",
    "totalIncome":30000,
    "totalExpense":500
   }
 ]
}

Rules
Only return:
TransactionDate exists
AND
IsDeleted == false

No transaction date should not return.
9. Frontend Implementation
9.1 DataSource Extension
File:
IDashboardDataSource

Add:
getCalendarDayDetail(
 date:string,
 options?:{
   signal?:AbortSignal
 }
)

Add:
getCalendarMonthSummary(
 year:number,
 month:number,
 options?:{
   signal?:AbortSignal
 }
)

10. Mapper
Create:
calendarMapper.ts

Responsibilities:
- DTO → ViewModel
- TransactionType mapping
- Privacy formatter binding
- Note display rule reuse
11. ViewModel
CalendarDayDetailViewModel
{
 date:string;

 totalIncome:number;

 totalExpense:number;

 transactions:RecentTransactionViewModel[];

 status:
   | "ready"
   | "empty";
}

CalendarMonthViewModel
{
 year:number;

 month:number;

 daysByDate:
 Record<string,CalendarDaySummary>;
}

12. Calendar Hook
Create:
useDashboardCalendar

Responsibilities:
- selected date
- month navigation
- API loading
- AbortController
- error handling
State
{
 selectedDate:string;

 viewedMonth:{
   year:number;
   month:number;
 };

 dayDetail:{
   data:null|CalendarDayDetailViewModel;
   loading:boolean;
   error:null|string;
 };

 monthSummary:{
   data:null|CalendarMonthViewModel;
   loading:boolean;
   error:null|string;
 }
}

13. Initial State Rule
selectedDate:

Source:
Dashboard Summary API
        |
businessDate

Do not use:
new Date()

Reason:

Backend business date is the source of truth.
14. Race Condition Handling
Requirement:

When user changes date:

Example:
11/01
 ↓
11/05
 ↓
11/10

Previous requests must cancel.

Implementation:
const controller =
 new AbortController();

Cleanup:
return ()=>{
 controller.abort();
}

15. Date Handling Rule
Forbidden:
new Date("2026-11-24")

Reason:

Timezone conversion risk.

Use:
date.split("-")

or existing DateOnly formatter.
16. Privacy Mode
Backend returns:

Raw decimal.

Example:
{
"totalExpense":1200
}

Frontend:
PrivacyFormatter
        |
display

Apply to:
- totalIncome
- totalExpense
- transaction amount
17. UI Behavior
Loading
When changing date:

Show:
Calendar Day Skeleton

Do not reload:
- HeroStats
- AccountSummary
- Dashboard Summary
Empty State
When no transaction:

Display:
當日無交易紀錄

Error
Allow:
Retry

Only refresh Calendar Day Detail.
18. Acceptance Criteria
Backend
Success
Request:
GET calendar/day?date=2026-11-24

Return:
200 OK

Invalid Date
Request:
date=test

Return:
400 Bad Request

Unauthorized
Without JWT:

Return:
401 Unauthorized

Frontend
Date Change
Given:
selectedDate=2026-11-01

When:

User selects:
2026-11-05

Then:
- previous request cancelled
- loading shown
- new data displayed
- Dashboard Hero unchanged
19. Implementation Order
Backend
1. Create DTO
2. Create Calendar Query Service
3. Add Controller Endpoint
4. Add Tests
Frontend
1. Extend DataSource Interface
2. Implement API calls
3. Create Mapper
4. Create useDashboardCalendar
5. Integrate Calendar UI
20. Out of Scope
Not included:
- Transaction editing from Calendar
- Drag & Drop
- Monthly budget calculation
- Calendar statistics chart
- Recurring transaction projection