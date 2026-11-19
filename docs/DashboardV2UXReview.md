# Dashboard V2 UX Review

Status: Approved for Frontend Planning

## 1. Review Result

Frontend analysis confirms that the current React structure can support the Dashboard V2 UX redesign.

However, Goal Funding related UX cannot be implemented using current APIs because the current frontend still depends on legacy concepts:

- Goal.currentAmount
- Goal Deposit Transfer flow
- Transaction.GoalId assumptions

These must not be recreated in frontend.

---

# 2. Current Frontend Architecture Assessment

## Supported Immediately

The following changes can proceed without backend modification:

### Transaction Display

Affected:

- RecentActivity
- TransactionPage

Changes:

- Improve transaction title hierarchy.
- Display meaningful description.
- Display account/date/category/amount.
- Never infer income purpose.

---

### Dashboard Layout

Affected:

- DashboardPage
- Dashboard components

Changes:

Current:
Financial Overview
Budget Radar
Goals
Recent Activity
Account Details

Target:


Financial Overview

Goals Progress

Budget Radar

Recent Transactions

Account Details requires separate placement decision.

---

### Navigation

Current:


Dashboard
Responsibilities
Goal
Stats

Problem:

Navigation mixes:

- User tasks
- System functions
- Dashboard widgets

Target:


Dashboard
Transactions
Goals
Budget
Accounts

System actions:


Settings
Logout

---

# 3. Blocked UX Features

## Goal Funding Allocation Preview

Status:

Blocked

Reason:

Current frontend uses:


Goal Deposit
|
|
Transfer Account A -> Goal Account

But new domain requires:


Income Transaction
    +

Explicit Allocation
    ↓

Goal Funding

Do not reuse:


POST /goals/{id}/deposit

because it represents Transfer behavior.

---

Required backend capability:


GET Eligible Income Transactions

GET Existing Goal Allocations

POST Create GoalFundingAllocation

---

# 4. Goal Progress Display

Current:


Goal.currentAmount

Target:


GoalFundingSummary

Rule:

Frontend must not calculate:


Transaction Sum
+
Transfer
+
CurrentAmount

to recreate funding.

The backend owns funding calculation.

---

# 5. Implementation Order


## Phase 1 - Frontend Only

Allowed:

- Transaction UI redesign
- Dashboard layout redesign
- Navigation redesign

No backend dependency.


---

## Phase 2 - After Goal Funding API Exists

Implement:

- Allocation Preview
- Available Allocation Amount
- Allocation Validation


---

## Phase 3 - After Migration Complete

Replace:


Goal.currentAmount

with:


GoalFundingSummary

---

# 6. Frontend Rules

Frontend must:

- Display data
- Validate user input
- Guide user actions


Frontend must not:

- Infer transaction purpose
- Convert transfers into funding
- Calculate canonical Goal progress
- Create financial rules


---

# 7. Decision

Frontend redesign can start with Phase 1.

Goal Funding related UI must wait until backend Domain implementation and API contract are completed.