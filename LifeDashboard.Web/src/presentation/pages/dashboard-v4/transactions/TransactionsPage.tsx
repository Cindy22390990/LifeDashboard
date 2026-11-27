import { useInfiniteQuery } from '@tanstack/react-query';
import { ApiError } from '../../../../api/dashboardV4/apiError';
import { dashboardV4Client } from '../../../../api/dashboardV4/dashboardV4Client';
import { dashboardV4Keys } from '../../../../api/queryKeys';
import { mapV4Transaction } from '../../../../mappers/dashboardV4';
import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { FilterChip } from '../../../components/dashboard-v4/common/FilterChip';
import { MetricCard } from '../../../components/dashboard-v4/common/MetricCard';
import { FeatureComingSoon } from '../../../components/dashboard-v4/common/FeatureComingSoon';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';

const TRANSACTION_PAGE_SIZE = 20;
const transactionFilter = {
  pageSize: TRANSACTION_PAGE_SIZE,
};

export function TransactionsPage() {
  const transactionsQuery = useInfiniteQuery({
    queryKey: dashboardV4Keys.transactions.list(transactionFilter),
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      dashboardV4Client.getTransactions({
        ...transactionFilter,
        page: pageParam,
      }),
    getNextPageParam: (lastPage, pages) =>
      lastPage.items.length === TRANSACTION_PAGE_SIZE
        ? pages.length + 1
        : undefined,
    select: (result) => ({
      ...result,
      pages: result.pages.map((page) => page.items.map(mapV4Transaction)),
    }),
  });
  const transactions = transactionsQuery.data?.pages.flat() ?? [];
  const transactionsByDate = transactions.reduce(
    (groups, transaction) => {
      const date = transaction.transactionDate.isoDate;
      const group = groups.get(date);

      if (group) {
        group.push(transaction);
      } else {
        groups.set(date, [transaction]);
      }

      return groups;
    },
    new Map<string, (typeof transactions)[number][]>(),
  );
  const errorMessage =
    transactionsQuery.error instanceof ApiError
      ? transactionsQuery.error.detail
      : '無法載入交易紀錄，請稍後再試。';

  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="搜尋、篩選並瀏覽交易紀錄。"
        title="交易明細"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="transaction-filters" title="搜尋與篩選">
          <div aria-label="交易類型篩選" className="filter-chip-list">
            <FilterChip label="全部" selected />
            <FilterChip label="支出" />
            <FilterChip label="收入" />
          </div>
          <FeatureComingSoon
            description="交易搜尋、日期區間與帳戶篩選介面將於後續階段完成。"
            title="篩選介面準備中"
          />
        </DashboardCard>
        <DashboardCard id="transaction-summary" title="收支摘要">
          <div className="metric-card-grid">
            <MetricCard label="收入" value="—" />
            <MetricCard label="支出" value="—" />
            <MetricCard label="淨收支" value="—" />
          </div>
          <FeatureComingSoon
            description="收入、支出與淨收支摘要目前尚未提供。"
            title="摘要區塊準備中"
          />
        </DashboardCard>
        <DashboardCard id="transaction-history" title="交易紀錄">
          {transactionsQuery.isPending && (
            <p aria-live="polite" role="status">
              正在載入交易紀錄…
            </p>
          )}
          {transactionsQuery.isError && !transactionsQuery.data && (
            <div role="alert">
              <p>{errorMessage}</p>
              <button
                disabled={transactionsQuery.isFetching}
                onClick={() => void transactionsQuery.refetch()}
                type="button"
              >
                重試
              </button>
            </div>
          )}
          {transactionsQuery.data && transactions.length === 0 && (
            <p role="status">目前沒有符合條件的交易紀錄。</p>
          )}
          {transactions.length > 0 && (
            <>
              {Array.from(transactionsByDate, ([date, dateTransactions]) => (
                <section aria-label={`交易日期 ${date}`} key={date}>
                  <h3>{date}</h3>
                  <ul
                    style={{
                      display: 'grid',
                      gap: 12,
                      listStyle: 'none',
                      margin: 0,
                      padding: 0,
                    }}
                  >
                    {dateTransactions.map((transaction) => (
                      <li
                        key={transaction.id}
                        style={{
                          alignItems: 'center',
                          borderBottom: '1px solid var(--border-subtle)',
                          display: 'flex',
                          justifyContent: 'space-between',
                          paddingBottom: 12,
                        }}
                      >
                        <div>
                          <p>{transaction.title}</p>
                          <p>
                            {[transaction.accountName, transaction.categoryName]
                              .filter(Boolean)
                              .join(' · ')}
                          </p>
                        </div>
                        <p>
                          {transaction.transactionType === 'income'
                            ? '收入'
                            : transaction.transactionType === 'expense'
                              ? '支出'
                              : transaction.transactionType === 'transfer'
                                ? '轉帳'
                                : '交易'}
                          {' · '}
                          {transaction.amount.toLocaleString()}
                        </p>
                      </li>
                    ))}
                  </ul>
                </section>
              ))}
              {transactionsQuery.isError && (
                <div role="alert">
                  <p>{errorMessage}</p>
                  <button
                    disabled={transactionsQuery.isFetchingNextPage}
                    onClick={() => void transactionsQuery.fetchNextPage()}
                    type="button"
                  >
                    重試載入
                  </button>
                </div>
              )}
              {transactionsQuery.hasNextPage &&
                !transactionsQuery.isError && (
                  <button
                    disabled={transactionsQuery.isFetchingNextPage}
                    onClick={() => void transactionsQuery.fetchNextPage()}
                    type="button"
                  >
                    {transactionsQuery.isFetchingNextPage
                      ? '載入中…'
                      : '載入更多'}
                  </button>
                )}
            </>
          )}
        </DashboardCard>
      </div>
    </main>
  );
}
