import type { RadarStateDto } from '../../../domain/budgetRadar';
import './BudgetRadarSummary.css';

interface BudgetRadarSummaryProps {
  radarState: RadarStateDto;
}

const mockRadarVisual = {
  progressPercent: 68,
  status: 'Normal',
} as const;

export default function BudgetRadarSummary({ radarState }: BudgetRadarSummaryProps) {
  return (
    <section
      className="budget-radar-summary dashboard-card"
      aria-labelledby="budget-radar-summary-title"
    >
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">BUDGET RADAR</p>
          <h2 id="budget-radar-summary-title">預算摘要</h2>
        </div>
        <span className="budget-radar-summary__status">
          {mockRadarVisual.status}
          <small>示意</small>
        </span>
      </div>

      <div className="budget-radar-summary__content">
        <div
          aria-hidden="true"
          className="budget-radar-summary__gauge"
          style={{
            background: `conic-gradient(var(--color-success) 0 ${mockRadarVisual.progressPercent}%, var(--color-border-subtle) ${mockRadarVisual.progressPercent}% 100%)`,
          }}
        >
          <span>Budget<br />Radar</span>
        </div>
        <div className="budget-radar-summary__remaining">
          <span>本期剩餘生活預算</span>
          <strong>{radarState.remainingLivingBudget.toLocaleString('zh-TW', {
            style: 'currency',
            currency: 'TWD',
            maximumFractionDigits: 0,
          })}</strong>
          <span className="budget-radar-summary__period">Payday 所屬預算週期</span>
        </div>
      </div>
      <p className="budget-radar-summary__note">狀態與圖表為 UI 示意資料。</p>
    </section>
  );
}
