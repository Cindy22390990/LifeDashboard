import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { FilterChip } from '../../../components/dashboard-v4/common/FilterChip';
import { MetricCard } from '../../../components/dashboard-v4/common/MetricCard';
import { FeatureComingSoon } from '../../../components/dashboard-v4/common/FeatureComingSoon';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';

export function ReportsPage() {
  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="依時間區間檢視收支、分類與資產趨勢。"
        title="報表"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="report-period" title="報表區間">
          <div aria-label="報表時間區間" className="filter-chip-list">
            <FilterChip label="週" />
            <FilterChip label="月" selected />
            <FilterChip label="半年" />
            <FilterChip label="年度" />
          </div>
          <FeatureComingSoon
            description="週、月、半年與年度區間選擇準備中。"
            title="區間選擇準備中"
          />
        </DashboardCard>
        <DashboardCard id="spending-trend" title="支出趨勢">
          <FeatureComingSoon
            description="每日消費趨勢圖表準備中。"
            title="圖表區塊準備中"
          />
        </DashboardCard>
        <DashboardCard id="income-expense" title="收入與支出">
          <div className="metric-card-grid">
            <MetricCard label="收入" value="—" />
            <MetricCard label="支出" value="—" />
          </div>
          <FeatureComingSoon
            description="收支比較與儲蓄率分析準備中。"
            title="收支分析準備中"
          />
        </DashboardCard>
        <DashboardCard id="category-report" title="分類支出佔比">
          <FeatureComingSoon
            description="分類支出分析圖表準備中。"
            title="分類分析準備中"
          />
        </DashboardCard>
        <DashboardCard id="assets-trend" title="淨資產趨勢">
          <FeatureComingSoon
            description="歷史資產趨勢圖表準備中。"
            title="資產趨勢準備中"
          />
        </DashboardCard>
      </div>
    </main>
  );
}
