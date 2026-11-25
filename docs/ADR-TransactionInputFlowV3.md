Flow V3
Decision
使用 Bottom Sheet 作為 Dashboard 新增交易入口。
Entry Points
1. Dashboard FAB
2. DayDetailPanel +記一筆
Form Fields
Required:
Type
Amount
Account
TransactionDate

Optional:
Description

Pending:
Category

Data Flow
TransactionBottomSheet

↓

useTransactionStore

↓

TransactionRepository

↓

POST /Transaction

↓

useDashboardData refresh


Non Goal
本階段不：
- 移除 TransactionPage
- 重構 Legacy Navigation
- 改 Backend Transaction Model
- 新增 Category System