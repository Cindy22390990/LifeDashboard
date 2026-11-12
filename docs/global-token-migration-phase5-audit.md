# Phase 5 Global Token Migration Audit

## Current State

- Phase 1 semantic tokens exist in `index.css`, including canvas/surface, text, border, status, tier, radius, spacing, and number-font tokens, with dark-mode overrides.
- Active DashboardPage, GoalPyramid, and BudgetRadar styles use semantic `--color-*` values for their surfaces, text, borders, and statuses. DashboardPage's `--dashboard-*` variables are local aliases to semantic tokens.
- GoalPyramid tier colors are semantic tier tokens and limited to the small tier indicator/progress accents. Financial values use `font-variant-numeric: tabular-nums`.
- Legacy color variables remain in global base styles and the active AppShell. The old `App.css` stylesheet contains legacy references but is not imported by the application entry or active App component path.
- Exact search found 33 occurrences of the requested legacy variable names (`--bg`, `--text`, `--text-h`, `--border`, `--accent`) in source CSS: 15 in `index.css`, 10 in `AppShell.css`, and 8 in the unimported `App.css`. The related `--accent-bg` / `--accent-border` family adds 7 references/definitions and should be considered with accent semantics.

## Legacy Token Inventory

| File | Legacy Token | Purpose | Replacement Candidate | Risk |
|---|---|---|---|---|
| `src/index.css` | `--bg` | Root/page canvas background; currently switches light/dark | `--color-canvas` for root canvas; `--color-surface` only for actual panels | Low if computed light/dark values are preserved; broad global impact requires visual regression checks |
| `src/index.css` | `--text` | Default body text and muted/supporting text | `--color-text-secondary` or `--color-text-muted`, selected by semantic role | Medium: the current value is reused globally, but its intended hierarchy is ambiguous |
| `src/index.css` | `--text-h` | Global h1/h2 and code/counter foreground | `--color-text-primary` | Low to medium: heading contrast is broad; code and counters may have separate surface requirements |
| `src/index.css` | `--border` | Root inline border and global utility/shell outlines | `--color-border-subtle` | Low if preserving light/dark values; affects the whole application shell |
| `src/index.css` | `--accent` | Active/interactive accent, including dark-mode purple shift | No direct one-to-one mapping in the current semantic set; requires a product decision about brand vs. interactive state | High if replaced with success or Goal tier colors; those meanings are not equivalent |
| `src/index.css` | `--accent-bg`, `--accent-border` | Hover/focus/active accent variants | Requires the same brand/interactive decision as `--accent`; do not map to status/tier tokens | Medium to high: focus and active-state visibility can regress |
| `src/AppShell.css` | `--bg` | Bottom navigation and modal surfaces | `--surface` or `--surface-elevated`, based on intended elevation | Low to medium: modal/nav contrast and dark palette should be checked |
| `src/AppShell.css` | `--text` | Navigation and modal body text | `--color-text-secondary` or `--color-text-muted`, by role | Medium: active/inactive navigation hierarchy should remain clear |
| `src/AppShell.css` | `--border` | Utility navigation, nav frame, modal header | `--color-border-subtle` | Low if token value remains equivalent |
| `src/AppShell.css` | `--accent`, `--accent-bg` | Active bottom tab foreground/background | No direct mapping until interactive/brand semantics are decided | High if conflated with success or tier identity |
| `src/App.css` | `--accent`, `--accent-bg`, `--accent-border` | Old counter/focus/hover treatment | No migration candidate until the stylesheet's intended ownership is confirmed | Low runtime risk while unimported; cleanup/importing it could introduce broad visual changes |
| `src/App.css` | `--border`, `--text-h` | Old starter/demo sections, separators, and link text | `--color-border-subtle`, `--color-text-primary` if the stylesheet is intentionally activated | Low runtime risk while unimported; confirm whether it is dead code before migrating |
| Dashboard component/page styles | None of the exact legacy variables directly | Dashboard-specific surfaces/text/status are already semantic-token based | No migration needed for these files in this audit | Low; keep checking global inherited heading rules |

### Non-Migration Candidates

- `--shadow` describes elevation, not a color role; retain it or review it in a separate elevation-token decision.
- The modal backdrop's `rgb(0 0 0 / 45%)` is an overlay opacity treatment, not a surface/text token. Preserve its behavior unless overlay semantics are separately designed.
- Goal tier colors must remain tied to Safety/Growth/Dream semantics; do not replace them with generic accent, success, or warning colors.
- Chart/visualization series colors, if introduced in other screens, need a chart palette decision rather than automatic surface/status substitution.

## Replacement Mapping Proposal

| Legacy usage | Candidate | Qualification |
|---|---|---|
| Root `--bg` | `--color-canvas` | Use for application/page canvas; panels should use `--color-surface` |
| Panel/modal/nav `--bg` | `--color-surface` or `--color-surface-elevated` | Choose by surface elevation and preserve contrast |
| `--text-h` | `--color-text-primary` | Suitable for headings and primary foreground; check code/counter consumers |
| `--text` | `--color-text-secondary` / `--color-text-muted` | Must classify each consumer; do not replace globally with one token without checking hierarchy |
| `--border` | `--color-border-subtle` | Preserve existing light/dark contrast |
| `--accent`, `--accent-bg`, `--accent-border` | No automatic mapping | First decide whether this is brand, interactive/focus, or active-navigation color. Do not map to `--color-success`, `--color-warning`, or Goal tier tokens by resemblance |

## Risk Assessment

- **Largest active migration risk: `index.css`.** Its global variables affect root canvas, body defaults, headings, and the shell. A broad replacement can change routes beyond Dashboard and alter inherited heading colors.
- **Semantic ambiguity: `--text`.** Its current value serves defaults that may be secondary or muted; each use needs classification.
- **Interactive-state risk: accent family.** `--accent` is purple in light mode and purple in dark mode with different values, while the semantic token set has no dedicated brand/action token. Substituting success or tier colors would change meaning and interaction cues.
- **AppShell risk: navigation and modal consumers.** Active tab, inactive tab, frame, and modal surfaces have different roles even where they currently share `--bg`, `--text`, or `--border`.
- **Dormant CSS risk: `App.css`.** It is not imported in the active app path. Migrating or reattaching it could activate demo styles and cause an unrelated visual change.
- **Browser baseline:** presentation-only mocked Dashboard data was checked at 1280px and 390px in light/dark themes. No horizontal overflow was observed. Root/page and Dashboard surfaces resolve to light surfaces in light mode and dark surfaces in dark mode; primary text switches to light text in dark mode. At page end, Quick Add remains 64–65px above the fixed nav, and the last Account row can be scrolled fully above it. No fixed white Dashboard card or fixed black Dashboard text was observed.
- **Active legacy behavior:** AppShell continues to follow the legacy global light/dark variables. The browser baseline showed no current theme mismatch, so migration is a consistency/maintainability task rather than an urgent rendering defect.

## Migration Order Proposal

1. **Token consumer inventory and semantic decisions:** confirm canvas vs. surface/elevated usage, secondary vs. muted text roles, and the product meaning of the accent family. Keep legacy definitions as aliases during transition.
2. **Global base (`index.css`):** migrate declarations by semantic role while preserving exact computed values in both themes; validate headings, body text, root canvas, code, and focus states.
3. **AppShell (`AppShell.css`):** migrate utility navigation, active/inactive tabs, modal surfaces, borders, and overlay-adjacent styles. Treat interactive accent as a separate approved choice.
4. **Remaining active pages/components:** search all `src` CSS for direct legacy references and migrate by role, then verify routes outside Dashboard.
5. **Dormant `App.css`:** confirm whether it is intentionally unused. Prefer removal or explicit ownership decision over a blind migration.
6. **Legacy retirement:** only remove `--bg`, `--text`, `--text-h`, `--border`, and accent-family variables after all active consumers are migrated and light/dark regression checks pass.

No migration was started as part of this audit.