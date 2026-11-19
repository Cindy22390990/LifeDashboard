Dashboard V2 UX Blueprint
1. Product UX Philosophy
LifeDashboard 的首頁必須在使用者打開 App 的第一秒鐘，無需任何點擊或滑動，直覺地回答以下四個核心財務問題：

我現在有多少錢？ (Total Assets - 給予安全感)

這個月還能花多少？ (Remaining Living Budget - 提供消費決策依據)

我的目標進度如何？ (Goal Progress - 建立成就感與動機)

最近錢都去了哪裡？ (Recent Transactions - 確保帳務清晰度)

所有的視覺層級與排版順序，皆須為這四個問題服務。

2. Information Architecture
為了符合真實使用者的財務管理心智模型，重新設計 Navigation（導覽列），捨棄過度系統化或冷硬的詞彙（如 Responsibilities, Stats）。

Dashboard (總覽)

使用情境：每日開啟 App 時的第一站，快速掌握健康度。

命名原因：業界標準詞彙，直覺且明確。

Ledger 或 Transactions (帳本/明細)

使用情境：記帳、查帳、修改消費紀錄。

命名原因：取代原先分散的功能，統一為「所有金流事實的歷史清單」。

Goals (目標分配)

使用情境：檢視存錢目標，未來將進行資金分配 (Explicit Allocation)。

命名原因：專注於「財務目標管理」，將其與普通的「帳戶轉帳」徹底切割。

Accounts (資產帳戶)

使用情境：對帳、檢視各銀行/現金的實際物理水位。

命名原因：代表真實世界的物理資產來源。

3. Dashboard Layout
Desktop Layout (網格排版)
在桌面端善用螢幕寬度，減少使用者的垂直滾動。

Top Row (Hero Section): Financial Overview 總覽卡片 (Total Assets, Monthly Income, Monthly Expense, Available Money)。橫向並排，字體最大。

Middle Row (2 Columns):

左側 (1/3 寬度): Budget Radar (視覺化圖表，強調剩餘預算與健康度 Status)。

右側 (2/3 寬度): Goal Progress (橫向進度條列表，展示 2-3 個主要目標)。

Bottom Row (2 Columns):

左側 (1/2 寬度): Recent Transactions (簡化的近期交易列表)。

右側 (1/2 寬度): Account Summary (各真實帳戶餘額列表)。

Mobile Layout (垂直滾動)
行動端注重單手操作與資訊吸收的優先順序。

Financial Overview: 置頂顯示，聚焦於 Total Assets 與 Available Money。

Budget Radar: 緊接在總覽下方，用顏色 (綠/黃/紅) 快速提示本月消費速度是否超標。

Recent Transactions: 列出最近 3-5 筆，方便使用者確認剛剛記的帳是否有進去。

Goal Progress: 放在中下段，作為查看完日常開銷後的正向回饋。

Account Summary: 放最底部，因為對帳頻率較低（通常是一週或月底才看）。

4. Component Design
Financial Overview Card

顯示：大字體的數字、貨幣符號。

不顯示：複雜的圖表或歷史趨勢線（避免干擾當下數字）。

Budget Radar Card

顯示：一個環狀進度條或油表圖、Remaining 數字、Status 文字（Normal/Warning/Exceeded）。

不顯示：各個小分類的細節預算（分類細節移至獨立的 Budget 頁面）。

Goal Progress Card

顯示：目標名稱、目標總額、目前進度 (基於 GoalFundingSummary Read Model)、百分比進度條。

不顯示：「從 XX 帳戶轉帳過來」的按鈕或提示。

Recent Transactions List

顯示：最新的 5 筆紀錄。

使用者操作：點擊列表底部的 "View All" 導航至 Ledger 頁面；點擊單筆交易可查看詳細資訊（但不在 Dashboard 直接編輯）。

Account Summary List

顯示：活躍狀態的實體帳戶名稱與餘額。

5. Transaction UX
為了解決過去「只看到一筆錢和銀行，卻不知道是什麼」的痛點，重新定義單筆交易列表元件 (Transaction Item) 的資訊解構：

Title (主標題)：具體的消費項目或來源（例如：「薪資收入」、「全聯福利中心」、「午餐」）。字體最粗。

Subtitle (副標題)：備註或次要資訊（如果沒有備註則隱藏，不留白）。

Category (類別)：獨立的 Icon 或 Tag（例如：🍔 餐飲、💰 薪水），放在左側或主標題旁。

Account (帳戶)：實際扣款或入帳的帳戶名稱（例如：「華南銀行」、「現金」），使用較小、灰色的字體，放在副標題下方。

Date (日期)：交易時間（例如：「今天 12:30」、「Oct 12」）。

Amount (金額)：靠右對齊，收入使用綠色 +$，支出使用黑色或紅色 -$。

6. Goal UX
配合未來的 Goal Funding 架構，Goal 頁面必須從「帳戶間的轉帳」轉型為「財務目標管理」。

現狀顯示：目前僅基於 API 傳回的 Read Model (TargetAmount, CurrentAmount) 呈現進度與剩餘所需金額。

去除轉帳操作：UI 上不再設計「存款 (Deposit)」並在背後發起 Transfer Transaction 的操作。

未來預留空間：在視覺上保留空間給未來的 Allocate Funds (分配資金) 行為。未來的 UX 會是「選擇一筆已發生的 Income Transaction，將其金額分配 (Allocate) 給特定 Goal」，而不是無中生有生出一筆轉帳。

7. Visual Design Direction
Color Palette (色彩方向)：

Semantic Colors (語意色)：收入/安全/正常 (Emerald Green)、警告 (Amber/Orange)、超支 (Rose Red)。

Background：使用非常淺的灰底 (Slate-50) 搭配純白 (White) 的卡片，創造乾淨的層次。

Card Style (卡片風格)：

無邊框，採用極淡的陰影 (Drop-shadow-sm) 與圓角 (Rounded-2xl)，打造現代化、無壓力的介面。

Spacing (間距)：

卡片與卡片之間保持充裕的留白 (Gap-4 到 Gap-6)，避免數字密密麻麻擠在一起造成的財務焦慮感。

Typography (字體層級)：

重點數字 (如 Total Assets) 必須具備絕對的視覺統治力。

Metadata (如帳戶名稱、日期) 使用 Muted text (Gray-500) 弱化。

Icons (圖示)：

全站統一使用 Outline 風格的圖示，保持簡潔，減少實心色塊帶來的視覺疲勞。

8. Implementation Guidance (For Copilot)
請依照以下階段逐步實作，確保符合 Clean Architecture 與現有 API 合約：

Phase 1: UI Scaffold & Mocking (純前端)

建立新的 Navigation 結構。

依據 Blueprint 切出 Desktop 與 Mobile 的 Layout 格線。

建立 DashboardSummary, BudgetRadar, GoalProgress, TransactionItem 基礎 UI Components。

使用假資料 (Mock Data，需對齊 Phase 5 DTO Contract) 確認渲染無誤。

Phase 2: API Integration (連接後端)

串接 GET /api/dashboard/summary 與 GET /api/budget/radar。

在 Zustand 中建立 useDashboardStore，負責 Fetching、Loading 與 Error 狀態管理（嚴禁在此撰寫業務計算邏輯）。

替換掉 Phase 1 的 Mock Data。

Phase 3: Goal Funding UI Preparation (準備下階段)

目前 Goal 卡片維持 Read-only 顯示。

等待後端實作 GoalFundingSummary 與 GoalFundingAllocation 相關 Command API 後，再新增 Allocation 互動介面。本階段不動。

9. Domain Constraint
# Domain Constraints

Frontend MUST NOT:

- calculate Goal funded amount from transactions
- infer user intention
- convert transfers into funding
- generate categories automatically

Frontend only displays:

- Backend provided facts
- Read Models
- Explicit user actions