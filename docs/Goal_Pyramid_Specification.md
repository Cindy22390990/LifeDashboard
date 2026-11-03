# Goal Pyramid (目標金字塔) 重構規格書

Version:
v1.0

Status:

Phase 1: Goal CRUD Pending
Phase 2: Goal Deposit Pending
Phase 3: Goal Purchase Pending
Phase 4: Frontend Pyramid UI Pending


# 1. 模組目的

Goal Pyramid 用於管理使用者：

- 短期儲蓄目標
- 中期財務目標
- 長期夢想目標
- 購物願望

核心原則：

- Backend 負責商業邏輯
- Frontend 只負責呈現
- Goal 金額異動必須有 Transaction 紀錄


---

# 2. Domain Model


## Goal Entity

欄位：
Id
UserId
Name
TargetAmount
CurrentAmount
Tier
GoalType
Status
Note
CompletedDate


---

# 3. Enum


## GoalTier


Tier1_Safety = 1

安全預備金
Tier2_Burden = 2

中期負擔 / 大額目標
Tier3_Dream = 3

夢想目標


## GoalType


Saving = 1

純存錢目標

Purchase = 2

購物願望


## GoalStatus


Active = 1

進行中

Completed = 2

已完成


---

# 4. Business Rules


## CurrentAmount 規則


Goal.CurrentAmount：

只代表：

「目前累積進度」


禁止：

直接修改 CurrentAmount。


禁止建立：

PUT /api/goals/{id}/amount


所有金額變化必須透過：

Transaction


---

# 5. Deposit Flow


使用者對 Goal 存錢：


流程：


來源帳戶

↓

Transfer Transaction

↓

目標帳戶

↓

增加 Goal.CurrentAmount


要求：

- 必須同一個資料庫交易
- 任一步失敗全部 Rollback


完成條件：

如果：


CurrentAmount >= TargetAmount

且：


GoalType = Saving

則：


Status = Completed

CompletedDate = Today


---

# 6. Purchase Flow


只適用：


GoalType = Purchase


流程：



Expense Transaction

↓

扣除 Account Balance

↓

Goal Status Completed


Category：

不可歸類 Living。


建議：

Entertainment

或

GoalAchievement


---

# 7. API Design


## Deposit


POST


/api/goals/{id}/deposit


Request:

```json
{
 "Amount":5000,
 "FromAccountId":"",
 "ToAccountId":"",
 "Note":"存入目標"
}

Purchase
POST
/api/goals/{id}/purchase

Request:
{
 "ActualAmount":45000,
 "AccountId":"",
 "CategoryId":"",
 "Note":"完成願望"
}

8. Permission
所有 API：

JWT Required

UserId：

只能從：

JWT Claim

取得。

禁止：

Frontend 傳 UserId。
9. Frontend Rules
Frontend:

只負責：
- 顯示 Goal
- 顯示 Progress
- 顯示 Tier
禁止：

自行計算：
- Tier Lock
- Completed
- CurrentAmount
如果需要 Lock 狀態：

由 Backend DTO 提供。
10. Delete Policy
已完成 Goal：

不可直接 Delete。

建議：

Soft Delete

或

Hide。
11. Implementation Order
Phase 1
Goal CRUD

範圍：

Backend:
- Entity
- Migration
- DTO
- Controller
- Service
Frontend:
- Goal API
- Store
- Basic List/Create/Edit
不要做：
- Deposit
- Purchase
- Pyramid UI
Phase 2
Goal Deposit

新增：

Transaction Atomic Flow
Phase 3
Goal Purchase
Phase 4
Frontend Pyramid UI
12. 禁止事項
禁止：
- 修改 Budget Radar
- 修改 Transaction 核心邏輯
- 新增未定義 API
- 前端計算商業規則
- Mock API
- 使用假資料
每 Phase 完成後：

執行：

dotnet test LifeDashboard.slnx

以及：

cd LifeDashboard.Web

npm run build

回報：
- 修改檔案
- 新增檔案
- API
- Test結果
- 尚未完成項目