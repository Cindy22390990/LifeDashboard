# Phase 5A Global Token Migration Decision

## Current Legacy Token Analysis

The Phase 5 audit found 33 exact occurrences of `--bg`, `--text`, `--text-h`, `--border`, and `--accent` in source CSS: 15 in `index.css`, 10 in the active `AppShell.css`, and 8 in `App.css`. The related `--accent-bg` / `--accent-border` family contributes 7 additional definitions/usages. No migration was performed for this decision design.

`App.css` is not imported by the active app entry/component path. Its counter, demo sections, and focus/hover rules are dormant unless the stylesheet is explicitly loaded. The active runtime imports `index.css` and `AppShell.css`.

| Legacy Token | Current Usage | Candidate Token | Confidence | Decision |
|---|---|---|---|---|
| `--bg` | Root/page canvas in `index.css`; nav and modal surfaces in `AppShell.css` | `--color-canvas` for the root canvas; `--color-surface` or `--color-surface-elevated` for panels/nav/modal | High when classified by surface role | Direct migration candidate, but split by usage rather than replace all `--bg` references with one token |
| `--text` | Default body text; inactive nav and modal body in AppShell | `--color-text-secondary` or `--color-text-muted` | Medium | Migrate per consumer after deciding whether each label is secondary or muted; do not globally alias every use to one role |
| `--border` | Root inline border and AppShell utility/nav/modal separators | `--color-border-subtle` | High | Direct migration candidate, preserving current light/dark contrast values |
| `--text-h` | Global h1/h2 foreground; old counter/demo link text in `App.css` | `--color-text-primary` | High for headings; medium for dormant demo/code consumers | Direct candidate for active headings. Confirm text treatment for `App.css` only if that stylesheet is intentionally reactivated |
| `--accent` | Active bottom-tab foreground in AppShell; counter text/focus outline in dormant `App.css` | No current one-to-one token | Low | Do not replace yet. Confirm whether its product role is brand accent, active navigation, interactive action, or focus indicator |
| `--accent-bg` | Active-tab background in AppShell; counter background in dormant `App.css` | No current one-to-one token | Low | Keep associated with the accent decision; define interactive/selected surface semantics before migration |
| `--accent-border` | Counter hover border in dormant `App.css` | No current one-to-one token | Low | No active runtime consumer found. Retain while `App.css` ownership/import status remains unresolved |

## Proposed Mapping

### A. Direct Migration Candidates

- Root `--bg` → `--color-canvas`; panel-like `--bg` usages → `--color-surface` or `--color-surface-elevated` based on hierarchy.
- `--border` → `--color-border-subtle` for root, navigation, and modal separators.
- Active global heading `--text-h` → `--color-text-primary`.
- `--text` requires per-use mapping: body/supporting values may be `--color-text-secondary`; tertiary labels may be `--color-text-muted`.

These are candidates only. Phase 5B should preserve the current computed light/dark colors while introducing semantic references, and retain legacy aliases until all consumers are verified.

### B. Product Semantic Confirmation Required

- The active AppShell bottom tab currently uses `--accent` and `--accent-bg`. Its observable role is selected/active navigation, but the token name suggests a broader brand role. Product/design should decide whether active navigation shares the brand accent or needs its own interactive-state semantic.
- The dormant counter uses accent for foreground/focus outline, accent-bg for fill, and accent-border for hover. These roles may require distinct focus, hover, and selected-state semantics rather than a single generic accent token.
- No accent-family value should be substituted with `--color-success`, `--color-warning`, or Goal tier colors based only on hue similarity.

### C. Retain / Do Not Blindly Migrate

- `App.css` references are in a stylesheet with no active import. Keep them out of the active migration until its intended ownership is confirmed; consider cleanup or an explicit activation decision separately.
- `--shadow` describes elevation, and the modal backdrop's `rgb(0 0 0 / 45%)` describes overlay opacity. Neither is a direct replacement for a surface/text/border semantic token.
- Goal tier colors remain tied to Safety/Growth/Dream meanings; chart series colors, if present elsewhere, need a visualization palette decision.

## Accent Token Decision

Search results show active accent use in `AppShell.css` only on `.bottom-tab-active` text and background. There is no active `--accent` use on a primary action button or ordinary link in the checked import path. The `.counter` focus-visible and hover uses are in the unimported `App.css`.

Decision for Phase 5A: **freeze the accent family; do not replace or redefine it**. Before Phase 5B migrates accent consumers, product/design must decide:

1. Whether the purple accent represents brand identity globally or only interactive selection.
2. Whether selected navigation, keyboard focus, hover borders, and button actions should share a role or use distinct roles.
3. Whether `App.css` is dead template styling or is expected to be reintroduced.

## Migration Risk

### `index.css`

- It defines and consumes the legacy root variables, sets the page canvas, base body color, and global heading colors. A change affects all routes, not only Dashboard.
- `--text` is ambiguous and global heading/code consumers of `--text-h` may need different semantic roles.
- Dark mode is implemented by `prefers-color-scheme` overrides. Migration must preserve both light and dark computed values; accidental inheritance or missing override can create unreadable text/surfaces.
- Risk: **high blast radius, manageable with exact-value aliases and route-level visual regression checks**.

### `AppShell.css`

- `--bg`, `--text`, and `--border` serve utility navigation, fixed bottom navigation, modal surfaces, and modal headers. `--accent`/`--accent-bg` represent active navigation.
- Navigation and modal visibility, active-tab contrast, fixed-nav safe-area behavior, and modal layering must remain unchanged.
- Risk: **medium-high interaction and contrast risk**. AppShell migration should follow the global base mapping and be verified on both mobile and desktop, in light and dark themes.

## Execution Plan

### Phase 5B: High-Confidence Base Mapping

- Add no token as part of this decision; use only the already existing semantic tokens where the role is unambiguous.
- Migrate root canvas, border separators, and active global heading foreground in `index.css`, preserving exact current light/dark values.
- Resolve each `--text` usage by role instead of applying a blanket replacement.
- Keep old variables as aliases during the transition; do not remove definitions in this phase.

### Phase 5C: Active Component and Shell Validation

- Migrate AppShell surfaces, separators, and text after Phase 5B.
- Migrate active navigation accent only after product/design confirms its semantic role.
- Verify nav active/focus states, modal surface and header, and bottom-nav safe-area clearance at desktop/mobile sizes in light/dark themes.
- Confirm the Dashboard and non-Dashboard routes still have equivalent computed colors and readable contrast.

### Phase 5D: Legacy Cleanup

- Search all active source for legacy variable definitions and references after migration.
- Confirm whether `App.css` should be removed, migrated, or intentionally retained before changing its state.
- Remove legacy aliases only when no active consumer remains and regression checks pass. Keep `--shadow`, overlay opacity, and any separately approved brand/chart semantics outside the color-token cleanup unless they receive their own decision.

No migration was started as part of this decision document.