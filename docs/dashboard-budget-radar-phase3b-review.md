# Budget Radar Phase 3B Review

## 現況分析

### TodayRemaining 狀態邏輯

- [BudgetRadarPanel](../LifeDashboard.Web/src/presentation/components/BudgetRadarPanel.tsx) 將 `todayRemaining < 0` 視為 warning，其餘（包含剛好為 0）視為 success。
- [BudgetSnapshot](../LifeDashboard.Domain/ValueObjects/BudgetSnapshot.cs) 的公式是 `TodayRemaining = DailySafeLimit - TodayLivingExpense`。`DailySafeLimit` 是薪資週期剩餘生活預算除以剩餘天數；負值依規格保留。
- 因此警示能辨認「今日生活支出已超過今日安全額度」，但不能辨認「即將用完」或「額度剛好歸零」的風險。TodayRemaining 為正數就一律綠色，沒有 near-limit 狀態或比例提示。
- `TodaySpent` 對應 `TodayLivingExpense`，不是所有交易支出或帳戶現金流。Goal Purchase、Transfer、Responsibility、未分類支出等依既有領域規則不一定包含在這個數字內。

### Dashboard 使用流程順序

目前 [DashboardPage](../LifeDashboard.Web/src/presentation/pages/DashboardPage.tsx) 由上而下為：頁面標題 → 財務總覽（帳戶餘額總和）→ Goal Pyramid → Budget Radar → Account Overview → Quick Add。

這是偏財務規劃的順序：資產概況與人生目標先出現，今日預算和記帳操作較後面。若使用者主要每天記帳與控制當日支出，最相關的 Budget Radar 和 Quick Add 都需要捲過目標、預算/帳戶區塊才能到達，與日常高頻流程不完全吻合。

### 資訊重複與缺口

- `DailySafeLimit` 與 `TodayRemaining` 並非相同指標，但後者直接由前者減去 `TodaySpent` 得出；同時顯示三者可讓使用者理解額度如何變化，也可能讓人把兩個額度金額誤認為重複預算。應以標籤/輔助說明呈現「每日額度、今日已用、今日剩餘」的關係，而不是任意移除欄位。
- `TotalLivingBudget` 是薪資週期總額，與每日指標時間尺度不同。DTO 沒有回傳週期起迄日期、週期已用生活費或剩餘生活預算，使用者不易看出總額是何期間或離下一週期還有多久。
- `UncategorizedExpenseAmount` 以提醒呈現，但依目前 BudgetSnapshot 規則，未分類支出不扣除 `RemainingLivingBudget`。因此畫面上的 TodayRemaining 可能高於將未分類交易補分類後的生活預算結果；提醒應說明此數字尚未納入，而不能暗示已從安全額度扣除。
- Goal Purchase 支出另行分類，不扣減 Living budget，現有 Radar DTO 也未呈現其金額。使用者若把 Radar 解讀為全部可花現金，可能忽略這項不同預算邊界。
- 五個欄位都保留有用途；主要問題是缺少指標間的時間尺度與計算口徑提示，不是單純欄位過多。

## 風險

- 僅以負值作為風險門檻會延遲警示：TodayRemaining 很接近 0 時仍呈 success，等於零也仍呈 success；但若 `DailySafeLimit` 為 0 或負值，單靠 TodayRemaining 顏色也難以表達週期預算本身已不足。
- TodaySpent/TodayRemaining 是 Living 類別口徑，不等於全交易支出或帳戶餘額變化；使用者可能用錯口徑作消費決策。
- 未分類金額獨立顯示卻不影響預算計算，若不解釋可能形成「可用餘額被高估」的認知風險。
- 頁面現行順序讓 Quick Add 位於長頁末端，增加日常記帳操作成本；反過來若把 Goal Pyramid 放得太後，也可能削弱人生目標作為產品主軸的價值。
- 現有 DTO 不提供「接近額度」的產品門檻、週期剩餘額或交易細節；新增更強風險判斷前需確認定義，不應由 UI 任意推導商業規則。

## 建議方案 A：日常記帳優先

建議順序：Header → 今日 Budget Radar → Quick Add → Goal Pyramid → 財務總覽/帳戶查核。

- TodayRemaining 成為首屏決策指標；今日支出與每日安全額度緊鄰呈現，讓使用者知道剩餘額度的來源。
- Quick Add 靠近今日預算，形成「查看今日狀態 → 記錄交易」的短流程。
- Goal Pyramid 和財務規劃資訊仍保留，但往後排列。
- 適合每日頻繁記帳、希望先控制當天支出的使用者。
- 代價是 Dashboard 的第一印象會由人生規劃轉為日常控支出，與現行 Goal Pyramid 的主視覺定位不同。

## 建議方案 B：財務規劃優先

維持或微調現行順序：Header → 財務總覽 → Goal Pyramid → Budget Radar → Account Overview → Quick Add。

- 保留資產與人生目標作為 Dashboard 的主要敘事，適合定期檢視財務規劃。
- 今日預算仍可作為第二階段的每日狀態查核，但需接受日常記帳入口較深。
- 可在視覺上讓 TodayRemaining 為 Radar 的單一主指標，並補充計算口徑與未分類支出影響，不必先改資料結構。
- 適合目標管理、資產規劃是主要使用情境的使用者。

若產品定位仍是「高質感生活財務 App + 人生目標金字塔」，建議先採 B，避免在沒有使用者流程數據前將 Goal Pyramid 降為次要內容；同時可讓 Radar 在自己的區塊內維持 TodayRemaining 主指標。若日常記帳頻率與快速記錄是核心成功指標，再以 A 進行獨立資訊架構測試。

## 後續 Phase 4 是否值得做

值得，但建議先做決策定義與小範圍驗證，不直接擴張 API。

1. 先確認 Dashboard 的主要工作流程（每日記帳 vs. 週期/人生規劃），再選 A 或 B；可用瀏覽器原型或可用性測試比較首要任務完成時間。
2. 定義風險狀態：例如 approaching-limit 的門檻、`DailySafeLimit <= 0`、`TodayRemaining == 0` 的語意，以及負值提示文案。不要在 UI 私自推定門檻。
3. 先用現有 DTO 改善呈現口徑：說明 TodaySpent 的 Living expense 範圍、TodayRemaining 的計算關係，以及未分類支出不會從預算扣除。
4. 若 Phase 4 要呈現週期剩餘預算、週期日期、Goal Purchase 或未分類交易如何影響可用額度，現有 DTO 無法完整回答；應另立 API/read-model 合約審查，而不是在前端從不足資料推算。

Phase 4 的價值在於讓「風險判斷與下一步行動」更可信；若沒有先確認門檻和預算口徑，單純新增紅黃綠狀態可能只增加視覺警報，反而降低信任。