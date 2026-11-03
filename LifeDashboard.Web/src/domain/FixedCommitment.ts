export const CommitmentType = {
  Rent: 1,
  Insurance: 2,
  Installment: 3,
  FamilySupport: 4,
} as const;

export type CommitmentType = typeof CommitmentType[keyof typeof CommitmentType];

export interface IFixedCommitment {
  id: string;
  name: string;
  monthlyAmount: number;
  commitmentType: CommitmentType;
  startMonth: string;
  endMonth: string | null;
}

export interface IFixedCommitmentPayload {
  name: string;
  monthlyAmount: number;
  commitmentType: CommitmentType;
  startMonth: string;
  endMonth: string | null;
}
