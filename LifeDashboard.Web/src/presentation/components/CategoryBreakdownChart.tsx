import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { CategoryBreakdown } from '../../domain/report';

interface CategoryBreakdownChartProps {
  categories: CategoryBreakdown[];
}

export default function CategoryBreakdownChart({
  categories,
}: CategoryBreakdownChartProps) {
  return (
    <div className="report-chart" role="img" aria-label="分類收入與支出長條圖">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={categories} margin={{ top: 8, right: 12, left: 0, bottom: 8 }}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="categoryName" />
          <YAxis />
          <Tooltip />
          <Legend />
          <Bar dataKey="income" name="收入" fill="#16a36a" />
          <Bar dataKey="expense" name="支出" fill="#dc4c4c" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
