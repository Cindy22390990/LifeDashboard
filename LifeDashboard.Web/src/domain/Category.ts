export const BucketType = {
  Living: 1,
  Entertainment: 2,
  Savings: 3,
  Investment: 4,
  Responsibility: 5,
} as const;

export type BucketType = typeof BucketType[keyof typeof BucketType];

export interface ICategory {
  id: string;
  name: string;
  icon: string;
  bucketType: BucketType;
  createdAt: string;
}

export interface ICategoryPayload {
  name: string;
  icon: string;
  bucketType: BucketType;
}
