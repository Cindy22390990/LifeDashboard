import { formatSensitiveMoney } from '../../../../i18n/formatters';
import type { V4DashboardSummaryViewModel } from '../../../../mappers/dashboardV4';
import { useTranslation } from 'react-i18next';
import { usePrivacyDisplayStore } from '../../../stores/privacyDisplayStore';
import './DashboardSummaryCard.css';

interface DashboardSummaryCardProps {
  viewModel: V4DashboardSummaryViewModel;
  isLoading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  isFetching?: boolean;
}

export function DashboardSummaryCard({
  viewModel,
  isLoading = false,
  error,
  onRetry,
  isFetching = false,
}: DashboardSummaryCardProps) {
  const { i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);
  const toggleVisibility = usePrivacyDisplayStore((state) => state.toggleVisibility);

  return (
    <section
      aria-label="Dashboard Summary"
      className="v4-home-card v4-dashboard-summary"
    >
      <div className="v4-dashboard-summary__heading">
        <h2>本月財務摘要</h2>
        <button
          aria-label={isAmountHidden ? '顯示金額' : '隱藏金額'}
          aria-pressed={isAmountHidden}
          type="button"
          onClick={toggleVisibility}
        >
          {isAmountHidden ? '顯示金額' : '隱藏金額'}
        </button>
      </div>

      {isLoading && (
        <p role="status">正在載入財務摘要…</p>
      )}

      {error && !isLoading && (
        <div role="alert">
          <p>{error}</p>
          {onRetry && (
            <button
              type="button"
              disabled={isFetching}
              onClick={onRetry}
            >
              重試
            </button>
          )}
        </div>
      )}

      {!isLoading && !error && (
        <dl className="v4-dashboard-summary__values">
          <div>
            <dt>總資產</dt>
            <dd>
              {formatSensitiveMoney(viewModel.totalAssets, locale, isAmountHidden)}
            </dd>
          </div>
          <div>
            <dt>本月收入</dt>
            <dd>
              {formatSensitiveMoney(viewModel.monthlyIncome, locale, isAmountHidden)}
            </dd>
          </div>
          <div>
            <dt>本月支出</dt>
            <dd>
              {formatSensitiveMoney(viewModel.monthlyExpense, locale, isAmountHidden)}
            </dd>
          </div>
        </dl>
      )}
    </section>
  );
}
