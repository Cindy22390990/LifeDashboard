# Migrated File

- [DashboardPage.css](../LifeDashboard.Web/src/presentation/pages/DashboardPage.css)

No other stylesheet or component file was changed.

# Token Replacement Summary

## Color Replacements

- Dashboard surface, primary text, muted text, and border aliases now reference `--color-surface`, `--color-text-primary`, `--color-text-muted`, and `--color-border-subtle`.
- Feedback styling now uses `--color-danger` with the semantic surface/border tokens for its tinted background and border.
- The overview's fixed green-tinted background was replaced with `--color-surface`; no Goal tier color was introduced into DashboardPage.
- Quick Add radio accent and submit button background now use `--color-success`. The button foreground uses `--color-surface` to retain contrast with the success surface in light and dark themes.
- The component's local color aliases now use semantic token names instead of legacy global variables.

## Spacing Replacements

- `4px` → `var(--space-1)`
- `8px` → `var(--space-2)`
- `12px` → `var(--space-3)`
- `16px` → `var(--space-4)`
- `24px` → `var(--space-5)`
- `32px` → `var(--space-6)`
- `48px` had no matching declaration in this stylesheet.
- Other values, including 5px, 6px, 10px, 14px, 15px, 18px, 20px, 22px, 28px, and 36px, remain unchanged because they are not part of the specified mapping.

## Radius Replacements

- `8px` → `var(--radius-md)`
- No `4px` radius declaration was present. Existing 6px control/button radii remain unchanged.
- Radius values greater than 8px and special button radii were not changed.

# Regression Check

- Layout: grid/flex declarations, widths, heights, alignment, positions, and spacing relationships were not changed; only explicitly mapped spacing values now resolve through their equivalent tokens.
- Responsive: breakpoints and responsive layout rules are unchanged.
- Dark mode: semantic colors now resolve through the Phase 1 light/dark token scopes. DashboardPage colors therefore follow the dark token palette rather than fixed light-theme values.
- Visual note: the overview background no longer has its previous hard-coded green tint; it now uses the standard surface token. No tier colors were used.
- Validation: `npm run build` succeeded. Vite reported the existing warning for a minified JavaScript chunk larger than 500 kB.

# Remaining Work

- Phase 2B: migrate `BudgetRadarPanel.css` to semantic surface, text, border, spacing, radius, and status tokens.
- Phase 2C: migrate `GoalPyramid.css` to semantic surface, text, border, spacing, radius, status, and Goal tier tokens.