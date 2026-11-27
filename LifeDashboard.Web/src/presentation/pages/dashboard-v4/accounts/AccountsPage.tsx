import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { MetricCard } from '../../../components/dashboard-v4/common/MetricCard';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';
import { useAccountsReadQuery } from '../../../../hooks/dashboardV4/useAccountsReadQuery';

export function AccountsPage() {
  const accountsQuery = useAccountsReadQuery();
  const accounts = accountsQuery.data?.items ?? [];
  const totalAssets = accountsQuery.data?.totalAssets ?? 0;
  const getAccountsByType = (accountType: 'cashflow' | 'saving' | 'invest') =>
    accounts.filter((account) => account.accountType === accountType);
  const errorMessage =
    accountsQuery.error?.detail ||
    '無法載入帳戶資料，請稍後再試。';

  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="檢視資產概況，並依帳戶類型管理資金。"
        title="帳戶"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="assets-overview" title="總資產概覽">
          <div className="metric-card-grid">
            <MetricCard
              label="總資產"
              value={accountsQuery.isSuccess ? totalAssets.toLocaleString() : '—'}
            />
            <MetricCard
              label="帳戶數"
              value={accountsQuery.isSuccess ? String(accounts.length) : '—'}
            />
          </div>
          {accountsQuery.isPending && <p role="status">正在載入資產資料…</p>}
          {accountsQuery.isError && (
            <div role="alert">
              <p>{errorMessage}</p>
              <button
                type="button"
                onClick={() => void accountsQuery.refetch()}
                disabled={accountsQuery.isFetching}
              >
                重試
              </button>
            </div>
          )}
        </DashboardCard>
        <DashboardCard id="cashflow-accounts" title="生活帳戶">
          {renderAccountGroup(getAccountsByType('cashflow'), accountsQuery.isSuccess)}
        </DashboardCard>
        <DashboardCard id="saving-accounts" title="儲蓄帳戶">
          {renderAccountGroup(getAccountsByType('saving'), accountsQuery.isSuccess)}
        </DashboardCard>
        <DashboardCard id="investment-accounts" title="投資帳戶">
          {renderAccountGroup(getAccountsByType('invest'), accountsQuery.isSuccess)}
        </DashboardCard>
      </div>
    </main>
  );
}

function renderAccountGroup(
  accounts: Array<{ id: string; name: string; balance: number }>,
  loaded: boolean,
) {
  if (!loaded) {
    return null;
  }

  if (accounts.length === 0) {
    return <p role="status">目前沒有帳戶。</p>;
  }

  return (
    <ul
      style={{
        display: 'grid',
        gap: 12,
        listStyle: 'none',
        margin: 0,
        padding: 0,
      }}
    >
      {accounts.map((account) => (
        <li
          key={account.id}
          style={{
            borderBottom: '1px solid var(--border-subtle)',
            display: 'flex',
            justifyContent: 'space-between',
            paddingBottom: 12,
          }}
        >
          <strong>{account.name}</strong>
          <span>{account.balance.toLocaleString()}</span>
        </li>
      ))}
    </ul>
  );
}
