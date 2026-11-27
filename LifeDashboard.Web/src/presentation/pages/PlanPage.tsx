import { BottomNavigation } from '../components/dashboard-v4/common/BottomNavigation';
import { BudgetView } from '../components/dashboard-v4/plan/BudgetView';
import { GoalView } from '../components/dashboard-v4/plan/GoalView';

export default function PlanPage() {
  return (
    <main>
      <BudgetView />
      <GoalView />
      <BottomNavigation />
    </main>
  );
}
