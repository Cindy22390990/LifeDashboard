# Added Tokens

Added semantic design tokens to the existing `:root` light-theme selector and matching overrides to the existing `prefers-color-scheme: dark` selector.

- Surface: `--color-canvas`, `--color-surface`, `--color-surface-elevated`
- Text: `--color-text-primary`, `--color-text-secondary`, `--color-text-muted`
- Border: `--color-border-subtle`
- Radius: `--radius-sm: 4px`, `--radius-md: 8px`
- Spacing: `--space-1: 4px`, `--space-2: 8px`, `--space-3: 12px`, `--space-4: 16px`, `--space-5: 24px`, `--space-6: 32px`, `--space-7: 48px`
- Status: `--color-success`, `--color-warning`, `--color-danger`
- Goal tiers: `--color-tier-safety`, `--color-tier-growth`, `--color-tier-dream`
- Number font: `--font-number-family: var(--mono)`; reuses the existing monospace stack and does not load an external font

Existing `--bg`, `--text`, `--border`, and other legacy variables remain unchanged for backward compatibility. No component CSS was switched to the new tokens.

# Changed Files

- [index.css](../LifeDashboard.Web/src/index.css): added light- and dark-theme token definitions.
- [design-token-phase1-result.md](design-token-phase1-result.md): documented Phase 1 changes and migration guidance.

# Visual Impact

- Screen appearance: unchanged. Existing components do not reference the newly added token names, so their computed styles continue to come from the existing legacy variables and component-local declarations.
- Component behavior: unchanged. No React/TSX, component hierarchy, data flow, or calculation was modified.
- Dark mode: existing behavior is unchanged. The new tokens have dark-mode values, but no current component consumes them yet; dark-mode visual migration belongs to a later phase.

# Next Phase

Phase 2 should migrate one component stylesheet at a time, without changing component structure or data behavior:

1. Replace `DashboardPage.css` local surface/text/border aliases and hard-coded colors, radius, and repeated spacing values with semantic tokens.
2. Replace `GoalPyramid.css` surface, text, border, tier accent, progress, and completed-state values with the corresponding tokens.
3. Replace `BudgetRadarPanel.css` card surface, border, text, spacing, radius, and warning-state values with tokens.
4. After each stylesheet, compare desktop/mobile rendering to ensure intended spacing and geometry remain stable; avoid simultaneously changing token values and component rules.

Phase 3 should verify light and dark themes for all three dashboard areas, including text contrast, status/tier colors, progress tracks, warning surfaces, and card surfaces. Keep the existing legacy variables until all consumers have been migrated and verified.