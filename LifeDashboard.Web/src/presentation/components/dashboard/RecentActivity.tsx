import { useState } from 'react';
import { TransactionType } from '../../../domain/Transaction';
import type { DashboardTransactionReadModel } from '../../../domain/dashboardSummary';
import { useMobileViewport } from '../../hooks/useMobileViewport';
import { formatCurrency } from './dashboardFormatting';
import './RecentActivity.css';

interface RecentActivityProps {
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

export default function RecentActivity({ transactions }: RecentActivityProps) {
  const isMobile = useMobileViewport();
  const [isExpanded, setIsExpanded] = useState(false);
  const latestTransaction = transactions[0];

  return (
    <section className="recent-activity dashboard-card" aria-labelledby="recent-activity-title">
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">RECENT ACTIVITY</p>
          <h2 id="recent-activity-title">最近交易</h2>
        </div>
        {isMobile && transactions.length > 0 && (
          <button
            className="recent-activity__toggle"
            type="button"
            aria-expanded={isExpanded}
            aria-controls="recent-activity-list"
            onClick={() => setIsExpanded((expanded) => !expanded)}
          >
            {isExpanded ? '收合' : '展開'}
          </button>
        )}
      </div>

      {isMobile && (
        <div className="recent-activity__preview">
          {latestTransaction ? (
            <>
              <div className="recent-activity__details">
                <strong>
                  {latestTransaction.description
                    || getTransactionTypeLabel(latestTransaction.type)}
                </strong>
                <span>
                  {latestTransaction.transactionDate}
                  {' · '}
                  {getTransactionTypeLabel(latestTransaction.type)}
                </span>
              </div>
              <strong className="recent-activity__amount">
                {formatCurrency(latestTransaction.amount)}
              </strong>
            </>
          ) : (
            <p className="recent-activity__empty">目前沒有交易資料。</p>
          )}
        </div>
      )}

      <div
        id="recent-activity-list"
        className="recent-activity__content"
        hidden={isMobile && !isExpanded}
      >
        {transactions.length === 0 ? (
          <p className="recent-activity__empty">目前沒有交易資料。</p>
        ) : (
          <ul className="recent-activity__list">
            {transactions.map((transaction) => (
              <li className="recent-activity__item" key={transaction.id}>
                <div className="recent-activity__details">
                  <strong>{transaction.description || getTransactionTypeLabel(transaction.type)}</strong>
                  <span>
                    {transaction.transactionDate}
                    {' · '}
                    {getTransactionTypeLabel(transaction.type)}
                    {transaction.accountName && ` · ${transaction.accountName}`}
                    {transaction.type === TransactionType.Transfer && transaction.toAccountName
                      ? ` → ${transaction.toAccountName}`
                      : ''}
                  </span>
                </div>
                <strong className="recent-activity__amount">{formatCurrency(transaction.amount)}</strong>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
