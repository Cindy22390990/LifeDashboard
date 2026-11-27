import { BottomNavigation } from '../components/dashboard-v4/common/BottomNavigation';
import { AssetOverview } from '../components/dashboard-v4/me/AssetOverview';
import { AccountList } from '../components/dashboard-v4/me/AccountList';

export default function MePage() {
  return (
    <main>
      <AssetOverview />
      <AccountList />
      <BottomNavigation />
    </main>
  );
}
