import { useState } from 'react';
import { formatCurrency } from './dashboardFormatting';
import './FinancialOverview.css';

interface FinancialOverviewProps {
  totalAssets: number;
  monthlyIncome: number;
  monthlyExpense: number;
  availableMoney: number;
  currentDate: string;
}

export default function FinancialOverview({
  totalAssets,
  monthlyIncome,
  monthlyExpense,
  availableMoney,
  currentDate,
}: FinancialOverviewProps) {
  const [isTotalAssetsVisible, setIsTotalAssetsVisible] = useState(true);
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

      <p className="financial-overview__month">{monthLabel} · 日曆月</p>

      <div className="financial-overview__metrics">
        <div className="financial-overview__metric financial-overview__metric--assets">
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
        <div className="financial-overview__metric">
          <span className="financial-overview__label">本月收入</span>
          <strong>{formatCurrency(monthlyIncome)}</strong>
        </div>
        <div className="financial-overview__metric">
          <span className="financial-overview__label">本月支出</span>
          <strong>{formatCurrency(monthlyExpense)}</strong>
        </div>
        <div className="financial-overview__metric financial-overview__metric--available">
          <span className="financial-overview__label">可用生活預算</span>
          <strong>{formatCurrency(availableMoney)}</strong>
        </div>
      </div>
    </section>
  );
}
