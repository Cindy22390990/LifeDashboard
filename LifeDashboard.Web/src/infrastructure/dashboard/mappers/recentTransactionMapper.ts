import type { RecentTransactionItemViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import {
  TransactionType as BackendTransactionType,
} from '../../../domain/Transaction';
import {
  TransactionType as DashboardTransactionType,
} from '../../../application/dashboard/models/TransactionType';
import type { RecentTransactionDto } from '../api/RecentTransactionDto';

function mapTransactionType(type: RecentTransactionDto['type']): DashboardTransactionType {
  if (typeof type === 'string') {
    switch (type.trim().toLowerCase()) {
      case 'income':
        return DashboardTransactionType.Income;
      case 'expense':
        return DashboardTransactionType.Expense;
      case 'transfer':
        return DashboardTransactionType.Transfer;
      default:
        return DashboardTransactionType.Unknown;
    }
  }

  switch (type) {
    case BackendTransactionType.Expense:
      return DashboardTransactionType.Expense;
    case BackendTransactionType.Income:
      return DashboardTransactionType.Income;
    case BackendTransactionType.Transfer:
      return DashboardTransactionType.Transfer;
    default:
      return DashboardTransactionType.Unknown;
  }
}

export function mapRecentTransaction(
  transaction: RecentTransactionDto,
): RecentTransactionItemViewModel {
  const transactionType = mapTransactionType(transaction.type);
  const categoryKey =
    transactionType === 'income'
      ? 'transactions.types.income'
      : transactionType === 'transfer'
        ? 'transactions.types.transfer'
        : transactionType === 'expense'
          ? 'transactions.types.expense'
          : transaction.categoryName;

  return {
    id: transaction.id,
    title: transaction.note?.trim() || transaction.categoryName || transaction.accountName,
    subtitle: transaction.note?.trim() || null,
    categoryKey,
    accountName: transaction.accountName,
    transactionType,
    amount: transaction.amount,
    occurredAt: transaction.transactionDate,
    icon: transactionType === 'income'
      ? '↗'
      : transactionType === 'transfer'
        ? '⇄'
        : transactionType === 'expense'
          ? '↓'
          : '•',
  };
}
