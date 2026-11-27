# ADR-006: V4 Server State Management Strategy

> **Document Path**: `docs/DashboardV4ADR006-StateManagement.md`
>
> **Status**: Accepted
>
> **Scope**: `src/presentation/components/dashboard-v4/**` 與 V4 Pages

---

## 1. Context

先前 ADR-004 保留了「獨立 V4 Hooks / Stores 或 TanStack Query」的彈性，但在實作階段留下雙軌模糊空間，極易導致開發者或 AI 工具複製 V3 習慣，建立 `homeStore.ts`、`transactionStore.ts`、`planStore.ts`、`meStore.ts`，引發 **Zustand Store 爆炸** 與快取同步失控（例如新增一筆交易後，需手動逐一更新 3 個 Store），最終回退為 V3.5 架構。

---

## 2. Decision

正式將 V4 的狀態管理切分為 **Server State** 與 **Client UI State**，嚴格分治：

### 2.1 Server State：一律採用 TanStack Query（React Query）
* **資料流**：`API Client → TanStack Query (useQuery / useMutation) → Component`
* **嚴格禁止**：**禁止使用 Zustand 保存任何後端回傳資料**。
  * ❌ 禁止：`dashboardStore.homeData`、`dashboardStore.transactions`、`dashboardStore.accounts`、`meStore.assets`。
* **快取失效策略（Cache Invalidation）**：
  * 透過統一的 Query Keys 管理（如 `['v4', 'home']`、`['transactions', filter]`、`['me', 'assets']`、`['budgets', 'current']`、`['goals', 'summary']`）。
  * 當 `TransactionBottomSheet` 執行 `useMutation` 建立交易成功後，直接呼叫 `queryClient.invalidateQueries()` 同步失效受影響的查詢，由框架在背景自動重新抓取。

### 2.2 Client UI State：Zustand 僅限跨元件純 UI 狀態
* **允許清單**：
  * `privacyStore`（例如：`isAmountHidden`）
  * `bottomSheetStore`（例如：`isOpen`、`defaultDate`、`mode`）
  * `languageStore`（例如：`locale`）
* **禁止事項**：上述 UI Store 內不得夾帶任何 API Response DTO 或資料抓取邏輯。

### 2.3 CI 邊界強制防護
* 新增 **ESLint Boundary Rule**（如 `no-restricted-imports`）：
  * 嚴格禁止 `dashboard-v4/**` import 任何 `dashboard-v3/**` 檔案或 `dashboardV3Store`。
  * 於 PR / Build 階段自動攔截，不依賴人工 Code Review。