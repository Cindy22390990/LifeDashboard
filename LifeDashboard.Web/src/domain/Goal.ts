export const GoalTier = {
  Tier1_Safety: 1,
  Tier2_Burden: 2,
  Tier3_Dream: 3,
} as const;

export type GoalTier = typeof GoalTier[keyof typeof GoalTier];

export const GoalType = {
  Saving: 1,
  Purchase: 2,
} as const;

export type GoalType = typeof GoalType[keyof typeof GoalType];

export const GoalStatus = {
  Active: 1,
  Completed: 2,
} as const;

export type GoalStatus = typeof GoalStatus[keyof typeof GoalStatus];

export interface IGoal {
  id: number;
  name: string;
  targetAmount: number;
  currentAmount: number;
  tier: GoalTier;
  goalType: GoalType;
  status: GoalStatus;
  note: string;
  completedDate: string | null;
}

export interface IGoalPayload {
  name: string;
  targetAmount: number;
  tier: GoalTier;
  goalType: GoalType;
  note: string;
}

export interface IGoalDepositRequest {
  amount: number;
  fromAccountId: string;
  toAccountId: string;
  note: string;
}
