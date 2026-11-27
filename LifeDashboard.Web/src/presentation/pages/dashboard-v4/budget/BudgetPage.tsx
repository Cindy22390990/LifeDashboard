import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { MetricCard } from '../../../components/dashboard-v4/common/MetricCard';
import { ProgressBar } from '../../../components/dashboard-v4/common/ProgressBar';
import { FeatureComingSoon } from '../../../components/dashboard-v4/common/FeatureComingSoon';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';

export function BudgetPage() {
  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="檢視每月可用預算與各分類支出進度。"
        title="預算"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="budget-overview" title="每月預算概覽">
          <div className="metric-card-grid">
            <MetricCard label="預算額度" value="—" />
            <MetricCard label="剩餘預算" value="—" />
          </div>
          <ProgressBar label="預算使用進度" />
          <FeatureComingSoon
            description="月份切換、剩餘預算與固定支出預留資訊準備中。"
            title="預算摘要準備中"
          />
        </DashboardCard>
        <DashboardCard id="category-budgets" title="分類預算進度">
          <FeatureComingSoon
            description="分類額度、已用金額與剩餘金額介面準備中。"
            title="分類進度準備中"
          />
        </DashboardCard>
      </div>
    </main>
  );
}
