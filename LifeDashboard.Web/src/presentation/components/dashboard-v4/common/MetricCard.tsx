import type { ReactNode } from 'react';

interface MetricCardProps {
  label: string;
  value: ReactNode;
  description?: string;
}

export function MetricCard({ label, value, description }: MetricCardProps) {
  return (
    <article className="metric-card">
      <p className="metric-card__label">{label}</p>
      <strong className="metric-card__value">{value}</strong>
      {description && <p className="metric-card__description">{description}</p>}
    </article>
  );
}
