interface ProgressBarProps {
  label: string;
  value?: number;
}

export function ProgressBar({ label, value }: ProgressBarProps) {
  const progress =
    typeof value === 'number' && Number.isFinite(value)
      ? Math.min(100, Math.max(0, value))
      : undefined;

  return (
    <div
      aria-label={label}
      aria-valuemax={100}
      aria-valuemin={0}
      aria-valuenow={progress}
      aria-valuetext={progress === undefined ? '尚未提供' : `${progress}%`}
      className={`progress-bar${progress === undefined ? ' is-placeholder' : ''}`}
      role="progressbar"
    >
      <span
        className="progress-bar__fill"
        style={progress === undefined ? undefined : { width: `${progress}%` }}
      />
    </div>
  );
}
