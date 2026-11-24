import type {
  AccountSummaryViewModel,
  DashboardViewModel,
} from '../../../application/dashboard/models/DashboardViewModel';
import type { SectionState } from '../../../application/dashboard/models/SectionState';
import {
  AccountType as BackendAccountType,
} from '../../../domain/Account';
import {
  AccountType as DashboardAccountType,
} from '../../../application/dashboard/models/AccountType';
import type { AccountSummaryDto } from '../api/AccountSummaryDto';
import type { DashboardSummaryResponse } from '../api/DashboardV3SummaryResponse';
import { mapRecentTransaction } from './recentTransactionMapper';

function mapAccountType(type: AccountSummaryDto['type']): DashboardAccountType {
  if (typeof type === 'number') {
    switch (type) {
      case BackendAccountType.Invest:
        return DashboardAccountType.Investment;
      case BackendAccountType.Cashflow:
        return DashboardAccountType.Cash;
      case BackendAccountType.Saving:
        return DashboardAccountType.Bank;
      default:
        return DashboardAccountType.Unknown;
    }
  }

  switch (type.toLowerCase()) {
    case 'bank':
    case 'saving':
      return DashboardAccountType.Bank;
    case 'cash':
    case 'cashflow':
      return DashboardAccountType.Cash;
    case 'creditcard':
      return DashboardAccountType.CreditCard;
    case 'investment':
    case 'invest':
      return DashboardAccountType.Investment;
    default:
      return DashboardAccountType.Unknown;
  }
}

function mapAccount(account: AccountSummaryDto): AccountSummaryViewModel {
  return {
    accountId: account.id,
    name: account.name,
    balance: account.balance,
    accountType: mapAccountType(account.type),
  };
}

function toSectionState<T>(items: T[]): SectionState<T[]> {
  return items.length > 0
    ? { status: 'ready' as const, data: items }
    : { status: 'empty' as const };
}

export function mapDashboardV3Api(
  summary: DashboardSummaryResponse,
): DashboardViewModel {
  const recentTransactions = (summary.recentTransactions ?? []).map(mapRecentTransaction);
  const accountSummary = (summary.accounts ?? []).map(mapAccount);

  return {
    meta: {
      businessDate: summary.businessDate,
    },
    referenceDate: summary.businessDate,
    heroStats: {
      totalAssets: summary.totalAssets,
      spentToday: summary.todayExpense,
      remainingBudget: null,
      monthlyIncome: null,
      monthlyExpense: null,
    },
    budgetRadar: { status: 'pending' },
    goalProgress: { status: 'pending' },
    accountSummary: toSectionState(accountSummary),
    recentTransactions: toSectionState(recentTransactions),
  };
}
