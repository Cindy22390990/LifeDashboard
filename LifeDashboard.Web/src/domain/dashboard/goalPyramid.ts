import type { GoalTier, IGoal } from '../Goal';

export interface GoalTierGroup {
  tier: GoalTier;
  label: string;
  goals: IGoal[];
}

const goalTiers: readonly { tier: GoalTier; label: string }[] = [
  { tier: 1, label: 'Tier 1｜安全預備金' },
  { tier: 2, label: 'Tier 2｜中期負擔 / 大額目標' },
  { tier: 3, label: 'Tier 3｜夢想目標' },
];

export function groupGoalsByTier(goals: readonly IGoal[]): GoalTierGroup[] {
  return goalTiers.map(({ tier, label }) => ({
    tier,
    label,
    goals: goals.filter((goal) => goal.tier === tier),
  }));
}

export function getGoalCompletionPercentage(
  goal: Pick<IGoal, 'currentAmount' | 'targetAmount'>,
): number {
  if (goal.targetAmount <= 0) {
    return 0;
  }

  return (goal.currentAmount / goal.targetAmount) * 100;
}