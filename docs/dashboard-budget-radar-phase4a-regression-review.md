# Budget Radar Phase 4A Regression Review

## 已確認項目

### Dashboard 區塊層級

- 頁面順序仍為 Financial Overview → Goal Pyramid → Budget Radar → Account Overview → Quick Add。
- Phase 4A 只改 BudgetRadarPanel 的內容呈現；沒有改 DashboardPage 資料流或區塊順序，財務規劃優先的整體層級保持不變。
- Budget Radar 區塊內，TodayRemaining 是獨立主指標，四個次要資料仍完整呈現。這不會讓 Radar 超越 Financial Overview 或 Goal Pyramid 的頁面層級。

### BudgetRadarPanel

- TodayRemaining 使用現有簡單門檻：負值為超支，非負值為正常；負值保留原始負號和金額。
- 主指標旁顯示「正常狀態」或「超支狀態」。超支時另有「今日已超過安全額度」提醒。
- UncategorizedExpenseAmount 大於零時提示「本期未分類支出…尚未計入今日預算」，文案有指出薪資週期口徑，也沒有改變預算計算。
- 顏色使用既有 semantic success/warning/surface/text token。Light mode 顯示對應淺色 surface 與深色文字；dark mode 顯示深色 surface 與淺色文字。主指標狀態色亦隨 theme 變化。
- 四個次要指標仍保留：今日已支出、每日安全額度、剩餘天數、生活預算。沒有新增計算欄位。

### Browser Responsive Review

使用僅在瀏覽器 route 內的展示資料檢查，不呼叫或修改持久化資料。

- `1280px`：Dashboard client width 1265px（含垂直 scrollbar 的 viewport 差異），scroll width 同為 1265px；沒有水平溢出。Radar 次要資訊維持水平排列。
- `390px`：client width 與 scroll width 均為 375px，沒有水平溢出。主指標金額為 30px，次要金額為 16px，次要資料以兩欄呈現。
- Light/dark 下主指標 surface、文字與狀態色均解析至對應 semantic tokens；超支狀態及未分類提醒在 dark mode 仍可辨識。
- 在包含超支和未分類提醒的手機測試資料下，Radar 高約 581px。捲動至 Radar 區時其底部仍在 fixed navigation 上方；捲動至 Account Overview 時最後一列完整可見。頁尾 Quick Add 可繼續捲至導覽上方，末端保留約 65px 空間。

## 發現問題

1. **決策文件與實作的零值邊界需要明確確認。** Phase 4A 要求 `todayRemaining >= 0` 為正常，因此 `TodayRemaining == 0` 顯示「正常狀態」；Phase 3C decision freeze 曾建議零值視為額度耗盡/警示。Phase 4A 實作遵循當時明確的 `>= 0` 要求，但正式產品決策最好統一文字與規則，避免零額度仍被理解為可繼續支出。
2. **超支語意在無障礙樹中有重複訊息。** 主指標內的「超支狀態」和額外 `role="status"` 的「今日已超過安全額度」都會傳達相近狀態。視覺上有主狀態與說明之分，但螢幕閱讀器可能連續宣告兩次超支。建議後續確認是否保留一個 live-region 提示、另一個只作靜態標籤。
3. **雙提醒時手機 Radar 區塊較長。** 390px 且同時超支、未分類金額大於零時，高約 581px。內容仍可完整捲動、未遮擋帳戶列或 fixed navigation，屬於垂直密度觀察，未構成溢出或無法操作的 regression。
4. **Dashboard 顯示的每日值有明確計算口徑。** `TodaySpent` 是今日 Living expense；`UncategorizedExpenseAmount` 是本期未分類總額且不扣減 Living budget。Phase 4A 文案已提示未分類金額尚未納入今日預算，但 Dashboard 其他區塊仍需避免讓使用者把 Radar 誤讀為全交易或帳戶現金流。

## 建議後續

- 不需回退 Phase 4A hierarchy；現有主指標、次要資料和響應式排列正常。
- 先確認 `TodayRemaining == 0` 的產品語意，再決定是否調整狀態門檻與文案；不要自行更改目前凍結的 Phase 4A 規則。
- 後續可做一次螢幕閱讀器檢查，確認主狀態與超支 live-region 的宣告是否重複。
- 保持 uncategorized 的期間口徑說明。若將來要把未分類支出納入可用預算，需另行更新 BudgetSnapshot/API 計算政策，不應只在前端調整數字。

## 未修改範圍

本次只新增此 review 文件；React、CSS、API、backend 與 DTO 均未修改。