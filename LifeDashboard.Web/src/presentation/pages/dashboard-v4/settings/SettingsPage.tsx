import { DashboardCard } from '../../../components/dashboard-v4/common/DashboardCard';
import { FilterChip } from '../../../components/dashboard-v4/common/FilterChip';
import { FeatureComingSoon } from '../../../components/dashboard-v4/common/FeatureComingSoon';
import { FeaturePageHeader } from '../../../components/dashboard-v4/common/FeaturePageHeader';

export function SettingsPage() {
  return (
    <main className="v4-feature-page">
      <FeaturePageHeader
        description="管理個人資料、快速記帳預設值與應用程式偏好。"
        title="設定"
      />
      <div className="v4-feature-grid">
        <DashboardCard id="profile-settings" title="個人資料">
          <FeatureComingSoon
            description="個人帳戶資料檢視與編輯介面準備中。"
            title="個人資料設定準備中"
          />
        </DashboardCard>
        <DashboardCard id="quick-create-settings" title="快速記帳預設">
          <FeatureComingSoon
            description="預設支出帳戶與預設分類設定介面準備中。"
            title="預設值設定準備中"
          />
        </DashboardCard>
        <DashboardCard id="appearance-settings" title="外觀與隱私">
          <div aria-label="外觀主題" className="filter-chip-list">
            <FilterChip label="淺色" />
            <FilterChip label="深色" />
            <FilterChip label="系統設定" selected />
          </div>
          <FeatureComingSoon
            description="深色模式、隱私顯示與語言選擇介面準備中。"
            title="外觀偏好準備中"
          />
        </DashboardCard>
        <DashboardCard id="security-settings" title="資料與安全性">
          <FeatureComingSoon
            description="資料匯出與帳號安全管理介面準備中。"
            title="資料與安全設定準備中"
          />
        </DashboardCard>
      </div>
    </main>
  );
}
