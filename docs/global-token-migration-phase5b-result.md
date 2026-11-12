# Phase 5B Low Risk Token Migration Result

## Modified Files

- [index.css](../LifeDashboard.Web/src/index.css)
- [AppShell.css](../LifeDashboard.Web/src/AppShell.css)
- [global-token-migration-phase5b-result.md](global-token-migration-phase5b-result.md)

No React/TSX, API, DTO, backend, layout, spacing, breakpoint, or migration-branch changes were made.

## Token Changes

- Root canvas background `var(--bg)` → `var(--color-canvas)`.
- Root `#root` inline border `var(--border)` → `var(--color-border-subtle)`.
- Global `h1, h2` heading foreground `var(--text-h)` → `var(--color-text-primary)`.
- AppShell utility divider, bottom-navigation border, and modal-header divider `var(--border)` → `var(--color-border-subtle)`.
- Bottom-navigation and modal surfaces `var(--bg)` → `var(--color-surface)`.
- Inactive bottom-tab text `var(--text)` → `var(--color-text-muted)`; light/dark values are equal to the existing legacy token values.

Legacy definitions remain available. No alias was removed.

## Deferred Tokens

- `--text` remains for global body text and modal body text because its role is not consistently primary, secondary, or muted.
- `--accent`, `--accent-bg`, and `--accent-border` remain unchanged. Active navigation still uses the existing accent family; the App.css counter focus/hover uses are in a stylesheet with no active import. Accent/brand/interactive semantics remain a product decision.
- `App.css` was not modified; it is not imported by the active app path.
- `--shadow` and the modal backdrop's fixed translucent black overlay are elevation/overlay treatments, not direct replacements for the migrated color roles.

## Regression Result

- `npm run build` succeeded. Vite emitted the existing warning for a minified JavaScript chunk larger than 500 kB.
- Browser-only Dashboard fixtures were checked at 1280px and 390px in light and dark schemes before and after the token substitutions.
- Computed canvas/surface, heading text, inactive-nav text, border, and active-tab accent values matched before and after in both themes.
- No horizontal overflow was observed. Fixed navigation remained in place; Quick Add could scroll to 64px (desktop) / 65px (mobile) above the nav, and the final Account row could be positioned fully above it.
- Dashboard section order and layout measurements were unchanged.

## Remaining Risks

- Global body `--text` and modal body `--text` need per-use semantic classification before migration.
- Accent semantics need product/design confirmation before mapping active navigation or focus states.
- The dormant `App.css` should be confirmed as unused before removal or reactivation.
- Legacy token definitions should remain until all active consumers, including non-Dashboard routes, are inventoried and browser-checked.