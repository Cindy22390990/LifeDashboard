import { GoalStatus, type IGoal } from '../../../domain/Goal';
import { getGoalCompletionPercentage } from '../../../domain/dashboard/goalPyramid';
import './GoalSummary.css';

interface GoalSummaryProps {
  goals: readonly IGoal[];
}

function formatAmount(amount: number): string {
  return `$ ${amount.toLocaleString('zh-TW', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

export default function GoalSummary({ goals }: GoalSummaryProps) {
  return (
    <section className="goal-summary dashboard-card" aria-labelledby="goal-summary-title">
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">GOALS</p>
          <h2 id="goal-summary-title">目標摘要</h2>
        </div>
      </div>

      {goals.length === 0 ? (
        <p className="goal-summary__empty">目前沒有目標</p>
      ) : (
        <ul className="goal-summary__list">
          {goals.map((goal) => {
            const percentage = Math.max(0, getGoalCompletionPercentage(goal));
            const progressValue = Math.min(100, percentage);

            return (
              <li className="goal-summary__item" key={goal.id}>
                <div className="goal-summary__heading">
                  <strong>{goal.name}</strong>
                  {goal.status === GoalStatus.Completed && (
                    <span className="goal-summary__status">已完成</span>
                  )}
                </div>
                <div className="goal-summary__amounts">
                  <span>{formatAmount(goal.currentAmount)}</span>
                  <span aria-hidden="true">/</span>
                  <span>{formatAmount(goal.targetAmount)}</span>
                  <strong>{percentage.toFixed(1)}%</strong>
                </div>
                <progress
                  className="goal-summary__progress"
                  aria-label={`${goal.name} 目標進度`}
                  max={100}
                  value={progressValue}
                />
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
