import type { RadarStateDto } from '../../domain/budgetRadar';
import './BudgetRadarPanel.css';

interface BudgetRadarPanelProps {
  radarState: RadarStateDto;
}

const numberFormatter = new Intl.NumberFormat('zh-TW', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export default function BudgetRadarPanel({ radarState }: BudgetRadarPanelProps) {
  const formatAmount = (amount: number) => numberFormatter.format(amount);

  return (
    <section className="budget-radar-panel" aria-label="預算雷達">
      <dl className="budget-radar-values">
        <div>
          <dt>DailySafeLimit</dt>
          <dd>{formatAmount(radarState.dailySafeLimit)}</dd>
        </div>
        <div>
          <dt>TodaySpent</dt>
          <dd>{formatAmount(radarState.todaySpent)}</dd>
        </div>
        <div>
          <dt>TodayRemaining</dt>
          <dd>{formatAmount(radarState.todayRemaining)}</dd>
        </div>
        <div>
          <dt>RemainingDays</dt>
          <dd>{radarState.remainingDays}</dd>
        </div>
        <div>
          <dt>TotalLivingBudget</dt>
          <dd>{formatAmount(radarState.totalLivingBudget)}</dd>
        </div>
      </dl>

      {radarState.todayRemaining < 0 && (
        <p className="budget-radar-warning" role="status">
          今日已超過安全額度
        </p>
      )}

      {radarState.uncategorizedExpenseAmount > 0 && (
        <p className="budget-radar-uncategorized" role="status">
          有未分類支出，請補充分類（{formatAmount(radarState.uncategorizedExpenseAmount)}）
        </p>
      )}
    </section>
  );
}
