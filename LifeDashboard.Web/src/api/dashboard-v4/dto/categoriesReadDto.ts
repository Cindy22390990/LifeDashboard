export type CategoryReadBucketType =
  | 1
  | 2
  | 3
  | 4
  | 5;

export interface CategoryReadItemDto {
  id: string;
  name: string;
  icon: string;
  bucketType: CategoryReadBucketType;
  createdAt: string;
}
