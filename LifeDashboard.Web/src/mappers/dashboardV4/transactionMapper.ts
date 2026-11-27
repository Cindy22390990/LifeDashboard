import type { HomeTransactionItemDto } from '../../api/dashboard-v4/dto/homeDto';
import type {
  TransactionItemDto,
  TransactionListResponseDto,
} from '../../api/dashboard-v4/dto/transactionDto';
import { mapDateOnly } from './dateOnlyMapper';
import type {
  V4TransactionItemViewModel,
  V4TransactionListViewModel,
  V4TransactionType,
} from './viewModels';

function mapTransactionType(value: string | null | undefined): V4TransactionType {
  switch (value?.trim().toLowerCase()) {
    case 'income':
      return 'income';
    case 'expense':
      return 'expense';
    case 'transfer':
      return 'transfer';
    default:
      return 'unknown';
  }
}

function getFallbackTitle(type: V4TransactionType): string {
  switch (type) {
    case 'income':
      return 'Income';
    case 'expense':
      return 'Expense';
    case 'transfer':
      return 'Transfer';
    default:
      return 'Transaction';
  }
}

export function mapV4Transaction(
  dto: TransactionItemDto,
): V4TransactionItemViewModel {
  const transactionType = mapTransactionType(dto.transactionType);
  const note = dto.note?.trim();

  return {
    id: dto.id,
    title: note || getFallbackTitle(transactionType),
    amount: dto.amount,
    transactionType,
    transactionDate: mapDateOnly(dto.transactionDate),
    accountId: dto.accountId ?? null,
    accountName: dto.accountName?.trim() || null,
    categoryId: dto.categoryId ?? null,
    categoryName: dto.categoryName?.trim() || null,
  };
}

export function mapV4HomeTransaction(
  dto: HomeTransactionItemDto,
): V4TransactionItemViewModel {
  const transactionType = mapTransactionType(dto.transactionType);
  const note = dto.note?.trim();

  return {
    id: dto.id,
    title: note || getFallbackTitle(transactionType),
    amount: dto.amount,
    transactionType,
    transactionDate: mapDateOnly(dto.transactionDate),
    accountId: null,
    accountName: null,
    categoryId: null,
    categoryName: null,
  };
}

export function mapV4TransactionList(
  dto: TransactionListResponseDto,
): V4TransactionListViewModel {
  return {
    totalCount: dto.totalCount,
    page: dto.page,
    pageSize: dto.pageSize,
    totalIncome: dto.totalIncome,
    totalExpense: dto.totalExpense,
    items: (dto.items ?? []).map(mapV4Transaction),
  };
}
