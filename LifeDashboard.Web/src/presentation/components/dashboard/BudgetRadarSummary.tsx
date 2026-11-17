import type { RadarStateDto } from '../../../domain/budgetRadar';
import './BudgetRadarSummary.css';

interface BudgetRadarSummaryProps {
  radarState: RadarStateDto;
}

const amountFormatter = new Intl.NumberFormat('zh-TW', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatAmount(amount: number): string {
  return amountFormatter.format(amount);
}

export default function BudgetRadarSummary({ radarState }: BudgetRadarSummaryProps) {
  return (
    <section className="budget-radar-summary dashboard-card" aria-labelledby="budget-radar-summary-title">
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">BUDGET RADAR</p>
          <h2 id="budget-radar-summary-title">預算摘要</h2>
        </div>
      </div>

      <p className="budget-radar-summary__period">Payday 所屬預算週期</p>
      <div className="budget-radar-summary__remaining">
        <span>本期剩餘生活預算</span>
        <strong>{formatAmount(radarState.remainingLivingBudget)}</strong>
      </div>
    </section>
  );
}
