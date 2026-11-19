import { Link } from 'react-router-dom';
import { TransactionType } from '../../../domain/Transaction';
import type { DashboardTransactionReadModel } from '../../../domain/dashboardSummary';
import type { ICategory } from '../../../domain/Category';
import { formatCurrency } from './dashboardFormatting';
import './RecentActivity.css';

interface RecentActivityProps {
  categories: readonly ICategory[];
  transactions: readonly DashboardTransactionReadModel[];
}

function getTransactionTypeLabel(type: number): string {
  switch (type) {
    case TransactionType.Income:
      return '收入';
    case TransactionType.Expense:
      return '支出';
    case TransactionType.Transfer:
      return '轉帳';
    default:
      return '交易';
  }
}

function getTransactionTitle(transaction: DashboardTransactionReadModel): string {
  return transaction.description?.trim() || getTransactionTypeLabel(transaction.type);
}

function getCategoryLabel(
  transaction: DashboardTransactionReadModel,
  categories: readonly ICategory[],
): string | null {
  const category = categories.find((item) => item.id === transaction.categoryEntityId);
  return category
    ? `${category.icon} ${category.name}`
    : transaction.categoryId?.trim() || null;
}

function formatTransactionDate(value: string): string {
  const date = new Date(`${value.slice(0, 10)}T00:00:00`);
  return Number.isNaN(date.getTime())
    ? value
    : date.toLocaleDateString('zh-TW', { month: 'short', day: 'numeric' });
}

function getAmountClass(type: number): string {
  if (type === TransactionType.Income) {
    return 'recent-activity__amount recent-activity__amount--income';
  }

  if (type === TransactionType.Expense) {
    return 'recent-activity__amount recent-activity__amount--expense';
  }

  return 'recent-activity__amount';
}

function getAmountSign(type: number): string {
  if (type === TransactionType.Income) return '+';
  if (type === TransactionType.Expense) return '−';
  return '';
}

export default function RecentActivity({ categories, transactions }: RecentActivityProps) {
  const displayedTransactions = transactions.slice(0, 5);

  return (
    <section className="recent-activity dashboard-card" aria-labelledby="recent-activity-title">
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">TRANSACTIONS</p>
          <h2 id="recent-activity-title">最近交易</h2>
        </div>
        <Link className="recent-activity__view-all" to="/transactions">
          查看全部
        </Link>
      </div>

      <div className="recent-activity__content">
        {displayedTransactions.length === 0 ? (
          <p className="recent-activity__empty">目前沒有交易資料。</p>
        ) : (
          <ul className="recent-activity__list">
            {displayedTransactions.map((transaction) => {
              const categoryLabel = getCategoryLabel(transaction, categories);

              return (
                <li className="recent-activity__item" key={transaction.id}>
                  <div className="recent-activity__details">
                    <strong>{getTransactionTitle(transaction)}</strong>
                    <div className="recent-activity__metadata">
                      {categoryLabel && (
                        <span className="recent-activity__category">{categoryLabel}</span>
                      )}
                      {transaction.type === TransactionType.Transfer ? (
                        <span>
                          {transaction.accountName || '帳戶'}
                          {transaction.toAccountName && ` → ${transaction.toAccountName}`}
                        </span>
                      ) : transaction.accountName ? (
                        <span>{transaction.accountName}</span>
                      ) : null}
                    </div>
                    <time dateTime={transaction.transactionDate}>
                      {formatTransactionDate(transaction.transactionDate)}
                    </time>
                  </div>
                  <strong className={getAmountClass(transaction.type)}>
                    {getAmountSign(transaction.type)}{formatCurrency(transaction.amount)}
                  </strong>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </section>
  );
}
