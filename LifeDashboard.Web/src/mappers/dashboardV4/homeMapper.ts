import type { DashboardV4HomeDto } from '../../api/dashboard-v4/dto/homeDto';
import { mapDateOnly } from './dateOnlyMapper';
import { mapV4HomeTransaction } from './transactionMapper';
import type { V4HomeViewModel } from './viewModels';

export function mapV4Home(dto: DashboardV4HomeDto): V4HomeViewModel {
  return {
    businessDate: mapDateOnly(dto.businessDate),
    dailySpendingLimit: dto.dailySpendingLimit,
    dailySpent: dto.dailySpent,
    dailyRemaining: dto.dailyRemaining,
    monthlyProgress: {
      saved: dto.monthlyProgress?.saved ?? 0,
      invested: dto.monthlyProgress?.invested ?? 0,
    },
    todayTransactions: (dto.todayTransactions ?? []).map(mapV4HomeTransaction),
  };
}
