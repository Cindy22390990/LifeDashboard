# Current Style Problems

## Background and Surface

- [index.css](../LifeDashboard.Web/src/index.css) defines legacy `--bg` as white in light mode and dark charcoal in dark mode.
- [AppShell.css](../LifeDashboard.Web/src/AppShell.css) uses `--bg` for the bottom navigation and modal. These surfaces follow the global theme, but have no explicit distinction between base canvas, regular surface, and elevated surface.
- [DashboardPage.css](../LifeDashboard.Web/src/presentation/pages/DashboardPage.css) aliases `--bg` into `--dashboard-surface`, then tints the overview with a hard-coded green using `color-mix()`. Quick Add uses the plain surface. Error feedback also uses fixed red colors.
- [GoalPyramid.css](../LifeDashboard.Web/src/presentation/components/dashboard/GoalPyramid.css) aliases global `--bg` to `--goal-surface`, so its tier surfaces follow light/dark mode.
- [BudgetRadarPanel.css](../LifeDashboard.Web/src/presentation/components/BudgetRadarPanel.css) hard-codes card backgrounds to white. This conflicts with the global dark surface and the theme-aware Dashboard/Goal surfaces.

## Colors and Status

- Hard-coded Dashboard colors include feedback red (`#e8c7c2`, `#8e3d35`, `#b6493d`), overview tint (`#77917d`), radio accent (`#3d7656`), and primary button green (`#315f48`).
- GoalPyramid hard-codes the progress track (`#e8eee9`), completion green (`#2f7252`), tier accents (`#327456`, `#36748b`, `#a05269`), plus the progress fallback green. Dark mode only overrides the progress track and completion green; tier accents remain unchanged.
- BudgetRadar hard-codes card border/text/background (`#eceaf1`, `#777487`, `#282536`, `#fff`) and warning/uncategorized states (`#f1bcbc`, `#fff3f3`, `#a52c2c`, `#f0d99b`, `#fff9e8`, `#765812`). These remain light-theme colors when the global theme switches to dark.
- Global `--accent` is purple, while GoalPyramid and Dashboard actions use greens and tier colors. The product-level accent and semantic tier/status colors currently have no documented relationship.

## Borders, Radius, and Cards

- Global `--border` is `#e5e4e7` in light mode and `#2e303a` in dark mode. Dashboard and Goal aliases use it, but BudgetRadar has a separate fixed border color.
- Card/surface radii vary: Dashboard summary/Quick Add use 8px; Quick Add controls use 6px; Goal tiers use 8px and status pills use 999px; BudgetRadar cards use 10px; AppShell navigation uses 16px and the modal uses 12px.
- Dashboard overview and Quick Add use bordered surfaces; Goal tiers have a top tier-color border; Goal cards are unframed rows inside the tier; BudgetRadar uses a grid of individual white bordered cards. The components therefore use three different card grammars.
- Border definitions are repeated locally across Dashboard, Goal, BudgetRadar, and AppShell rather than consistently using one surface/border recipe.

## Spacing

- There is no shared spacing scale. AppShell uses 2, 8, 12, 16, and 32px values; Dashboard uses 3, 4, 5, 6, 8, 10, 12, 14, 15, 16, 18, 20, 22, 24, 32, and 36px; Goal uses 3, 4, 6, 7, 8, 10, 12, 14, 16, 18, and 22px; BudgetRadar uses 8, 12, and 16px.
- Repeated concepts such as component gaps, card padding, section spacing, and compact label spacing use different values across the three dashboard sections.
- AppShell's bottom-navigation variables are layout-specific and should stay separate from the visual spacing scale unless they become a shared layout token.

## Typography and Number Display

- Global text uses system UI/Segoe UI/Roboto; global `h1` is 56px, then Dashboard overrides its own heading to 32px (27px mobile). Dashboard section headings are 21px; GoalPyramid's title is 24px, its tier title 18px, and Goal names 16px. BudgetRadar values use 22px. These values are locally specified rather than derived from a common type scale.
- Muted labels vary: Dashboard resolves to global `--text` (`#6b6375` light / `#9ca3af` dark), Goal resolves to the same token, while BudgetRadar fixes labels at `#777487`.
- Goal and Dashboard account/overview numbers use `font-variant-numeric: tabular-nums`; BudgetRadar values do not explicitly opt in. Goal amounts show two decimal places, while Dashboard overview/account amounts show zero decimals and BudgetRadar's formatting uses its existing two-decimal DTO presentation. The display differences may be intentional by context but are not yet a shared convention.

## Dark Mode Support

- `index.css` changes legacy global tokens under `prefers-color-scheme: dark`; AppShell's nav/modal surfaces inherit these tokens.
- DashboardPage mostly follows global text/border/surface aliases, but its green-tinted overview, feedback colors, control colors, and primary button colors are not theme tokens and are not dark-mode adjusted.
- GoalPyramid surface and primary/muted text follow global aliases. Its custom tier accents remain fixed; only track and completion colors get a dark override.
- BudgetRadar cards, labels, values, borders, and warning surfaces are fixed light colors, so they do not support the global dark palette reliably.
- Theme support is tied to `prefers-color-scheme`; components do not have a shared semantic token layer that can be audited for both themes.

# Proposed Tokens

Use semantic tokens as the shared contract for AppShell and dashboard surfaces. Values below are a starting light-theme palette; dark-theme values should preserve the same semantics rather than reusing the light hex values.

| Token | Suggested light value | Usage |
|---|---|---|
| `--canvas` | `#f5f7f5` | Application/page background |
| `--surface` | `#ffffff` | Standard panel and card surface |
| `--surface-elevated` | `#ffffff` | Navigation, modal, or content requiring elevated separation; differentiate through border/shadow rather than another white shade |
| `--text-primary` | `#202a24` | Main headings, goal names, primary numbers |
| `--text-secondary` | `#526158` | Supporting values and secondary labels |
| `--text-muted` | `#77847b` | Eyebrows, hints, tertiary metadata |
| `--border-subtle` | `#dfe6e0` | Card, divider, and input borders |
| `--radius-sm` | `6px` | Inputs, compact controls, small status shapes |
| `--radius-md` | `8px` | Dashboard panels and tier surfaces |
| `--space-1` | `4px` | Tight inline separation |
| `--space-2` | `8px` | Compact component gap |
| `--space-3` | `12px` | Control/group spacing |
| `--space-4` | `16px` | Standard component spacing and card padding |
| `--space-5` | `24px` | Section interior spacing |
| `--space-6` | `32px` | Major section spacing |
| `--status-success` | `#2f7252` | Completed goal/success state |
| `--status-warning` | `#956b1e` | Budget warning state |
| `--accent-safety` | `#327456` | Goal Pyramid Safety tier |
| `--accent-growth` | `#36748b` | Goal Pyramid Growth tier |
| `--accent-dream` | `#a05269` | Goal Pyramid Dream tier |

Additional guidelines:

- Add dark-theme semantic values for canvas, surfaces, text, borders, status colors, and tier accents in the same token layer. Validate contrast rather than mechanically inverting colors.
- Keep the requested token set small. If a distinct danger/error state is needed, add a semantic error token in a later decision rather than overloading `--status-warning`.
- Use a shared number convention: tabular numerals for financial values; decide separately whether totals and balances round to whole currency units while goal progress keeps cents.
- Retain legacy variables temporarily as aliases during migration (for example, map `--bg` to `--canvas`, `--text-h` to `--text-primary`, and `--border` to `--border-subtle`) to avoid a broad unrelated stylesheet rewrite.
- Keep AppShell's `--bottom-nav-height`, `--bottom-nav-inset`, and `--bottom-nav-clearance` as layout tokens; they describe geometry, not visual theme.

# Migration Plan

## Phase 1: Add Tokens Without Visual Changes

- Define the semantic tokens in `index.css` for light and dark color schemes, plus the shared radius and spacing scale.
- Map existing global variables to the new tokens as aliases so current consumers retain their present computed styles.
- Do not replace component declarations in this phase; compare computed styles before and after to confirm there is no visual change.

## Phase 2: Migrate Dashboard Components to Tokens

- Replace local DashboardPage aliases and hard-coded colors, radii, and repeated spacing values with semantic tokens.
- Migrate GoalPyramid's surface, text, border, tier accents, progress track, and completion state to the shared tokens.
- Migrate BudgetRadar card surfaces, borders, muted labels, and warning/uncategorized states; align its card radius and padding with the shared surface rule.
- Keep component hierarchy, props, calculations, API data, and dashboard ordering unchanged. Review desktop and mobile layouts after visual substitutions.

## Phase 3: Dark Mode Validation

- Verify DashboardPage, GoalPyramid, BudgetRadarPanel, navigation, and modals in both light and dark color schemes.
- Check readable contrast for primary/muted text, tier accents, completed states, warning states, progress tracks, and borders.
- Check that cards do not retain white backgrounds in dark mode and that focus/active controls remain distinguishable.
- Treat visual differences as intentional only when documented by semantic token usage; avoid component-specific dark-mode exceptions where a shared token can express the state.