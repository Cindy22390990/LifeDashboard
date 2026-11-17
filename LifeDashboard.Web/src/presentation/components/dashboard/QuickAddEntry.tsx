import './QuickAddEntry.css';

interface QuickAddEntryProps {
  onOpenTransactionEntry?: () => void;
}

export default function QuickAddEntry({ onOpenTransactionEntry }: QuickAddEntryProps) {
  return (
    <button
      className="quick-add-entry"
      type="button"
      onClick={onOpenTransactionEntry}
      disabled={!onOpenTransactionEntry}
    >
      + 新增交易
    </button>
  );
}
