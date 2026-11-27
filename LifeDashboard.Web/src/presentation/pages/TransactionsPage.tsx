import { BottomNavigation } from '../components/dashboard-v4/common/BottomNavigation';
import { TransactionList } from '../components/dashboard-v4/transactions/TransactionList';
import { SearchBar } from '../components/dashboard-v4/transactions/SearchBar';

export default function TransactionsPage() {
  return (
    <main>
      <SearchBar />
      <TransactionList />
      <BottomNavigation />
    </main>
  );
}
