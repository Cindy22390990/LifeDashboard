import type { SectionState } from '../../../application/dashboard/models/SectionState';
import { useTranslation } from 'react-i18next';

type NonReadySectionState = Exclude<SectionState<unknown>, { status: 'ready' }>;

interface SectionStateMessageProps {
  state: NonReadySectionState;
  emptyMessage: string;
}

export default function SectionStateMessage({
  state,
  emptyMessage,
}: SectionStateMessageProps) {
  const { t } = useTranslation();
  const isError = state.status === 'error';
  const message = isError
    ? state.error.message
    : state.status === 'pending'
      ? t('reports.comingSoon')
      : emptyMessage;

  return (
    <div
      className="dashboard-v3__empty-state"
      role={isError ? 'alert' : 'status'}
    >
      {message}
    </div>
  );
}
