interface FilterChipProps {
  label: string;
  selected?: boolean;
  disabled?: boolean;
  onClick?: () => void;
}

export function FilterChip({
  label,
  selected = false,
  disabled,
  onClick,
}: FilterChipProps) {
  const isDisabled = disabled ?? !onClick;

  return (
    <button
      aria-pressed={selected}
      className={`filter-chip${selected ? ' is-selected' : ''}`}
      disabled={isDisabled}
      onClick={onClick}
      type="button"
    >
      {label}
    </button>
  );
}
