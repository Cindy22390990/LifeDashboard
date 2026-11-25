import type { RecentTransactionItemViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import {
  TransactionType as DashboardTransactionType,
} from '../../../application/dashboard/models/TransactionType';
import type { DayTransactionItemDto } from '../api/DashboardCalendarDayResponse';
import type { RecentTransactionDto } from '../api/RecentTransactionDto';

type TransactionDto = RecentTransactionDto | DayTransactionItemDto;

function mapTransactionType(type: string): DashboardTransactionType {
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

export function mapRecentTransaction(
  transaction: TransactionDto,
): RecentTransactionItemViewModel {
  const note = transaction.note ?? '';
  const trimmedNote = note.trim();
  const transactionType = mapTransactionType(
    'transactionType' in transaction
      ? transaction.transactionType
      : transaction.type,
  );
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
    title: trimmedNote,
    subtitle: trimmedNote || null,
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
