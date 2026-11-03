# Core Transaction & Budget Radar 重構規格書

版本：
v1.0

目的：

建立 LifeDashboard 核心金流管理與預算雷達系統。

---

# 1. 模組目的

核心負責：

- 收入管理
- 支出管理
- 帳戶轉帳
- 固定責任
- 每日生活預算計算


系統目標：

將預算計算集中於 Backend。

Frontend 不負責任何預算邏輯。

---

# 2. Transaction 模組

## Transaction Entity


欄位：

```text
Id
UserId
AccountId
TargetAccountId
CategoryId
Amount
Type
TransactionDate
Note
```


Type:

```text
Expense = 1

Income = 2

Transfer = 3
```


---

# 3. Transaction 原子性


所有交易必須符合：

ACID


新增交易流程：

```text
Begin Transaction

新增 Transaction

更新 Account Balance

SaveChanges

Commit

```

任何錯誤：

Rollback


---

# 4. Transfer 規則


轉帳：

來源：

AccountId


目標：

TargetAccountId


流程：

```text
檢查來源餘額

扣除來源

增加目標

建立 Transaction

Commit

```


不足：

拋出：

InsufficientFundsException


---

# 5. Fixed Commitment


用途：

管理固定支出。


例如：

- 房租
- 保險
- 分期
- 孝親費


Entity:


```text
FixedCommitment

Id

UserId

Name

MonthlyAmount

CommitmentType

StartMonth

EndMonth
```


---

# 6. Budget Radar


目的：

計算使用者目前可使用生活預算。


輸入：

```text
UserId

CurrentDate
```


輸出：

```text
RadarStateDto
```


包含：


```text
DailySafeLimit

TodaySpent

TodayRemaining

RemainingDays

TotalLivingBudget

```


---

# 7. 發薪週期


依：

SystemSetting.Payday


例如：

Payday = 5


目前：

2026-11-02


週期：

2026-10-05

至

2026-11-04


---

# 8. 預算公式


生活預算：


```
收入 - 固定責任 = 可使用生活預算
```


每日標準：


```
剩餘生活預算 / 剩餘天數
```


---

# 9. Frontend 規則


Frontend 不允許：

- 計算日期
- 計算固定責任
- 計算剩餘預算


Frontend 只呼叫：

```
GET /api/budget/radar
```


顯示：

- 今日標準
- 今日已花
- 今日剩餘
- 剩餘天數


---

# 10. 禁止事項


禁止：

1. Transaction 與 Account 更新分開儲存。

2. React 計算預算。

3. 固定責任重複計入生活支出。

4. 未確認直接修改 Migration。


---

# 11. 開發順序


Phase 1:

Transaction 原子化


Phase 2:

Fixed Commitment


Phase 3:

Budget Radar


Phase 4:

Frontend Dashboard


---

# 12. 測試要求


必須測試：


## Transaction

- Income
- Expense
- Transfer
- Balance rollback


## Budget Radar

測試：

- 發薪日前
- 發薪日後
- 跨月
- 固定責任
- 無剩餘預算


```