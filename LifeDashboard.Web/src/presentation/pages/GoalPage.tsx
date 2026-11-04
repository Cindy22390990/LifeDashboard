import axios from 'axios';
import { useEffect, useState } from 'react';
import { useGoalStore } from '../../application/stores/goalStore';
import type { IAccount } from '../../domain/Account';
import {
  GoalStatus,
  GoalTier,
  GoalType,
  type IGoal,
  type IGoalDepositRequest,
} from '../../domain/Goal';
import { AccountRepository } from '../../infrastructure/api/AccountRepository';
import GoalDepositModal from '../components/GoalDepositModal';

const tiers = [
  { value: GoalTier.Tier1_Safety, name: 'Tier 1｜安全預備金' },
  { value: GoalTier.Tier2_Burden, name: 'Tier 2｜中期負擔 / 大額目標' },
  { value: GoalTier.Tier3_Dream, name: 'Tier 3｜夢想目標' },
];

function getGoalTypeName(goalType: GoalType): string {
  switch (goalType) {
    case GoalType.Saving:
      return '純存錢目標';
    case GoalType.Purchase:
      return '購物願望';
    default:
      return '未知類型';
  }
}

function getGoalStatusName(status: GoalStatus): string {
  switch (status) {
    case GoalStatus.Active:
      return '進行中';
    case GoalStatus.Completed:
      return '已完成';
    default:
      return '未知狀態';
  }
}

function formatAmount(amount: number): string {
  return `$ ${amount.toLocaleString('zh-TW', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

function getProgress(goal: IGoal): number {
  if (goal.targetAmount <= 0) {
    return 0;
  }

  return Math.min(100, Math.max(0, (goal.currentAmount / goal.targetAmount) * 100));
}

function getAccountErrorMessage(error: unknown): string {
  if (axios.isAxiosError<{ message?: string; Message?: string }>(error)) {
    return error.response?.data?.message
      ?? error.response?.data?.Message
      ?? '無法載入帳戶資料，請稍後再試。';
  }

  return '載入帳戶資料時發生未預期錯誤。';
}

export default function GoalPage() {
  const {
    goals,
    isLoading,
    error,
    fetchGoals,
    depositGoal,
  } = useGoalStore();
  const [accounts, setAccounts] = useState<IAccount[]>([]);
  const [accountsLoading, setAccountsLoading] = useState(true);
  const [accountsError, setAccountsError] = useState<string | null>(null);
  const [selectedGoal, setSelectedGoal] = useState<IGoal | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    void fetchGoals().catch(() => undefined);
  }, [fetchGoals]);

  useEffect(() => {
    let isCurrent = true;
    setAccountsLoading(true);
    setAccountsError(null);

    AccountRepository.getAll()
      .then((result) => {
        if (isCurrent) {
          setAccounts(result);
          setAccountsLoading(false);
        }
      })
      .catch((loadError: unknown) => {
        if (isCurrent) {
          setAccountsError(getAccountErrorMessage(loadError));
          setAccountsLoading(false);
        }
      });

    return () => {
      isCurrent = false;
    };
  }, []);

  const handleDeposit = async (request: IGoalDepositRequest) => {
    if (!selectedGoal) {
      return;
    }

    setIsSubmitting(true);
    try {
      await depositGoal(selectedGoal.id, request);
      setSelectedGoal(null);
    } catch {
      // The goal store exposes the API error on this page.
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main style={{ maxWidth: '1000px', margin: '0 auto', padding: '20px' }}>
      <h1>Goal Pyramid</h1>
      <p>依目標層級檢視進度；目標金額與狀態以伺服器資料為準。</p>

      {error && <p role="alert">{error}</p>}
      {accountsError && <p role="alert">{accountsError}</p>}

      {isLoading && goals.length === 0 ? (
        <p>目標載入中…</p>
      ) : goals.length === 0 ? (
        <p>目前沒有目標。</p>
      ) : (
        <div style={{ display: 'grid', gap: '20px' }}>
          {tiers.map((tier) => {
            const tierGoals = goals
              .filter((goal) => goal.tier === tier.value)
              .sort((left, right) => left.id - right.id);

            return (
              <section key={tier.value} aria-labelledby={`goal-tier-${tier.value}`}>
                <h2 id={`goal-tier-${tier.value}`}>{tier.name}</h2>
                {tierGoals.length === 0 ? (
                  <p>此層目前沒有目標。</p>
                ) : (
                  <div
                    style={{
                      display: 'grid',
                      gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
                      gap: '12px',
                    }}
                  >
                    {tierGoals.map((goal) => {
                      const progress = getProgress(goal);

                      return (
                        <article
                          key={goal.id}
                          style={{
                            padding: '16px',
                            border: '1px solid #ddd',
                            borderRadius: '10px',
                            backgroundColor: 'white',
                          }}
                        >
                          <div
                            style={{
                              display: 'flex',
                              justifyContent: 'space-between',
                              alignItems: 'flex-start',
                              gap: '12px',
                            }}
                          >
                            <h3 style={{ marginTop: 0 }}>{goal.name}</h3>
                            <span>{getGoalStatusName(goal.status)}</span>
                          </div>
                          <p>{getGoalTypeName(goal.goalType)}</p>
                          <p>
                            {formatAmount(goal.currentAmount)} / {formatAmount(goal.targetAmount)}
                          </p>
                          <progress
                            aria-label={`${goal.name} 目標進度`}
                            max={100}
                            value={progress}
                            style={{ width: '100%' }}
                          />
                          <p>{Math.floor(progress)}%</p>
                          {goal.status === GoalStatus.Active && (
                            <button
                              type="button"
                              onClick={() => setSelectedGoal(goal)}
                              disabled={accountsLoading || Boolean(accountsError)}
                            >
                              存入目標
                            </button>
                          )}
                        </article>
                      );
                    })}
                  </div>
                )}
              </section>
            );
          })}
        </div>
      )}

      {selectedGoal && (
        <GoalDepositModal
          goal={selectedGoal}
          accounts={accounts}
          isSubmitting={isSubmitting}
          onClose={() => setSelectedGoal(null)}
          onSubmit={handleDeposit}
        />
      )}
    </main>
  );
}
