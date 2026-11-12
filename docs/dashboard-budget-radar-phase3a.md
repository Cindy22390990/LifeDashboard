# Dashboard Budget Radar Phase 3A

## Modified Files

- [BudgetRadarPanel.tsx](../LifeDashboard.Web/src/presentation/components/BudgetRadarPanel.tsx)
- [BudgetRadarPanel.css](../LifeDashboard.Web/src/presentation/components/BudgetRadarPanel.css)
- [dashboard-budget-radar-phase3a.md](dashboard-budget-radar-phase3a.md)

## Reason

The previous Budget Radar rendered five equal-weight metric cards, so TodayRemaining did not read as the primary daily decision metric. The updated presentation gives the remaining safe amount its own leading region and lowers the visual weight of supporting values.

## Hierarchy Changes

- TodayRemaining is first, labeled in Chinese as 「今日剩餘安全額度」, and displayed with the largest amount typography.
- A non-negative remaining amount uses the semantic success color; a negative amount uses the semantic warning color. The existing over-budget notice remains.
- TodaySpent, DailySafeLimit, RemainingDays, and TotalLivingBudget remain present with Chinese labels in a smaller, unframed supporting-metric layout.
- Uncategorized expense feedback remains unchanged in behavior.
- Existing semantic tokens are reused; no token, API, DTO, calculation, or data field was added or changed.

## Responsive Behavior

- Desktop: TodayRemaining spans the full panel width; the four supporting metrics sit in a horizontal row.
- Mobile: TodayRemaining remains first and full-width; supporting values switch to a compact two-column layout. At 390px there was no horizontal overflow.

## Validation

- `npm run build` succeeded. Vite emitted the existing warning for a minified JavaScript chunk larger than 500 kB.
- Browser review used presentation-only mocked summary data at 1280px and 390px in light and dark schemes. TodayRemaining remained the most prominent metric; the light/dark semantic colors resolved correctly; the page had no horizontal overflow.

## Unchanged Items

- Backend/API, DTO, domain models, and dashboard data flow.
- DashboardPage, GoalPyramid, AppShell, and all non-BudgetRadar styles.
- Metric calculation/formatting and warning conditions.
- No component split, animation, or new token was introduced.