import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { FilterChip } from '../../../components/dashboard-v4/common/FilterChip';
import { FeatureComingSoon } from '../../../components/dashboard-v4/common/FeatureComingSoon';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';
import { useCategoriesReadQuery } from '../../../../hooks/dashboardV4/useCategoriesReadQuery';

export function CategoriesPage() {
  const categoriesQuery = useCategoriesReadQuery();
  const categories = categoriesQuery.data ?? [];
  const errorMessage =
    categoriesQuery.error?.detail ||
    '無法載入分類資料，請稍後再試。';

  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="管理支出與收入分類，整理常用記帳項目。"
        title="分類"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="category-types" title="分類類型">
          <div aria-label="分類類型" className="filter-chip-list">
            <FilterChip label="支出分類" selected />
            <FilterChip label="收入分類" />
          </div>
          <FeatureComingSoon
            description="支出分類與收入分類切換介面準備中。"
            title="分類切換準備中"
          />
        </DashboardCard>
        <DashboardCard id="category-list" title="分類清單">
          {categoriesQuery.isPending && (
            <p role="status">正在載入分類資料…</p>
          )}
          {categoriesQuery.isError && (
            <div role="alert">
              <p>{errorMessage}</p>
              <button
                type="button"
                onClick={() => void categoriesQuery.refetch()}
                disabled={categoriesQuery.isFetching}
              >
                重試
              </button>
            </div>
          )}
          {categoriesQuery.isSuccess && categories.length === 0 && (
            <p role="status">目前沒有分類。</p>
          )}
          {categories.length > 0 && (
            <ul
              style={{
                display: 'grid',
                gap: 12,
                listStyle: 'none',
                margin: 0,
                padding: 0,
              }}
            >
              {categories.map((category) => (
                <li
                  key={category.id}
                  style={{
                    borderBottom: '1px solid var(--border-subtle)',
                    display: 'flex',
                    justifyContent: 'space-between',
                    paddingBottom: 12,
                  }}
                >
                  <span>
                    {category.icon} {category.name}
                  </span>
                  <span>{category.bucketTypeName}</span>
                </li>
              ))}
            </ul>
          )}
        </DashboardCard>
      </div>
    </main>
  );
}
