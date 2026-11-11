# Phase 2 Goal Transaction Boundary Review

Date: 2026-11-09
Scope: Static architecture review of Goal deposit/purchase and transaction creation paths. No application behavior was changed for this review.

## Findings

### High: Unknown transaction types can be persisted without balance effects

`CreateTransactionAsync` casts the incoming integer directly to `TransactionType` without checking that the value is defined ([TransactionService.cs:97](../LifeDashboard.Application/Services/TransactionService.cs#L97)). Only `Transfer`, `Expense`, and `Income` receive explicit handling ([TransactionService.cs:116](../LifeDashboard.Application/Services/TransactionService.cs#L116), [TransactionService.cs:155](../LifeDashboard.Application/Services/TransactionService.cs#L155)); every non-transfer transaction is nevertheless added and saved ([TransactionService.cs:166](../LifeDashboard.Application/Services/TransactionService.cs#L166)). An unsupported type can therefore be recorded without the corresponding balance update.

Recommendation: reject values not defined by `TransactionType` before account mutation or persistence.

### Medium: TransactionService is coupled to the Goal feature

The general transaction service interface exposes a Goal-specific method that accepts a Domain `Goal` ([ITransactionService.cs:17](../LifeDashboard.Application/Services/Interfaces/ITransactionService.cs#L17)). Its implementation conditionally associates the transaction with that Goal ([TransactionService.cs:66](../LifeDashboard.Application/Services/TransactionService.cs#L66), [TransactionService.cs:151](../LifeDashboard.Application/Services/TransactionService.cs#L151)). This is not an outward Clean Architecture dependency violation: both interfaces and services are in the Application layer, and Goal is a Domain entity. It does, however, make the general transaction abstraction aware of a feature-specific use case.

Recommendation: if additional transaction-linked features are added, move the coordination/association policy to a Goal-specific application boundary or introduce a neutral transaction association contract rather than adding more feature-specific parameters to `ITransactionService`.

### Medium: Caller-owned transaction participation is a convention, not an enforced contract

`CreateTransactionWithinExistingTransactionAsync` skips beginning, committing, and rolling back a transaction ([TransactionService.cs:66](../LifeDashboard.Application/Services/TransactionService.cs#L66), [TransactionService.cs:76](../LifeDashboard.Application/Services/TransactionService.cs#L76)). It does not verify that a database transaction is active. The current Goal callers do begin a UnitOfWork transaction before calling it ([GoalDepositService.cs:53](../LifeDashboard.Application/Services/GoalDepositService.cs#L53), [GoalPurchaseService.cs:68](../LifeDashboard.Application/Services/GoalPurchaseService.cs#L68)), then own commit/rollback. A future caller that omits that step could save changes without the intended encompassing transaction.

Recommendation: make transaction participation harder to misuse, for example by exposing an explicit transaction scope/participant abstraction or validating the active transaction at the UnitOfWork boundary.

### Low: Purchase category is queried twice

`GoalPurchaseService` reads the category to enforce the non-Living rule ([GoalPurchaseService.cs:58](../LifeDashboard.Application/Services/GoalPurchaseService.cs#L58)); the subsequent TransactionService call resolves the same category again from `CategoryId` ([TransactionService.cs:102](../LifeDashboard.Application/Services/TransactionService.cs#L102)). This is a small extra query and duplicates category resolution, not transaction creation logic.

Recommendation: leave as-is unless this becomes a measurable cost; if optimized, keep ownership validation in the transaction path and avoid passing unchecked category state.

## Transaction Creation Flow

| Entry point | Creation path | Transaction and balance owner | Transaction lifecycle owner |
| --- | --- | --- | --- |
| `POST /api/Transaction` | `TransactionController` → `CreateTransactionAsync` | `TransactionService`; Transfer delegates to `TransferCore` | `TransactionService` |
| Goal deposit | `GoalDepositService` → `CreateTransactionWithinExistingTransactionAsync` → `TransferCore` | `TransactionService`/`TransferCore` | `GoalDepositService` |
| Goal purchase | `GoalPurchaseService` → `CreateTransactionWithinExistingTransactionAsync` | `TransactionService` and `IAccountDebitService` | `GoalPurchaseService` |

There is no full duplicate of expense/transfer creation in the Goal services. Non-transfer `Transaction` construction is centralized in `TransactionService` ([TransactionService.cs:139](../LifeDashboard.Application/Services/TransactionService.cs#L139)); transfer-specific construction and account movement are isolated in `TransferCore` ([TransferCore.cs:57](../LifeDashboard.Application/Services/TransferCore.cs#L57), [TransferCore.cs:66](../LifeDashboard.Application/Services/TransferCore.cs#L66)). This is a deliberate type-specific branch, although persistence is split: `TransferCore` adds transfer records while `TransactionService` adds non-transfer records.

The three entry points use the same transaction creation core and account debit rules. Their main difference is transaction ownership: the general API owns its unit of work, while each Goal use case wraps transaction creation and Goal state mutation in one unit of work. Goal-specific association is applied by the shared transaction core.

## Clean Architecture Assessment

- `GoalDepositService` and `GoalPurchaseService` depend on Application interfaces and Domain entities; neither references Infrastructure implementations or EF Core types.
- They act as application use-case coordinators: user-scoped Goal lookup, business validation, transaction orchestration, Goal state mutation, and unit-of-work lifecycle.
- Account balance mutation remains behind the transaction flow; Goal services do not update account balances directly.
- The primary architecture concern is feature coupling in `ITransactionService`, not an Application-to-Infrastructure dependency inversion failure.

## Review Summary

The Phase 2 transaction boundary is functionally coherent: Goal changes and their associated transactions share a caller-owned unit of work, while existing transaction API callers retain TransactionService-owned lifecycle behavior. Address the invalid enum acceptance as the highest-priority correctness concern. The Goal-aware method and unverified external-transaction convention are maintainability risks to contain as the application grows.
