import { useState } from 'react';
import { AccountType, type IAccount } from '../../../domain/Account';
import { useMobileViewport } from '../../hooks/useMobileViewport';
import { formatCurrency } from './dashboardFormatting';
import './AccountDetails.css';

interface AccountDetailsProps {
  accounts: readonly IAccount[];
}

function getAccountTypeLabel(type: AccountType): string {
  switch (type) {
    case AccountType.Cashflow:
      return '日常現金流';
    case AccountType.Saving:
      return '儲備帳戶';
    case AccountType.Invest:
      return '投資帳戶';
  }
}

export default function AccountDetails({ accounts }: AccountDetailsProps) {
  const isMobile = useMobileViewport();
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <section className="account-details dashboard-card" aria-labelledby="account-details-title">
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">ACCOUNTS</p>
          <h2 id="account-details-title">帳戶明細</h2>
        </div>
        <span className="account-details__count">{accounts.length} 個帳戶</span>
        {isMobile && (
          <button
            className="account-details__toggle"
            type="button"
            aria-expanded={isExpanded}
            aria-controls="account-details-list"
            onClick={() => setIsExpanded((expanded) => !expanded)}
          >
            {isExpanded ? '收合' : '展開'}
          </button>
        )}
      </div>

      <div
        id="account-details-list"
        className="account-details__content"
        hidden={isMobile && !isExpanded}
      >
        {accounts.length === 0 ? (
          <p className="account-details__empty">目前沒有帳戶資料。</p>
        ) : (
          <ul className="account-details__list">
            {accounts.map((account) => (
              <li className="account-details__item" key={account.id}>
                <div className="account-details__identity">
                  <strong>{account.name}</strong>
                  <span>{getAccountTypeLabel(account.type)}</span>
                </div>
                <strong className="account-details__balance">{formatCurrency(account.currentBalance)}</strong>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
