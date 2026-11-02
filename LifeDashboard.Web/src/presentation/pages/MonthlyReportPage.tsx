import { useEffect, useState } from 'react';
import { useReportStore } from '../../application/stores/reportStore';
import CategoryBreakdownChart from '../components/CategoryBreakdownChart';
import './MonthlyReportPage.css';

const currencyFormatter = new Intl.NumberFormat('zh-TW', {
  style: 'currency',
  currency: 'TWD',
  maximumFractionDigits: 2,
});

export default function MonthlyReportPage() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const { report, isLoading, error, fetchMonthlyReport } = useReportStore();

  useEffect(() => {
    void fetchMonthlyReport(year, month).catch(() => undefined);
  }, [fetchMonthlyReport, month, year]);

  const handleFetchReport = async () => {
    try {
      await fetchMonthlyReport(year, month);
    } catch {
      // The store exposes the API error for rendering.
    }
  };

  return (
    <section className="monthly-report">
      <header className="report-header">
        <div>
          <p className="report-eyebrow">REPORT</p>
          <h1>月度收支報表</h1>
          <p>檢視每月收支與分類摘要</p>
        </div>
        <div className="report-period">
          <label>
            年份
            <input
              aria-label="年份"
              type="number"
              min="1"
              max="9999"
              value={year}
              onChange={(event) => setYear(Number(event.target.value))}
            />
          </label>
          <label>
            月份
            <select
              aria-label="月份"
              value={month}
              onChange={(event) => setMonth(Number(event.target.value))}
            >
              {Array.from({ length: 12 }, (_, index) => index + 1).map((value) => (
                <option key={value} value={value}>{value} 月</option>
              ))}
            </select>
          </label>
          <button type="button" onClick={handleFetchReport} disabled={isLoading || year < 1}>
            {isLoading ? '載入中…' : '更新報表'}
          </button>
        </div>
      </header>

      {error && <p className="report-error" role="alert">{error}</p>}
      {isLoading && !report && <p className="report-status">正在載入報表…</p>}

      {report && (
        <>
          <div className="report-summary">
            <article className="summary-card income-card">
              <span>本月收入</span>
              <strong>{currencyFormatter.format(report.totalIncome)}</strong>
            </article>
            <article className="summary-card expense-card">
              <span>本月支出</span>
              <strong>{currencyFormatter.format(report.totalExpense)}</strong>
            </article>
            <article className="summary-card balance-card">
              <span>本月結餘</span>
              <strong>{currencyFormatter.format(report.balance)}</strong>
            </article>
          </div>

          <section className="report-panel">
            <div className="panel-heading">
              <div>
                <h2>分類收支</h2>
                <p>{report.year} 年 {report.month} 月</p>
              </div>
            </div>

            {report.categories.length === 0 ? (
              <p className="report-status">本月尚無分類收支資料。</p>
            ) : (
              <>
                <CategoryBreakdownChart categories={report.categories} />
                <div className="report-table-wrap">
                  <table className="report-table">
                    <thead>
                      <tr>
                        <th scope="col">分類</th>
                        <th scope="col">收入</th>
                        <th scope="col">支出</th>
                        <th scope="col">筆數</th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.categories.map((category, index) => (
                        <tr key={category.categoryId ?? `${category.categoryName}-${index}`}>
                          <th scope="row">{category.categoryName}</th>
                          <td>{currencyFormatter.format(category.income)}</td>
                          <td>{currencyFormatter.format(category.expense)}</td>
                          <td>{category.transactionCount}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </section>
        </>
      )}
    </section>
  );
}
