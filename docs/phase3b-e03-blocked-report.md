# E-03 Blocked Report

## Test

Transaction Delete / Restore Balance

## Status

BLOCKED

No DELETE API was called, and no database changes were made.

## Precondition

PASS

`phase3b_test`:

- Account balance: 9000
- Transaction count: 1
- TransactionId: `901b93c5-66c8-442b-8bcb-95448ba451ab`

## Blocking Issue

Expected:

- Transaction soft delete
- Historical transaction preserved
- Balance restored

Current implementation:

- Transaction entity has no `IsDeleted` field
- `TransactionRepository.Delete` uses EF `Remove()`
- `DeleteTransactionAsync` performs physical deletion

## Risk

Executing DELETE now would:

1. Permanently remove the E-01 transaction
2. Validate hard delete instead of soft delete
3. Hide missing domain capability required for future history/report features

## Required Decision

Choose one:

**Option A:** Change E-03 specification to “Validate hard delete behavior and balance restoration”.

**Option B:** Implement transaction soft delete:

- Add `IsDeleted` field
- Update entity configuration
- Update repository query filtering
- Update delete behavior
- Add migration
- Rerun E-03
