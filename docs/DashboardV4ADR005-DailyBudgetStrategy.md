# ADR-005: Daily Budget Accumulation Strategy

> **Document Path**: `docs/DashboardV4ADR005-DailyBudgetStrategy.md`
>
> **Status**: Accepted
>
> **Owner**: Backend `BudgetService`（Frontend: Only Display）

---

## 1. Context

Dashboard V4 Home 首屏的核心數字為「今日可花（`dailySpendingLimit` / `dailyRemaining`）」。若單純採用 `AvailableMonthlyBudget / RemainingDays`，當使用者昨天刻意省錢沒花時，隔天看到的可花額度卻幾乎沒有「累積獎勵感」，容易削弱日常節制消費的動機，也與 LifeDashboard 的核心理念——**「錢不是剩多少，而是分配後還剩多少自由」**——產生落差。

---

## 2. Options Considered

### Option A：每日固定額度（均攤制）
* **公式**：`remainingBudget / remainingDays`
* **優點**：公式單純、易於向使用者解釋。
* **缺點**：昨日未花完的金額被重新均攤至整個月份，缺乏即時正向回饋，不鼓勵節省累積。

### Option B：累積自由額度（推薦）
* **核心公式**：
  ```text
  RemainingFlexibleBudget
    = AvailableMonthlyBudget - SpentThisMonth - ReservedFutureCommitments

  TodayAvailable
    = (RemainingFlexibleBudget / RemainingDays) + PreviousUnusedDailyBudget
  ```
* **產品效果**：昨天省下 `$1,000`，今天「今日可花」直接反映增加的自由額度，強化正向儲蓄回饋。
* **優點**：完全貼合「分配後剩多少自由」的產品理念。
* **缺點**：後端計算需明確定義 `PreviousUnusedDailyBudget` 的基準，避免重複計算。

---

## 3. Decision

採用 **Option B（累積自由額度）** 作為 V4 `BudgetService` 的預設策略，並透過 **Strategy Pattern（`IDailyBudgetStrategy`）** 封裝，保留未來替換為「平日／週末權重」或「個人消費習慣」策略的擴充空間。

### 實作邊界與防呆規則
1. **防重複計算原則（Backend 實作檢核）**：`BudgetService` 實作 Option B 時，需確保「昨日未花完額度」的基準線與 `SpentThisMonth` 口徑一致（例如基準日額度固定以月初可支配額度除以當月天數計算，或明確區分「截至昨日累積結餘」與「當日額度」），避免同一筆未花金額在分子中被計算兩次。
2. **邊界條件**：
   * 未設定月預算時：`dailySpendingLimit = 0`、`dailyRemaining = -dailySpent`（或依 UI 狀態改顯「今日已花」）。
   * 當月最後一天：`RemainingDays` 最小鎖定為 `1`，避免除以零。
   * 當日超支：`dailyRemaining` 允許為負數，由前端以警示色呈現。
3. **單一計算責任**：Frontend 僅消費 API 回傳的 `dailySpendingLimit`、`dailySpent`、`dailyRemaining`，嚴禁於前端重算。

## 4. Addendum: `PreviousUnusedDailyBudget` Source Decision

為避免 Phase 2 實作 `BudgetService` 時重新引發架構爭論，明確定義 `PreviousUnusedDailyBudget` 的資料來源策略：

- [x] **Calculated dynamically（MVP 採用）**
- [ ] **Persisted Daily Snapshot（未來演進）**

### 決策理由
* **MVP 階段採動態即時計算**：以「月初截至昨日之基準累計額度 − 截至昨日累計實際支出」動態推算 `PreviousUnusedDailyBudget`，維持無狀態設計，**不新增 `DailyBudgetSnapshot` 資料表與每日排程 Job**，降低 Phase 2 Domain 與維運複雜度。
* **未來演進預留**：透過 `IDailyBudgetStrategy` 介面隔離；當未來導入「平日／週末動態權重」或「歷史每日結算報表」需求時，再切換為 `DailyBudgetSnapshot`（`Id`, `UserId`, `Date`, `AllocatedAmount`, `SpentAmount`, `UnusedAmount`）持久化實作，對上層 API Contract 零影響。