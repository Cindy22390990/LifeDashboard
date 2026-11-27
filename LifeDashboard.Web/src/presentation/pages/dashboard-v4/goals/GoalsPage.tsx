import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { MetricCard } from '../../../components/dashboard-v4/common/MetricCard';
import { ProgressBar } from '../../../components/dashboard-v4/common/ProgressBar';
import { FeatureComingSoon } from '../../../components/dashboard-v4/common/FeatureComingSoon';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';

export function GoalsPage() {
  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="追蹤目標進度與預計完成時間。"
        title="目標"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="goals-overview" title="目標總覽">
          <div className="metric-card-grid">
            <MetricCard label="進行中目標" value="—" />
            <MetricCard label="已完成目標" value="—" />
          </div>
          <ProgressBar label="整體目標進度" />
          <FeatureComingSoon
            description="進行中目標與整體達成摘要準備中。"
            title="目標摘要準備中"
          />
        </DashboardCard>
        <DashboardCard id="goals-list" title="目標清單">
          <FeatureComingSoon
            description="目標卡片與進度檢視介面準備中。"
            title="目標清單準備中"
          />
        </DashboardCard>
      </div>
    </main>
  );
}
