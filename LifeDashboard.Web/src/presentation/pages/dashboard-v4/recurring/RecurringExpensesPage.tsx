import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { MetricCard } from '../../../components/dashboard-v4/common/MetricCard';
import { FeatureComingSoon } from '../../../components/dashboard-v4/common/FeatureComingSoon';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';

export function RecurringExpensesPage() {
  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="整理每月固定帳單、繳款狀態與到期資訊。"
        title="固定支出"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="recurring-summary" title="本月固定支出">
          <div className="metric-card-grid">
            <MetricCard label="本月總額" value="—" />
            <MetricCard label="已繳" value="—" />
            <MetricCard label="待繳" value="—" />
          </div>
          <FeatureComingSoon
            description="固定支出總額、已繳與待扣款摘要準備中。"
            title="本月摘要準備中"
          />
        </DashboardCard>
        <DashboardCard id="recurring-list" title="每月固定清單">
          <FeatureComingSoon
            description="固定支出項目、到期日與繳款狀態介面準備中。"
            title="固定支出清單準備中"
          />
        </DashboardCard>
      </div>
    </main>
  );
}
