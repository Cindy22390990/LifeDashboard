import type { ReactNode } from 'react';

interface DashboardCardProps {
  id: string;
  title: string;
  children: ReactNode;
}

export function DashboardCard({ id, title, children }: DashboardCardProps) {
  return (
    <section aria-labelledby={`${id}-title`} className="dashboard-card">
      <h2 className="dashboard-card__title" id={`${id}-title`}>
        {title}
      </h2>
      {children}
    </section>
  );
}
