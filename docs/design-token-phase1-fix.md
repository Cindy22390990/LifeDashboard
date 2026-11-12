# Problem

The Phase 1 dark semantic color values were placed inside the global `:root` light-theme rule in `index.css`, after the light values. Since later declarations win in the same selector, those dark values overrode the light token values in every color scheme. The existing `prefers-color-scheme: dark` selector only overrode legacy variables, not the new semantic color tokens.

# Fix

- Kept the light semantic color values in the existing global `:root` selector.
- Moved the semantic dark color overrides into the nested `:root` selector inside the existing `@media (prefers-color-scheme: dark)` rule.
- Kept spacing, radius, and `--font-number-family` tokens declared once in global `:root`; these tokens do not require theme-specific overrides.
- Preserved all legacy variables and current component CSS usage. No component stylesheet was migrated.

# Impact

- Current screen appearance: unchanged. Existing components do not consume the semantic token names, and the legacy variables and their values were not modified.
- Component behavior: unchanged. No React/TSX, component hierarchy, data flow, or calculation was changed.
- Dark mode: existing component appearance is unchanged; semantic tokens now resolve to dark values only when the dark color-scheme media query matches. This prepares correct token behavior for future migration without changing current component styles.

Validation: `npm run build` succeeded. Vite emitted the existing warning that a minified JavaScript chunk exceeds 500 kB.

Phase 2 DashboardPage CSS migration was not started.