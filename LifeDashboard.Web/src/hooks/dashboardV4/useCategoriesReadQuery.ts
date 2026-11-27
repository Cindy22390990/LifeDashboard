import { useQuery } from '@tanstack/react-query';
import type { ApiError } from '../../api/dashboardV4/apiError';
import { dashboardV4Client } from '../../api/dashboardV4/dashboardV4Client';
import type { CategoryReadItemDto } from '../../api/dashboard-v4/dto/categoriesReadDto';
import { dashboardV4Keys } from '../../api/queryKeys';
import {
  mapV4CategoriesRead,
  type V4CategoryReadViewModel,
} from '../../mappers/dashboardV4';

export function useCategoriesReadQuery() {
  return useQuery<CategoryReadItemDto[], ApiError, V4CategoryReadViewModel[]>({
    queryKey: dashboardV4Keys.categories.all(),
    queryFn: () => dashboardV4Client.getCategories(),
    select: mapV4CategoriesRead,
  });
}
