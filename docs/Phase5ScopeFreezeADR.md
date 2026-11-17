# Phase 5 Scope Freeze ADR  
## Dashboard & Budget Radar Implementation

## Status

**Decision:** READY FOR IMPLEMENTATION

**Scope Source of Truth:** This document

**Phase:** Phase 5 — Dashboard & Budget Radar

---

# 1. Phase 5 Dashboard Scope

Phase 5 focuses on implementing the Dashboard read model.

Included:

- Dashboard summary data aggregation
- Account balance overview
- Monthly income summary
- Monthly expense summary
- Recent transaction display
- Goal progress display (only if existing domain data is already available)

Goal progress continues to use the existing `Goals` response collection. Phase 5 does not add a separate `GoalsProgress` collection.

The Phase 5 Dashboard response additions are limited to:

- `TotalAssets`
- `MonthlyIncome`
- `MonthlyExpense`
- `RecentTransactions` (maximum 5)

Requirements:

- Dashboard is read-only.
- Dashboard must consume existing domain data.
- Dashboard must respect existing Transaction soft-delete behavior.
- Deleted transactions must not affect displayed calculations.

Not included:

- New transaction behavior
- Transaction lifecycle changes
- Account balance calculation changes

---

# 2. Phase 5 Budget Radar Scope

Phase 5 focuses on presenting current budget status based on existing financial data.

Included:

- Current spending summary
- Budget usage visualization
- Remaining available amount calculation based on existing supported data
- No new spending category analysis in Budget Radar. Category breakdown remains the responsibility of Report.

The only Phase 5 Budget Radar response addition is `RemainingLivingBudget`, exposed from the existing `BudgetSnapshot`. Existing Radar fields remain unchanged. Phase 5 does not add `TotalBudget` or `TotalSpent` fields and does not implement `UsagePercentage` or `Status`.

Budget Radar responsibility:

- Display financial status
- Aggregate existing information
- Provide read-only insight

Budget Radar is NOT responsible for:

- Creating financial rules
- Modifying transaction behavior
- Changing account balance logic

---

# 3. Explicitly Out of Scope

The following items are excluded from Phase 5:

## Fixed Commitment Payment Tracking

Not included:

- Linking Expense Transaction to Fixed Commitment
- Creating payment matching logic
- Determining whether a commitment has been paid
- Adding transaction relationship fields

Reason:

Current domain model does not provide a reliable relationship between commitment planning and actual transaction payment.

---

## Transaction Domain Changes

Not included:

- Transaction entity changes
- Transaction creation flow changes
- Transaction deletion flow changes
- Balance calculation changes
- Migration changes related to Transaction

---

## AI Analysis Preparation

Not included:

- Spending prediction
- Financial recommendation engine
- AI categorization
- Behavior analysis

These belong to future phases.

---

# 4. Fixed Commitment Boundary in Phase 5

Fixed Commitment handling follows this rule:

## Phase 5 may:

- Read existing Fixed Commitment related data if already available through existing architecture.
- Display existing supported information.

## Phase 5 may not:

- Infer payment completion.
- Match transactions manually.
- Create new relationships.
- Modify Fixed Commitment domain behavior.

Any advanced Fixed Commitment calculation must wait for a future architecture decision.

---

# 5. Phase 6 Extension Path

Future phases may introduce:

- Transaction and Fixed Commitment relationship design.
- Payment tracking.
- Historical commitment analysis.
- AI-based financial insights.

Phase 5 implementation must avoid blocking these future extensions.

Design principle:

> Phase 5 provides reliable visibility first. Domain expansion happens only after a separate architecture decision.

---

# 6. Copilot Implementation Boundary

Copilot implementation rules:

## Allowed

- Extend the existing Dashboard read API based on existing architecture; do not add API endpoints.
- Create required DTOs.
- Create read-only aggregation services.
- Add API tests.
- Add frontend consumption after backend contract approval.

## Forbidden

- Adding new Entities without approval.
- Adding database migrations.
- Modifying Transaction schema.
- Modifying balance calculation rules.
- Creating Fixed Commitment payment tracking.
- Assuming repositories or services that do not exist.

---

# Final Decision

## READY FOR IMPLEMENTATION

Phase 5 can proceed under this frozen scope.

Any requirement outside this document requires a new Architecture Decision Review before implementation.