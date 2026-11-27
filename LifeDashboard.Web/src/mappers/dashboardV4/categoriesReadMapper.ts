import type { CategoryReadItemDto } from '../../api/dashboard-v4/dto/categoriesReadDto';
import type { V4CategoryReadViewModel } from './viewModels';

const bucketTypeNames: Record<number, string> = {
  1: 'Living',
  2: 'Entertainment',
  3: 'Savings',
  4: 'Investment',
  5: 'Responsibility',
};

export function mapV4CategoriesRead(
  dto: CategoryReadItemDto[],
): V4CategoryReadViewModel[] {
  return (dto ?? []).map((category) => ({
    id: category.id,
    name: category.name?.trim() || 'Unnamed Category',
    icon: category.icon?.trim() || '•',
    bucketType: category.bucketType,
    bucketTypeName: bucketTypeNames[category.bucketType] ?? 'Unknown',
  }));
}
