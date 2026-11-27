import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
  },
  {
    files: [
      'src/presentation/components/dashboard-v4/**/*.{ts,tsx}',
      'src/presentation/pages/HomePage.tsx',
      'src/presentation/pages/TransactionsPage.tsx',
      'src/presentation/pages/PlanPage.tsx',
      'src/presentation/pages/MePage.tsx',
      'src/api/dashboard-v4/**/*.{ts,tsx}',
    ],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            '**/dashboard-v3/**',
            '**/dashboardV3Store*',
          ],
        },
      ],
    },
  },
])
