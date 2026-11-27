interface FeatureComingSoonProps {
  title: string;
  description: string;
}

export function FeatureComingSoon({
  title,
  description,
}: FeatureComingSoonProps) {
  return (
    <div className="v4-feature-coming-soon" role="status">
      <span aria-hidden="true" className="v4-feature-coming-soon__icon">◷</span>
      <div>
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}
