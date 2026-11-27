import { DashboardV4Navigation } from './DashboardV4Navigation';

interface FeaturePageHeaderProps {
  title: string;
  description: string;
}

export function FeaturePageHeader({
  title,
  description,
}: FeaturePageHeaderProps) {
  return (
    <header className="v4-feature-header">
      <div className="v4-feature-header__title">
        <p className="v4-feature-header__eyebrow">Dashboard V4</p>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      <DashboardV4Navigation />
    </header>
  );
}
