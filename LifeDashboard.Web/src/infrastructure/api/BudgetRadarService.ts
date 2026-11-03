import type { RadarStateDto } from '../../domain/budgetRadar';
import http from './http';

export const BudgetRadarService = {
  getRadar: (currentDate: string): Promise<RadarStateDto> =>
    http.get<RadarStateDto>('/budget/radar', {
      params: { currentDate },
    }),
};
