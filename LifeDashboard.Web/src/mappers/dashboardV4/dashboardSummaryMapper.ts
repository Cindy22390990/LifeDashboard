import type { DashboardSummaryDto } from '../../api/dashboard-v4/dto/dashboardSummaryDto';
import type { V4DashboardSummaryViewModel } from './viewModels';

export function mapV4DashboardSummary(
  dto: DashboardSummaryDto,
): V4DashboardSummaryViewModel {
  return {
    totalAssets: dto.totalAssets,
    monthlyIncome: dto.monthlyIncome,
    monthlyExpense: dto.monthlyExpense,
  };
}
