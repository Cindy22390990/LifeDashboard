# Budget Radar Phase 3C Decision Freeze

Status: MVP decisions recommended for product confirmation. This document does not implement any option or change the current API, DTO, UI, or calculation.

## 1. TodayRemaining State Model Comparison

| Model | Definition | Advantages | Risks and data needs |
|---|---|---|---|
| Simple threshold | Use the existing `TodayRemaining` value: positive means allowance remains, zero means exhausted, negative means overspent. | Easy to explain, deterministic, and fully supported by the current DTO. No forecast or additional data required. | Does not warn when the user is close to the limit. Product must define the exact zero-boundary wording and warning severity. A non-positive `DailySafeLimit` also needs careful copy because the daily budget itself may already be unavailable. |
| Ratio based | Compare `TodayRemaining` with `DailySafeLimit`, such as remaining percentage = `TodayRemaining / DailySafeLimit`, and apply bands (for example, low remaining percentage). | Can indicate approaching the daily limit before the amount reaches zero and adapts to different daily budget sizes. | Requires product-approved bands and explicit handling for zero/negative `DailySafeLimit`, negative remaining values, and rounding. The ratio is not a forecast and can change sharply after one purchase. |
| Spending velocity | Compare spending pace with elapsed time or project end-of-day spending from intraday transactions. | Could provide an early warning when spending is unusually fast, even while the remaining amount is still positive. | Current DTO has only an aggregate `TodaySpent`, not transaction timestamps or a time series. Forecasting needs a defined timezone/day boundary, sampling window, model, cold-start behavior, and false-positive policy. Adds more complexity and can imply predictive certainty the data does not support. |

## 2. Dashboard Positioning Comparison

### Financial Planning Dashboard

Suggested order: Header → Financial Overview → Goal Pyramid → Budget Radar → Account Overview → Quick Add.

- Supports periodic review of account balances and life goals; aligns with the current Dashboard order and Goal Pyramid's role as a major product feature.
- Keeps daily budget and entry actions below planning content, so frequent bookkeeping requires more scrolling.
- Best when users primarily visit to review financial position and goal progress.

### Daily Bookkeeping Dashboard

Suggested order: Header → Budget Radar → Quick Add → Account Overview → Financial Overview/Goal Pyramid.

- Puts today's spending decision and transaction entry at the start of the workflow.
- Reduces the distance between checking today's remaining allowance and recording a transaction.
- Makes the life-goal planning experience less prominent and changes the first impression from planning to daily control.
- Best when daily expense logging and immediate spending decisions are the primary success measures.

## 3. UncategorizedExpenseAmount Policy Comparison

### Keep excluded from the available-budget calculation (current policy)

- `UncategorizedExpenseAmount` is reported separately; it is not subtracted from `TotalLivingBudget`, `RemainingLivingBudget`, `DailySafeLimit`, or `TodayRemaining`.
- This matches the current BudgetSnapshot policy: an uncategorized expense cannot reliably be assumed to be a Living expense.
- Risk: a user may interpret `TodayRemaining` as net of all spending and overestimate the available amount if they overlook the uncategorized notice.
- MVP safeguard: retain the calculation and clearly label the notice as spending not yet classified/included in the Living budget.

### Conservatively reserve uncategorized spending

- Subtract some or all uncategorized expenses from the available budget until categorized.
- Advantage: reduces the chance of presenting an overly optimistic remainder.
- Risks: the API does not identify whether an uncategorized transaction is Living, Responsibility, Goal Purchase, or another type of expense. Reserving all of it can understate the Living allowance; subtracting it again after categorization or applying it in the wrong period can also produce misleading totals.
- This option changes BudgetSnapshot semantics and requires a domain/API contract review plus acceptance cases. It should not be implemented as a UI-only adjustment.

## 4. Recommended MVP Decision

1. Use the simple threshold model because it is explainable and uses only the existing DTO values:
   - `TodayRemaining > 0`: remaining allowance is available.
   - `TodayRemaining == 0`: allowance is exhausted; show a caution/exhausted state rather than a positive success state.
   - `TodayRemaining < 0`: over the daily allowance; preserve and display the negative amount with a warning state.
2. Do not define an approaching-limit ratio or spending-velocity forecast until product owners approve thresholds and required data semantics.
3. Keep the Financial Planning Dashboard order for the MVP: financial overview and Goal Pyramid remain prominent, with Budget Radar as a clearly emphasized daily decision panel and Quick Add retained as an available action. Revisit ordering if daily-entry frequency is the primary product KPI.
4. Keep uncategorized spending outside the Living budget calculation, consistent with the current domain policy. Make the notice explicit that the amount has not been included in the displayed Living allowance; do not imply it was deducted.

These are recommended product decisions, not implementation instructions. The current component still treats zero as the non-warning state until a separate implementation is approved.

## 5. Data Needed for Future Phase 4

- **Approaching-limit bands:** product-approved thresholds and equality/rounding rules; define behavior when `DailySafeLimit <= 0`. Current values are sufficient to calculate a ratio only when the denominator is positive, but they do not supply the policy.
- **Spending velocity:** timestamped or time-bucketed qualifying Living expense aggregates for the current local day, timezone/day-boundary semantics, and a defined forecast window. A single `TodaySpent` total cannot establish pace.
- **Payroll-period context:** period start/end, period living expense, remaining living budget, and fixed commitment total if the UI is to explain why the daily allowance changed or show period-level progress. These values exist in internal snapshot/read-model logic but are not exposed in `RadarStateDto`.
- **Uncategorized policy:** period and today uncategorized amounts, explicit inclusion/exclusion semantics, and (if classification or review actions are needed) transaction identifiers/details and category availability. Do not infer the category from the amount or note.
- **Goal Purchase context:** amount/count and period scope if users need the Radar to explain spending excluded from Living budget. The current DTO does not expose these internal aggregates.
- **Dashboard workflow choice:** user research or product analytics for daily-entry frequency, Radar visibility, Quick Add usage, and Goal Pyramid engagement to decide between planning-first and daily-bookkeeping-first layouts.

No Phase 4 behavior should be inferred solely from missing or zero DTO fields; first define whether the data is absent, unavailable, or a valid zero.