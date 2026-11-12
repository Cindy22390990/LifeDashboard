import {
  GoalStatus,
  GoalTier as GoalTierValue,
  GoalType,
  type IGoal,
} from '../../../domain/Goal';
import {
  getGoalCompletionPercentage,
  groupGoalsByTier,
  type GoalTierGroup,
} from '../../../domain/dashboard/goalPyramid';
import './GoalPyramid.css';

interface GoalPyramidProps {
  goals: readonly IGoal[];
}

function formatAmount(amount: number): string {
  return `$ ${amount.toLocaleString('zh-TW', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

function getStatusLabel(goal: IGoal): string {
  if (goal.status === GoalStatus.Completed) {
    return '已完成';
  }

  return goal.currentAmount <= 0 ? '尚未開始' : '進行中';
}

function GoalTier({ group }: { group: GoalTierGroup }) {
  const tierDetails = {
    [GoalTierValue.Tier1_Safety]: { title: 'Safety Foundation' },
    [GoalTierValue.Tier2_Burden]: { title: 'Growth' },
    [GoalTierValue.Tier3_Dream]: { title: 'Dream' },
  }[group.tier];

  return (
    <section
      className="goal-pyramid__tier"
      data-tier={group.tier}
      aria-labelledby={`goal-pyramid-tier-${group.tier}`}
    >
      <header className="goal-pyramid__tier-header">
        <div>
          <p className="goal-pyramid__tier-label">{group.label}</p>
          <h3 id={`goal-pyramid-tier-${group.tier}`}>{tierDetails.title}</h3>
        </div>
        <span className="goal-pyramid__tier-count">{group.goals.length} 個目標</span>
      </header>
      {group.goals.length === 0 ? (
        <p className="goal-pyramid__empty-tier">此層目前沒有目標。</p>
      ) : (
        <div className="goal-pyramid__goals">
          {group.goals.map((goal) => <GoalCard key={goal.id} goal={goal} />)}
        </div>
      )}
    </section>
  );
}

function GoalCard({ goal }: { goal: IGoal }) {
  const isCompleted = goal.status === GoalStatus.Completed;
  const goalTypeLabel = goal.goalType === GoalType.Saving ? '儲蓄目標' : '購買目標';

  return (
    <article className="goal-pyramid__goal">
      <div className="goal-pyramid__goal-header">
        <div className="goal-pyramid__goal-identity">
          <h4>{goal.name}</h4>
          <span className="goal-pyramid__goal-type">{goalTypeLabel}</span>
        </div>
        <span
          className={`goal-pyramid__status${isCompleted ? ' goal-pyramid__status--completed' : ''}`}
        >
          {getStatusLabel(goal)}
        </span>
      </div>
      <div className="goal-pyramid__amounts">
        <div className="goal-pyramid__current-amount">
          <span>已累積</span>
          <strong>{formatAmount(goal.currentAmount)}</strong>
        </div>
        <span className="goal-pyramid__amount-divider" aria-hidden="true">/</span>
        <div className="goal-pyramid__target-amount">
          <span>目標金額</span>
          <strong>{formatAmount(goal.targetAmount)}</strong>
        </div>
      </div>
      <ProgressIndicator goal={goal} />
    </article>
  );
}

function ProgressIndicator({ goal }: { goal: IGoal }) {
  const percentage = Math.max(0, getGoalCompletionPercentage(goal));
  const progressValue = Math.min(100, percentage);
  const isOverflow = percentage > 100;

  if (goal.status === GoalStatus.Completed) {
    return (
      <div className="goal-pyramid__completion">
        <span className="goal-pyramid__completion-mark" aria-hidden="true">✓</span>
        <span>目標已完成</span>
        {isOverflow && (
          <span className="goal-pyramid__overflow">
            超出 {formatAmount(goal.currentAmount - goal.targetAmount)}
          </span>
        )}
      </div>
    );
  }

  return (
    <div className="goal-pyramid__progress-indicator">
      <div className="goal-pyramid__progress-meta">
        <span>目標進度</span>
        <span className="goal-pyramid__percentage">{percentage.toFixed(1)}%</span>
      </div>
      <progress
        className="goal-pyramid__progress"
        aria-label={`${goal.name} 目標進度`}
        max={100}
        value={progressValue}
      />
      {isOverflow && (
        <span className="goal-pyramid__overflow">
          超出 {formatAmount(goal.currentAmount - goal.targetAmount)}
        </span>
      )}
    </div>
  );
}

export default function GoalPyramid({ goals }: GoalPyramidProps) {
  if (goals.length === 0) {
    return (
      <section className="goal-pyramid" aria-labelledby="goal-pyramid-title">
        <h2 id="goal-pyramid-title">目標金字塔</h2>
        <p>目前沒有目標</p>
      </section>
    );
  }

  const tierGroups = groupGoalsByTier(goals);

  return (
    <section className="goal-pyramid" aria-labelledby="goal-pyramid-title">
      <h2 id="goal-pyramid-title">目標金字塔</h2>
      <div className="goal-pyramid__tiers" aria-label="由安全基礎至夢想目標">
        {tierGroups.map((group) => (
          <GoalTier key={group.tier} group={group} />
        ))}
      </div>
    </section>
  );
}