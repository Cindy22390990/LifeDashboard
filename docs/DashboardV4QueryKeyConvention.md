# Dashboard V4 Query Key Convention

> **Document Path**: `docs/DashboardV4QueryKeyConvention.md`
>
> **Status**: Accepted
>
> **Reference**: `docs/DashboardV4ADR006-StateManagement.md`

---

## 1. Purpose
集中管理 Dashboard V4 所有 TanStack Query 的 `queryKey`，禁止在各 Hook 或 Component 中散落手寫字串陣列，確保快取失效（Invalidation）精準可控，避免半年後 Key 命名失控。

---

## 2. Centralized Location
所有 V4 Query Keys 統一存放於：

```text
src/api/queryKeys.ts
```

---

## 3. Key Structure

```typescript
export const dashboardV4Keys = {
  all: ['v4'] as const,
  home: () => [...dashboardV4Keys.all, 'home'] as const,
  transactions: {
    all: () => ['transactions'] as const,
    list: (filter: TransactionListFilter) => ['transactions', 'list', filter] as const,
    detail: (id: string) => ['transactions', 'detail', id] as const,
  },
  me: {
    assets: () => ['me', 'assets'] as const,
  },
  plan: {
    budget: (year?: number, month?: number) => ['budgets', 'current', { year, month }] as const,
    goals: () => ['goals', 'summary'] as const,
  },
};
```

---

## 4. Cache Invalidation Rules
* **新增／刪除交易成功（Mutation Success）**：
  * 必須失效：`dashboardV4Keys.home()`、`dashboardV4Keys.transactions.all()`、`dashboardV4Keys.me.assets()`。
  * 若 Budget / Goal 已啟用：同步失效 `dashboardV4Keys.plan.budget()`。
* **禁止事項**：禁止於 Component 內直接寫死 `queryClient.invalidateQueries({ queryKey: ['home'] })` 等未經 `queryKeys.ts` 匯出的陣列。