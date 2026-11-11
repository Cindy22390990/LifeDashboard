import type { IAccount } from './Account';
import type { IGoal } from './Goal';
import type { RadarStateDto } from './budgetRadar';

export interface DashboardSummaryDto {
  budget: RadarStateDto;
  accounts: IAccount[];
  goals: IGoal[];
}