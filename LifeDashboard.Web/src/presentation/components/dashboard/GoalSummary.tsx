import { mockGoalFundingSummary } from './dashboardPreviewData';
import './GoalSummary.css';

function formatAmount(amount: number): string {
  return `$ ${amount.toLocaleString('zh-TW', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

export default function GoalSummary() {
  return (
    <section className="goal-summary dashboard-card" aria-labelledby="goal-summary-title">
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">GOALS</p>
          <h2 id="goal-summary-title">目標進度</h2>
        </div>
        <span className="goal-summary__mock-label">UI 示意</span>
      </div>

      <p className="goal-summary__note">
        GoalFundingSummary API 尚未提供；以下為預覽資料。
      </p>
      <ul className="goal-summary__list">
        {mockGoalFundingSummary.map((goal) => (
          <li className="goal-summary__item" key={goal.id}>
            <div className="goal-summary__heading">
              <strong>{goal.name}</strong>
            </div>
            <div className="goal-summary__amounts">
              <span>{formatAmount(goal.fundedAmount)}</span>
              <span aria-hidden="true">/</span>
              <span>{formatAmount(goal.targetAmount)}</span>
              <strong>{goal.progressPercent}%</strong>
            </div>
            <progress
              className="goal-summary__progress"
              aria-label={`${goal.name} 目標進度（示意）`}
              max={100}
              value={goal.progressPercent}
            />
          </li>
        ))}
      </ul>
    </section>
  );
}
