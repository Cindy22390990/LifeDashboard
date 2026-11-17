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
      <div className="budget-radar-values">
        <dl className="budget-radar-primary">
          <dt>
            今日剩餘安全額度
          </dt>
          <dd>{formatAmount(radarState.todayRemaining)}</dd>
        </dl>
        <dl className="budget-radar-supporting">
          <div>
            <dt>今日已支出</dt>
            <dd>{formatAmount(radarState.todaySpent)}</dd>
          </div>
          <div>
            <dt>每日安全額度</dt>
            <dd>{formatAmount(radarState.dailySafeLimit)}</dd>
          </div>
          <div>
            <dt>剩餘天數</dt>
            <dd>{radarState.remainingDays}</dd>
          </div>
          <div>
            <dt>生活預算</dt>
            <dd>{formatAmount(radarState.totalLivingBudget)}</dd>
          </div>
          <div>
            <dt>本期剩餘生活預算</dt>
            <dd>{formatAmount(radarState.remainingLivingBudget)}</dd>
          </div>
          <div>
            <dt>未分類支出</dt>
            <dd>{formatAmount(radarState.uncategorizedExpenseAmount)}</dd>
          </div>
        </dl>
      </div>

    </section>
  );
}
