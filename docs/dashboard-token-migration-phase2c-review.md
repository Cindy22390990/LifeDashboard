# Dashboard Token Migration Phase 2C Review

## Completed Migration

- `DashboardPage.css` uses Phase 1 `--color-*`, `--space-*`, and `--radius-md` tokens. Its local `--dashboard-*` variables are aliases to semantic tokens, not independent color values.
- `GoalPyramid.css` uses semantic surface, text, border, success, and warning tokens. Safety/Growth/Dream tier tokens are confined to the tier top indicator and progress accent. Goal amount values already use `font-variant-numeric: tabular-nums`.
- `BudgetRadarPanel.css` uses semantic surface, text, border, and warning tokens. No fixed white card surface or fixed black/text color remains in these dashboard component styles.
- In light and dark browser checks, the Goal and Budget card surfaces resolve to the semantic surface for the active scheme. Dark mode uses the dark surface/text/border values; status and tier accents also resolve to their dark token values. No additional dark selector was needed in the component styles.

## Remaining Legacy Values

- `index.css` retains the original `--bg`, `--text`, `--text-h`, and `--border` variables for backward compatibility. Its root/page styling still consumes these legacy variables.
- `AppShell.css` still uses legacy variables for the utility navigation, fixed bottom navigation, modal surfaces, and borders. Their current light/dark values visually align with the corresponding semantic canvas/surface/text/border values, but token usage is not yet unified across the shell.
- AppShell's modal backdrop uses a fixed translucent black overlay; this is an overlay treatment, not a fixed card background or text color.
- Radius roles are not fully represented by Phase 1 tokens: only `--radius-sm` (4px) and `--radius-md` (8px) exist. Current styles retain 6px controls/buttons, 10px Budget cards, 12px modal, 16px navigation, and pill/circle radii. Goal tier cards and Dashboard panels use 8px; Budget metric cards use 10px; Goal status pills use 999px.
- The requested names `--radius-control` and `--radius-card` are not defined; current styles use the available `--radius-sm` and `--radius-md` where applicable.
- BudgetRadar values do not explicitly set tabular numerals. `--font-number-family` exists, but it is a font-family token, not a tabular-numeric setting. No typography change was made during this review.

## Dark Mode Review

Browser computed-style checks were performed in light and dark schemes at desktop and mobile viewport sizes.

- Dashboard background and panel surfaces change with the active color scheme. Goal tiers and Budget metric cards resolve to `#fff` in light mode and the dark surface (`#16171d`) in dark mode; they are not fixed white.
- Primary and muted text resolve through semantic tokens (`#08060d` / `#6b6375` in light, `#f3f4f6` / `#9ca3af` in dark). Text does not remain fixed black.
- Borders resolve through the subtle border token (`#e5e4e7` light, `#2e303a` dark). They remain distinguishable against their respective surfaces.
- Completed state uses the success token; overflow and Budget warnings use the warning token. Their dark values are theme-specific.
- Tier accents remain narrow 3px tier indicators and progress fills. They do not fill tier/card backgrounds or replace primary text, so they do not dominate the information hierarchy.

## Responsive Review

Browser checks used the running Dashboard with presentation-only mocked summary data; no API or persisted data was changed.

### Desktop, 1280px

- Browser `innerWidth` was 1280px; document client/scroll widths were both 1265px (the difference from the viewport is the vertical scrollbar), with no horizontal overflow.
- Pyramid tier widths were 900px, 738px, and 576px for Tier 1, Tier 2, and Tier 3 respectively; their visual order is Tier 3 at the top through Tier 1 at the base.
- Progress indicators remained within their goal rows and the completed state rendered its check and label.
- BudgetRadar renders five equal-sized metric cards with equal 22px values. `TodayRemaining` does not currently have stronger visual emphasis than the other four values.

### Mobile, 390px

- Browser `innerWidth` was 390px; document client/scroll widths were both 375px (vertical scrollbar allowance), so there was no horizontal overflow.
- Tier sections become a vertical Safety → Growth → Dream list, each 357px wide inside the page content area.
- The two active progress bars measured 324px and fit within their tier content; the completed goal's state remained readable.
- All three account rows fit their 357px container; account names, types, and balances remained readable without horizontal scrolling.
- BudgetRadar stacks to one column and becomes relatively tall. Its five values remain visually equal in hierarchy.

## Issues and Recommendations

- No clear semantic-token or dark-mode CSS bug requiring a small correction was found; no code was changed during this review.
- The BudgetRadar primary-metric requirement is not met: TodayRemaining is visually equal to DailySafeLimit, TodaySpent, RemainingDays, and TotalLivingBudget. Address this in a separately scoped UI hierarchy change, not as part of token migration.
- Consider a later design-system decision on card/control/pill radius roles. Do not silently normalize the retained 6px, 10px, 12px, 16px, or pill/circle values during token-only migration.
- Migrate AppShell's legacy color references in a separate phase after verifying all legacy variable consumers; retain backward-compatible aliases until that migration is complete.