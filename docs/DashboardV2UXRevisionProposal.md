# Dashboard V2 UX Revision Proposal

**Status:** Proposed

## 1. Context

Dashboard V2 Backend Domain 已完成 Goal Funding Domain Model 決策。

目前前端存在數個與新 Domain 不一致的 UX 問題：

1. Goal Funding 分配流程缺少「可分配金額」概念。
2. Transaction 最近紀錄資訊不足，使用者無法追溯收入來源。
3. Dashboard 區塊 Responsibilities / Goal / Stats 職責混亂。
4. Navigation 結構將主要功能與系統設定混在一起。

本文件只定義 Frontend UX 與 Information Architecture。

不修改：
- Domain Model
- Database Schema
- API Contract
- Backend Service

---

# 2. Transaction Display Decision

## Problem

目前最近交易顯示：


🟢 收入

華南銀行

+$33,521

問題：

使用者未來無法判斷：

- 這是哪筆收入？
- 是否為薪資？
- 是否已分配給 Goal？

Transaction 是財務 Audit Trail，顯示資訊必須支援追溯。

---

## Decision

Transaction List 必須優先顯示：


Transaction Title
Category / Account
Date
Amount

---

## Display Rule

### 有使用者備註

Example:


11月薪資

華南銀行
2026/11/05

+33,521

---

### 無備註

不可自行推論。

禁止：


華南銀行
↓
薪資

系統不可猜測收入用途。

Fallback:


收入

華南銀行
2026/11/05

+33,521

---

# 3. Goal Funding Allocation Preview Decision

## Problem

目前 Funding Allocation UI 缺少剩餘可分配金額。

使用者無法知道：

- 還能分配多少
- 是否超過收入金額

---

## Decision

Allocation Preview 必須顯示：


Eligible Transaction Amount
-
Existing Allocations

=
Available Allocation Amount

---

Example:


薪資收入

+50,000

已分配：

緊急預備金
10,000

旅遊基金
5,000

剩餘可分配：

35,000

---

## Validation Rule

Frontend 必須阻止：


Allocation Total > Available Amount

Example:


收入 50,000

分配：
緊急預備金 30,000
旅遊基金 30,000

禁止提交

---

## Important Boundary

Frontend 不負責推論：

- 哪筆收入應該存錢
- 哪筆 Transfer 是 Goal Funding
- 自動建立 Allocation

Funding 必須由使用者明確操作。

---

# 4. Dashboard Information Architecture

## Current Problem

目前：


Stats

Goals

Responsibilities

三者資訊層級混亂。

---

## Decision

Dashboard 改為以下結構：


Dashboard

├── Financial Overview
│
├── Goals Progress
│
├── Budget Radar
│
└── Recent Transactions

---

# 5. Section Responsibilities


## Financial Overview

Purpose:

回答：

「我現在財務狀況如何？」

包含：

- Total Balance
- Monthly Income
- Monthly Expense
- Available Amount


---

## Goals Progress

Purpose:

回答：

「我要完成哪些財務目標？」

包含：

- Goal Name
- Target Amount
- Current Funded Amount
- Progress


注意：

Current Funded Amount 來源：

GoalFundingSummary / Read Model

不可直接依賴 Goal.currentAmount。

---

## Budget Radar

Purpose:

回答：

「我的支出是否符合預算規則？」

包含：

- Category Budget
- Remaining Budget
- Warning


---

## Recent Transactions

Purpose:

回答：

「最近發生哪些金流？」

包含：

- Description
- Account
- Date
- Amount


---

# 6. Navigation Revision


## Current Issue

目前：


交易記錄
新增交易
系統設定
管理帳戶
登出

問題：

- Page Navigation 與 Action 混合。
- System Function 與 User Task 混合。

---

# Decision

## Primary Navigation

主要功能：


Dashboard

Transactions

Goals

Budget

Accounts


---

## Primary Action

新增交易不是 Navigation。

改為：


Transactions Page
          + 新增交易



或：


Dashboard
          + 新增交易



---

## Secondary Navigation

系統功能：


Settings

Logout

放：

- Sidebar Bottom
- User Menu

---

# 7. Component Responsibility

Frontend Component 不應包含：

- 財務推論
- Allocation 自動判斷
- Goal 完成計算
- Transaction 分類猜測


Frontend 只負責：

- 顯示資料
- 收集使用者輸入
- 基本 Validation
- 呼叫 Application API

---

# 8. Implementation Boundary

## Phase 1

只完成：

- Transaction Display redesign
- Dashboard Layout redesign
- Navigation redesign


## Phase 2

完成：

- Goal Funding Allocation Preview UI


## Phase 3

完成：

- Goal Funding Dashboard Integration


---

# 9. Acceptance Criteria

完成後：

## Transaction

User can answer:

「這筆錢是什麼？」

---

## Funding

User can answer:

「我還能分多少錢給目標？」

---

## Dashboard

User can answer:

「我的財務狀況、目標、預算、近期交易在哪裡？」

---

## Navigation

User can answer:

「我要去哪裡完成某件事？」

---

# 10. Out of Scope

本文件不包含：

- Backend API redesign
- Database migration
- Domain Model change
- Automatic saving rules
- AI financial recommendation