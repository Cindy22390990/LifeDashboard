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
        <dl
          className={`budget-radar-primary${radarState.todayRemaining < 0 ? ' budget-radar-primary--warning' : ' budget-radar-primary--safe'}`}
        >
          <dt>
            今日剩餘安全額度
            <span
              className="budget-radar-state"
              aria-hidden={radarState.todayRemaining < 0}
            >
              {radarState.todayRemaining < 0 ? '超支狀態' : '正常狀態'}
            </span>
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
        </dl>
      </div>

      {radarState.todayRemaining < 0 && (
        <p className="budget-radar-warning" role="status">
          今日已超過安全額度
        </p>
      )}

      {radarState.uncategorizedExpenseAmount > 0 && (
        <p className="budget-radar-uncategorized" role="status">
          本期未分類支出 {formatAmount(radarState.uncategorizedExpenseAmount)} 尚未計入今日預算，請完成分類後再確認。
        </p>
      )}
    </section>
  );
}
