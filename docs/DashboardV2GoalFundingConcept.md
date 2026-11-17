# Goal Funding Concept

## Status

Future product concept only. Not approved for implementation.

## Current Phase

Phase 0 is UI-only for Goal funding. The Dashboard continues to display
existing Goal data and behavior without adding funding-source selection,
account allocation, or spending relationships.

This document records a possible future direction. It does not define
classes, tables, API shapes, DTOs, or implementation details.

## Problem Statement

The current Goal `CurrentAmount` cannot clearly distinguish:

- Money explicitly contributed to a Goal.
- Funds already present in an account that a user may want to associate
  with a Goal.
- Goal funds already used for Goal-related spending.

Without these distinctions, a Goal amount may be mistaken for an account
balance, or an account's full balance may be incorrectly treated as
belonging to a Goal.

## Proposed Future Model

```text
Goal
|
+-- Target Amount
+-- Allocated Amount
+-- Used Amount
`-- Funding Source
    +-- Manual Contribution
    `-- Linked Account Allocation
```

These are conceptual terms, not approved fields or entities.

- **Target Amount:** the Goal's intended target under existing Goal
  semantics.
- **Allocated Amount:** the amount explicitly assigned to the Goal from one
  or more funding modes. It is not automatically an account's whole
  balance.
- **Used Amount:** the amount consumed for Goal-related spending. Whether
  this is tracked independently or derived from approved transaction rules
  remains undecided.
- **Funding Source:** the origin or mechanism by which an amount is
  associated with the Goal.

The future design must keep these financial facts distinct:

| Concept | Meaning |
|---|---|
| **Account Balance** | The account's actual recorded balance. |
| **Goal Allocated Amount** | The explicitly assigned portion associated with a Goal. |
| **Goal Spending Amount** | The amount used for Goal-related spending under rules that have not yet been defined. |

Linking an account does not make the entire Account Balance a Goal amount.
Any allocation must be explicit, and the same funds must not be counted
twice as independent assets.

## Funding Modes

### Manual Contribution

Conceptual flow:

1. The user chooses a Goal.
2. The user manually enters an amount to contribute to that Goal.
3. The contribution is recorded as Goal funding under future approved
   rules.

It remains undecided whether a manual contribution represents an actual
movement of money between accounts, a user-entered progress fact, or both.
It must not silently change Account Balance or create a Transaction before
those rules are approved.

### Linked Account Allocation

Conceptual flow:

1. The user chooses a Goal and an existing account as a funding source.
2. The user explicitly identifies an amount to allocate from that account.
3. The system associates only that allocated amount with the Goal.
4. If the user later spends Goal funds, the relationship between that
   spending, the Goal allocation, and any Transaction follows separately
   approved rules.

The linked account is a source reference, not a declaration that all funds
in the account belong to the Goal. The account's recorded balance remains
an Account Balance; the Goal's allocation and spending are separate
concepts.

## Risks

- **Account balance misinterpretation:** users may assume a linked account's
  entire balance is allocated to a Goal.
- **Duplicate asset calculation:** the same money may be counted as both
  account funds and additional Goal funds.
- **Transaction and Goal relationship complexity:** deciding when a
  Transaction contributes to, spends from, or is unrelated to a Goal can
  affect history, balances, reversals, and reporting.
- **Ambiguous current amount:** continuing to show one Goal amount without
  distinguishing contributed, allocated, and used funds may preserve the
  current ambiguity.
- **Unclear update and correction rules:** edits, refunds, transfers, and
  account changes could make allocations inconsistent without explicit
  business rules.

## Implementation Boundary

### Phase 0

**UI only.** Continue displaying the existing Goal information and
preserving current behavior. No Goal funding behavior is introduced.

Do not add:

- `GoalAccount` entity.
- `GoalTransaction`.
- Database migration.
- API contract or endpoint.
- DTO fields.
- Goal/account or Goal/transaction persistence relationships.
- New calculations for allocation, spending, or Goal progress.

### Future

Implementation may be considered only after separate ADR, API, and Domain
design is approved. That work must define the meanings and lifecycle of
Account Balance, Goal Allocated Amount, and Goal Spending Amount, including
how transactions affect them and how duplicate asset counting is avoided.

This concept does not decide whether persistence, migrations, or any
particular relationship is ultimately required.
