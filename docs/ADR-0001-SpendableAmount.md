# ADR-0001: Spendable Amount

## Status

Proposed

## Date

2026-11-17

## Context

Dashboard V2 is intended to answer a user's practical question: “How much
money can I safely use now?” A clearly defined Spendable Amount could serve
as a concise primary indicator, distinct from a financial report or a list of
account balances.

The current Domain Model contains account balances, transactions, Goals,
Fixed Commitments, a Payday-period Budget Snapshot, and an Allocation Ratio.
These concepts represent balances, recorded activity, plans, obligations,
period budgets, and configuration. None explicitly records the amount of
money that has been reserved and is therefore unavailable for ordinary use.
Combining existing values into a Spendable Amount would require assumptions
about which funds are available and which plans or obligations should reduce
availability.

The following concepts are distinct and must not be treated as synonyms:

| Concept | Meaning |
|---|---|
| **Account Balance** | The amount currently recorded for an account. It says where funds are recorded, but does not by itself establish whether they are accessible, liquid, or intended for spending. |
| **Budget Remaining** | The unused amount under a budget rule for a particular budget period. The current `BudgetSnapshot.RemainingLivingBudget` is tied to the Payday-period budget behavior; it is not a consolidated account balance. |
| **Goal Amount** | Progress recorded toward a Goal's target. It does not necessarily mean funds are held separately or reserved. A Goal deposit may be represented as a transfer between accounts. |
| **Reserved Money** | Funds intentionally set aside and not available for ordinary spending, according to an explicit business rule. The current model has no general representation for this concept. |
| **Spendable Amount** | A user-facing interpretation of funds currently eligible for ordinary use after applying approved account eligibility and reservation rules. Its exact scope and rules have not been defined. |

This ADR documents the decision space. It does not define a formula, select
an implementation, or authorize changes to code, APIs, DTOs, database schema,
migrations, or tests.

## Problem

The business meaning and inputs for Spendable Amount are ambiguous:

1. **Goal deposits are not equivalent to reserved cash.** A Goal's
   `CurrentAmount` tracks progress, while a saving deposit can move money
   between accounts. Subtracting Goal progress from balances could count the
   same money twice. It is not decided whether a Goal reserves money before
   a transfer, after a transfer, or at all.
2. **Allocation Ratio is configuration only.** It describes preferred
   proportions, not actual allocations, transfers, or reservations. It is
   unknown whether it should ever constrain an amount that can be spent.
3. **Fixed Commitments are planned obligations.** The current model gives a
   monthly amount and active month range but does not say whether a
   commitment is due, paid, overdue, or already represented by a transaction.
   The amount and timing to reserve are unknown.
4. **Account Balance includes different kinds of money.** Recorded balances
   may include funds with different liquidity, restrictions, purpose, or
   availability. The current model does not establish that all balances can
   be used in the same way.
5. **Account participation is undefined.** It is unknown whether cashflow,
   savings, investment, credit-card, system, or other account types should
   participate, and how negative balances or overdrafts should be treated.
6. **Transactions describe recorded activity, not every obligation.** It is
   unclear whether pending, scheduled, uncleared, or future transactions
   exist in the product's accounting boundary or should reduce availability.
   Transfers must not be mistaken for new income or expenses.
7. **Budget Remaining has a different scope.** `BudgetSnapshot` represents a
   budget period based on existing Payday rules. It is unknown whether users
   want Spendable Amount to be an immediate cash figure, a Payday-period
   planning figure, or something else.
8. **Reservations have no shared lifecycle.** The model does not define who
   can create a reservation, when it takes effect, how it changes, or when
   it is released.
9. **Double-counting boundaries are unresolved.** A commitment may later be
   paid through a transaction; a Goal deposit may move funds; a budget may
   already account for expenses. It is unknown how these events would
   interact with any reserved amount.
10. **User adjustments are unspecified.** It is unknown whether users can
    override or create reservations manually, and how such changes should be
    explained or audited.

## Options

### Option A — Do not provide Spendable Amount

Continue showing existing values with their established labels, such as
Account Balance and Budget Remaining. Do not present a consolidated
Spendable Amount until its business meaning is defined.

**Pros**

- Avoids presenting an assumption as a financial fact.
- Preserves the existing semantics of account balances, Goals, commitments,
  and budget values.
- Avoids double-counting or misleading users about which funds are available.
- Requires no new domain concept to support this KPI.

**Cons**

- Dashboard V2 cannot yet answer its intended “how much can I use?” question
  with one value.
- Users must interpret several existing figures themselves.
- The Dashboard may need to defer or omit a planned primary KPI.

### Option B — Calculate using existing data

Derive a Spendable Amount from some combination of current balances,
transactions, Goals, Fixed Commitments, Budget Snapshot, or Allocation Ratio,
without introducing an explicit Reserved Money concept.

**Pros**

- Could reuse existing read models and may appear to provide a result sooner.
- Avoids immediately introducing a new domain concept.
- May be sufficient for a narrower, explicitly named figure if business
  stakeholders define a limited meaning and accept its limitations.

**Cons**

- Existing values were not defined as a complete representation of
  spendability.
- Any deduction or inclusion rule would encode assumptions not represented
  in the Domain Model.
- Goal progress, transfers, fixed commitments, and recorded expenses can
  overlap and be counted more than once.
- A value based on a budget remainder could be mistaken for available cash;
  a value based on all account balances could include funds not intended or
  eligible for ordinary use.

**Risks**

- Users may make spending decisions based on an overstated or understated
  value.
- Results may change unexpectedly when account types, budget periods, or
  transaction behavior change.
- Later introduction of explicit reservations may require changing the
  value's meaning and historical interpretation.
- Labelling the value “Spendable” could communicate a guarantee the model
  cannot support.

### Option C — Introduce a Reserved Money domain concept

Represent, at the business-concept level, money that has been intentionally
set aside and is unavailable for ordinary spending. Spendable Amount could
then be considered only after the business defines how reservations relate
to eligible balances and other domain concepts.

This option is conceptual only. It does not select classes, tables, APIs,
or a calculation.

**Pros**

- Makes an important business fact explicit rather than inferring it from
  Goal progress, ratios, or budget remainders.
- Can provide a basis for explaining why some recorded funds are not
  considered available.
- Offers a clearer boundary for discussing planned obligations and
  intentional set-asides.

**Cons**

- “Reserved” itself needs precise rules for creation, ownership, amount,
  period, changes, release, and auditability.
- Some reservations could overlap with Goals, Fixed Commitments, or
  transactions and would need explicit non-duplication rules.
- Adds domain complexity and may require coordinated changes across several
  product areas after approval.
- Does not by itself answer which accounts count or what “spendable” means.

**Future extensibility**

If defined carefully, an explicit reservation concept could later support
different reasons for setting money aside, such as Goals, upcoming
obligations, or user-created buffers. It could also support explanation and
reporting of reserved amounts. These are possible future capabilities, not
decisions made by this ADR.

## Domain Concepts That May Be Required

The following terms identify business problems that may need explicit
definitions. They are not a design for classes, tables, or persistence.

| Concept | Business problem it may solve |
|---|---|
| **Reserved Money** | Expresses that some funds are intentionally unavailable for ordinary spending, rather than merely present in an account. |
| **Locked Balance** | Distinguishes funds that cannot currently be accessed due to a restriction from funds intentionally reserved by the user or a plan. |
| **Reserved Transaction** | Describes an expected or pending transaction that should affect availability before it is reflected as a finalized balance change, if the product supports that behavior. |
| **Cash Available** | Clarifies the subset of funds that is liquid and accessible now. It may differ from total assets, which can include non-cash or less accessible balances. |
| **Protected Balance** | Represents a minimum cushion or amount the user or product intends to keep untouched, distinct from a specific Goal or bill. |
| **Obligation reservation** | Explains how an upcoming Fixed Commitment becomes unavailable, and how it stops being reserved when paid, cancelled, or no longer active. |
| **Goal reservation** | Clarifies whether a Goal has funds set aside, how that differs from Goal progress, and whether a transfer already moved the funds. |
| **Reservation lifecycle** | Makes clear when a reservation begins, changes, is fulfilled, or is released so amounts do not remain reserved indefinitely or disappear without explanation. |

These concepts may overlap. Their boundaries and relationships require
business decisions before any implementation design.

## Questions Requiring Business Decision

The following rules remain unanswered:

### Meaning and time horizon

- Is Spendable Amount intended to mean immediately accessible cash, funds
  available for the rest of the current day, the remaining Payday period, or
  another time horizon?
- Is the value a factual balance, a planning estimate, or a conservative
  recommendation?
- May the value be negative, and if so, how should a negative result be
  explained?

### Account scope

- Should cashflow accounts participate?
- Should savings accounts count?
- Should investment accounts count, and if so, at what valuation or liquidity
  assumption?
- Should credit-card accounts participate? If so, does outstanding debt
  reduce the amount, and how are credit limits treated?
- Should system accounts be excluded?
- How should account restrictions, locked funds, negative balances, and
  overdrafts be treated?
- Is currency conversion in scope if accounts can use different currencies?

### Goals and allocations

- Should Goal money become unavailable immediately when the Goal is created,
  when the user marks funds as set aside, or only after money is transferred?
- Does a Goal deposit reserve the transferred funds, or are those funds
  already represented by the destination account's balance?
- Should purchase Goals and saving Goals have different reservation behavior?
- Does Allocation Ratio remain planning guidance, or should any ratio
  correspond to actual reserved amounts?

### Fixed Commitments and other obligations

- Should upcoming Fixed Commitments reserve money?
- If yes, when does reservation begin, which period is used, and how much of
  a recurring amount is reserved at a time?
- How is a commitment considered paid, including partial payment, late
  payment, cancellation, or a payment recorded without a commitment link?
- Should other planned obligations, beyond Fixed Commitments, be included?

### Transactions and lifecycle

- Should pending, scheduled, or uncleared transactions affect availability?
- When a reserved obligation is recorded as a transaction, how is duplicate
  reduction avoided?
- How do transfers between participating accounts affect availability?
- What happens to reservations when transactions are edited, deleted, or
  reversed?
- Can reservations be created, changed, or released manually? If so, who can
  do so and should the reason/history be visible?
- What event or date releases a reservation that is no longer needed?

### Budget relationship and user communication

- Is Budget Remaining only contextual information, or should a defined
  budget rule ever constrain Spendable Amount?
- How should Payday period boundaries relate to the chosen Spendable Amount
  time horizon?
- Which inputs and exclusions must be shown so users can understand and
  trust the value?

## Impact Analysis

The following describes potential impact if business rules are approved. It
does not authorize changes to these areas.

| Area | Potential impact of future decisions |
|---|---|
| **Dashboard** | A defined meaning could support a Spendable Amount KPI. The Dashboard would need clear period/context labels and an explanation of what is included or excluded; it must not substitute Budget Remaining or Total Assets without an explicit decision. |
| **Goal** | A decision that Goal funds become reserved would affect how Goal progress relates to balances, transfers, completion, cancellation, and user expectations. If Goals do not reserve money, the Dashboard must avoid implying that Goal amounts are unavailable. |
| **Budget** | Decisions about time horizon and Fixed Commitment treatment could affect how a Payday-period budget is presented alongside Spendable Amount. Existing Budget Remaining semantics should remain distinct unless a separate decision changes them. |
| **Reports** | Users may need to distinguish actual income/expense from reserved or protected amounts. Reports could otherwise present planned reservations as if they were transactions or actual spending. |
| **Transactions** | Matching or settlement decisions could determine when a reservation becomes a recorded transaction and how edits, deletion, reversals, transfers, or pending activity avoid duplicate effects. |
| **Accounts** | Account eligibility, liquidity, restrictions, negative balances, credit, and currency rules could affect which balances contribute to the concept and how account-level amounts are explained. |

## Recommendation

Postpone implementing Spendable Amount until the business rules above are
finalized. Until then, continue to present existing values only under their
current meanings, such as Account Balance and Payday-period Budget Remaining.
Do not calculate or label a combined amount as Spendable Amount from current
data.

This is a recommendation about timing and semantic safety. It does not select
Option A, B, or C as the future implementation approach, and it does not
approve a formula or domain design.

## Assumptions

- The current Domain Model remains the source of truth for the concepts
  described in this ADR; no unreviewed reservation model is assumed.
- `BudgetSnapshot.RemainingLivingBudget` retains its existing Payday-period
  budget meaning.
- Goal progress and account balances may refer to overlapping money, so
  treating both as separate deductions is unsafe without an explicit rule.
- The requested `ADR-XXXX` identifier is a placeholder. Since no numbered
  ADR filename exists in `docs/`, this document uses `ADR-0001`.

## Unanswered Questions

All business rules listed in “Questions Requiring Business Decision” remain
open. In particular, account eligibility, Goal and commitment reservation
timing, pending transaction behavior, period boundaries, and manual override
policy must be resolved before selecting an approach.

## Risks

1. **Financially misleading output:** An assumed value could cause users to
   spend funds that are restricted or intended for another purpose.
2. **Double-counting or omission:** Goals, transfers, commitments, budgets,
   and transactions can overlap; incomplete rules can either deduct funds
   twice or fail to reserve them.
3. **Semantic drift:** Reusing an existing value under a new label could
   change user expectations without changing the underlying data.
4. **Cross-area inconsistency:** Different rules in Dashboard, Goal, Budget,
   Reports, Transactions, and Accounts could show contradictory views of
   availability.
5. **Unbounded domain complexity:** Introducing reservations before defining
   their lifecycle and ownership could create orphaned, stale, or
   unauditable amounts.
