# Dashboard Token Hardening Final Review

## 檢查範圍

Reviewed the Dashboard styles and related global shell styles:

- `LifeDashboard.Web/src/presentation/pages/DashboardPage.css`
- `LifeDashboard.Web/src/presentation/components/dashboard/GoalPyramid.css`
- `LifeDashboard.Web/src/presentation/components/BudgetRadarPanel.css`
- `LifeDashboard.Web/src/index.css`
- `LifeDashboard.Web/src/AppShell.css`
- Read-only behavior review of `BudgetRadarPanel.tsx`, `DashboardPage.tsx`, and `App.tsx` for state, section order, and navigation integration.

The requested `LifeDashboard.Web/src/presentation/components/AppShell.css` path does not exist; the actual AppShell stylesheet is `LifeDashboard.Web/src/AppShell.css`.

## 已確認項目

### Semantic Tokens and Legacy Variables

- DashboardPage, GoalPyramid, and BudgetRadar styles use the existing `--color-*` semantic colors for surfaces, text, borders, and statuses. Goal tier accents use the tier tokens only for the small tier indicator and progress accent.
- DashboardPage's `--dashboard-*` declarations are local aliases to semantic tokens, not independent legacy colors.
- Legacy `--bg`, `--text`, `--border`, and `--accent` remain in `index.css` and are still consumed by global base rules and `AppShell.css` (utility/nav/modal surfaces, text, borders, and active navigation). Per scope, they were recorded and not migrated.
- `App.css` contains legacy references but no TS/TSX import was found; it is not part of the active AppShell import path.
- No fixed hex/rgb color was found in the reviewed Dashboard component styles; status and warning blends are composed from existing semantic tokens.

### Radius and Typography

- Dashboard panels, Goal tiers, and BudgetRadar primary/notice panels use `--radius-md`; Goal status/progress indicators use pill radii; the completion mark is circular.
- Quick Add input/select, fieldset, submit button, and the nested category-navigation button use `--radius-sm`. The nested button needed a specific selector because it is inside a paragraph rather than a direct form child.
- AppShell's 16px navigation and 12px modal radii are shell-level treatments and remain unchanged; they are outside the Dashboard component radius migration.
- Dashboard balance, account, Goal amount, and BudgetRadar financial numbers use `font-variant-numeric: tabular-nums`. The defined `--font-number-family` is not used; no monospace font was forced because tabular numerals already provide aligned financial figures without changing the existing font appearance.
- GoalPyramid's h2 now explicitly uses `--color-text-primary` rather than inheriting the global legacy `--text-h` heading color. The resulting color remains equivalent in the current light/dark palettes.

### Mobile Safe Area and Browser Results

Browser checks used presentation-only mocked summary data; no API or persisted data was changed.

| Viewport/theme | Horizontal overflow | Fixed navigation and scroll result |
|---|---|---|
| 1280px light | None; document scroll/client widths both 1265px (vertical scrollbar excluded from layout width) | Quick Add bottom can scroll to 64px above navigation; Account final row is reachable above navigation |
| 1280px dark | None; document scroll/client widths both 1265px | Same 64px footer clearance; Goal/Budget surfaces and text use dark semantic values |
| 390px light | None; document scroll/client widths both 375px | Radar and Account sections can be scrolled fully above navigation; final Quick Add bottom has 65px clearance |
| 390px dark | None; document scroll/client widths both 375px | Same 65px footer clearance; no fixed white Dashboard cards or fixed black Dashboard text |

- `.app-main` reserves `--bottom-nav-height + --bottom-nav-inset + --bottom-nav-clearance`; the fixed nav uses the same safe-area-aware inset. At page end, content remains scrollable above the nav.
- The mobile Account last row can be positioned above the nav. The Radar panel can also be brought fully into view by scrolling; it is not trapped behind the fixed nav.

### BudgetRadar State and Accessibility

- `todayRemaining >= 0` renders the normal state; `< 0` renders the over-budget state and preserves the negative amount.
- For an over-budget value, the visible inline state text is `aria-hidden`; the full over-budget message remains a `role="status"` announcement. The uncategorized reminder has its own `role="status"` and separate message, so the two live announcements convey distinct information.
- The inline state is available to assistive technology in the normal state; in the warning state, the live warning message supplies the accessible status.
- The uncategorized copy states that the current-period amount has not been included in today's budget. It does not change calculation semantics.
- Under a simultaneous over-budget and uncategorized mobile mock, Radar measured approximately 581px tall. The long notice wrapped within its 357px content width with no horizontal overflow; it remained fully reachable above the nav.

## 修改項目

- This final review added only this document; no code was changed during this review.
- Earlier minimal hardening now present in the reviewed files: Quick Add controls use the existing small-radius token, GoalPyramid's h2 explicitly uses the primary text token, and the nested Quick Add category button receives the same small-radius token.

## 未處理 Legacy Token

- `index.css` continues to use legacy variables for global page background/text/border and base heading styles.
- `AppShell.css` continues to consume legacy variables for utility navigation, bottom navigation, modal surfaces, borders, and active tab accent. This was intentionally not migrated under the stated scope.
- No forced migration of legacy aliases was performed; current values visually align with the corresponding semantic colors.

## 後續建議

- Keep AppShell legacy-token migration as a separate task, after reviewing non-Dashboard routes and modal/navigation contrast.
- Keep `--font-number-family` unused unless product/design explicitly wants a different numeric typeface; `tabular-nums` already addresses alignment.
- Preserve the current safe-area layout variables and recheck page-end scroll clearance if the fixed navigation height or safe-area behavior changes.

## Build

`npm run build` succeeded. Vite emitted the existing warning that a minified JavaScript chunk exceeds 500 kB.