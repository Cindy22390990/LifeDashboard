import { useState } from 'react';
import { formatCurrency } from './dashboardFormatting';
import './FinancialOverview.css';

interface FinancialOverviewProps {
  totalAssets: number;
  monthlyIncome: number;
  monthlyExpense: number;
  todayRemaining: number;
  currentDate: string;
}

export default function FinancialOverview({
  totalAssets,
  monthlyIncome,
  monthlyExpense,
  todayRemaining,
  currentDate,
}: FinancialOverviewProps) {
  const [isTotalAssetsVisible, setIsTotalAssetsVisible] = useState(false);
  const monthLabel = new Date(`${currentDate}T00:00:00`).toLocaleDateString('zh-TW', {
    year: 'numeric',
    month: 'long',
  });

  return (
    <section className="financial-overview dashboard-card" aria-labelledby="financial-overview-title">
      <div className="dashboard-card__heading">
        <div>
          <p className="dashboard-card__eyebrow">FINANCIAL OVERVIEW</p>
          <h2 id="financial-overview-title">財務總覽</h2>
        </div>
      </div>

      <div className="financial-overview__total">
        <span className="financial-overview__label">總資產</span>
        <div className="financial-overview__value-row">
          <strong aria-live="polite">
            {isTotalAssetsVisible ? formatCurrency(totalAssets) : '*******'}
          </strong>
          <button
            className="financial-overview__visibility"
            type="button"
            aria-pressed={isTotalAssetsVisible}
            aria-label={isTotalAssetsVisible ? '隱藏總資產' : '顯示總資產'}
            onClick={() => setIsTotalAssetsVisible((visible) => !visible)}
          >
            {isTotalAssetsVisible ? '隱藏' : '查看'}
          </button>
        </div>
      </div>

      <div className="financial-overview__monthly" aria-label={`${monthLabel}收入與支出`}>
        <p className="financial-overview__period">{monthLabel} · 日曆月</p>
        <div className="financial-overview__monthly-values">
          <div>
            <span>本月收入</span>
            <strong>{formatCurrency(monthlyIncome)}</strong>
          </div>
          <div>
            <span>本月支出</span>
            <strong>{formatCurrency(monthlyExpense)}</strong>
          </div>
        </div>
        <div className="financial-overview__today">
          <div className="financial-overview__today-label">
            <span>今日剩餘預算額度</span>
            <span className="financial-overview__period">Payday 所屬預算週期</span>
          </div>
          <strong>{formatCurrency(todayRemaining)}</strong>
        </div>
      </div>
    </section>
  );
}
