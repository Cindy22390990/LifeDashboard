LifeDashboard V3 — Dashboard Data Architecture Specification

Version: 1.0

Status: Architecture Specification (Ready for Frontend Implementation)

Scope: Frontend Data Architecture Only (React + TypeScript + Vite)

Exclusions: No Backend Code, No New Backend Entities, No Database Migrations, No Multi-Currency.

1. Architecture Goal

本規格書旨在為 LifeDashboard V3 的 Dashboard 建立穩定、可測試且與後端實作解耦的前端資料架構，核心目標包含：

建立單向且清晰的資料流 (Unidirectional Data Flow)：嚴格隔離 UI 元件與資料來源，確保 Widget 僅依賴專為畫面設計的 ViewModel。

無痛切換 Mock 與真實 API (Seamless Source Swapping)：透過介面反轉 (Dependency Inversion) 與資料轉換層 (Mapper)，讓目前使用 Mock Data 的 V3 Scaffold 在未來串接 .NET API 時，無須修改任何 UI 元件。

保護前端 Clean Architecture 邊界：避免將後端 Domain Model（如 Account, Transaction, Goal）直接暴露給 React 元件，防止後端 Schema 演進（例如未來導入 Goal Funding Read Model）引發前端連鎖破壞。

確保資料不可變性 (Read-Only Guarantee)：Dashboard 屬於唯讀視圖 (Read View)，前端資料層嚴禁在畫面渲染過程中直接修改或覆寫使用者建立的原始領域資料。

2. Current Constraint Analysis

在設計前端資料層之前，必須明確識別目前「UX 需求」與「現有後端能力」之間的不對稱性：

維度

V3 UX 需求

現有後端限制

架構應對策略

總資產與帳戶

Hero Stats 顯示總資產、Account Summary 顯示各帳戶餘額

僅有 Account (Balance)，尚無正式 V3 Summary API

前端定義 HeroStatsViewModel 與 AccountSummaryViewModel，現階段由 Mock Provider 提供。

月曆與每日明細

Monthly/Weekly Calendar 顯示每日收支摘要，點擊展開 Day Detail Panel

僅有 Transaction 列表概念，無專屬日曆聚合 API

定義獨立的 CalendarMonthViewModel，將交易紀錄投影為「依日期分組」的 UI 結構。

預算雷達

Budget Radar 顯示本期預算、已花費、剩餘可用、健康狀態

無 Backend Budget Entity，且禁止新增

建立 Frontend-only 的 BudgetRadarViewModel，將預算基數與狀態暫時隔離於 Mock Adapter 中。

目標進度

Goal Progress 顯示進度條、百分比、剩餘金額（無存款按鈕）

現有 Goal 有 TargetAmount / CurrentAmount，未來將轉為 Allocation Read Model

透過 GoalProgressViewModel 抽象化進度欄位，切斷 UI 與 Goal Entity 的直接耦合。

隱私與多語系

支援金額遮罩 (Privacy Mode) 與多語系格式化

後端僅回傳純數值與原始文字

ViewModel 必須保留原始數值 (Raw Numbers)；格式化與遮罩屬於 Presentation 層職責，不應寫入 ViewModel。

3. Dashboard ViewModel Design

Dashboard 的頂層資料契約為 DashboardViewModel，它是一個純粹的唯讀投影 (Read-Only Projection)，由六個子 ViewModel 組合而成。

3.1 DashboardViewModel (Root Container)

欄位名稱

結構類型

職責說明

referenceDate

String (ISO Date)

本次儀表板查詢的基準日期（用於決定「今日」與「本月」）。

heroStats

HeroStatsViewModel

首頁第一眼核心數據。

calendar

CalendarViewModel

日曆視圖與每日收支摘要。

budgetRadar

BudgetRadarViewModel

本月生活預算健康度。

goalProgress

GoalProgressViewModel[]

首頁展示的重點財務目標清單。

recentTransactions

RecentTransactionItemViewModel[]

最近 N 筆交易明細（預設上限 5 筆）。

accountSummary

AccountSummaryViewModel[]

活躍資產帳戶摘要清單。

3.2 HeroStatsViewModel

職責：回答「我現在有多少錢」、「今天花了多少」、「這個月還剩多少」。

欄位名稱

型別

資料來源分類

說明

totalAssets

Number

Existing Domain (Account.Balance 聚合)

所有活躍帳戶餘額總和。

spentToday

Number

Existing Domain (Transaction 篩選聚合)

基準日當天的支出總額。

remainingBudget

Number

UI Projection / Mocked (未來由 Budget API 提供)

本月剩餘可用生活預算。

monthlyIncome

Number

Existing Domain (Transaction 篩選聚合)

當月總收入（供輔助顯示或計算參考）。

monthlyExpense

Number

Existing Domain (Transaction 篩選聚合)

當月總支出。

3.3 CalendarViewModel & CalendarDayDetailViewModel

職責：驅動首頁日曆格線與點擊日期後展開的 Day Detail Panel（Desktop 側邊欄 / Mobile Bottom Sheet）。

欄位名稱

型別

資料來源分類

說明

year / month

Number

UI Projection

目前日曆顯示的年份與月份。

days

Map / List of CalendarDaySummary

UI Projection (源自 Transaction)

每日摘要：date, totalIncome, totalExpense, hasTransfer (布林值，用於顯示灰點標記)。

selectedDate

String (ISO Date)

UI State

目前選中的日期。

selectedDayDetail

CalendarDayDetailViewModel

UI Projection (源自 Transaction)

包含該日的 dailyIncome, dailyExpense, 與 transactions: RecentTransactionItemViewModel[]。

3.4 BudgetRadarViewModel

職責：視覺化呈現本月生活費的消耗速度與健康度。

欄位名稱

型別

資料來源分類

說明

periodLabel

String

UI Projection

預算週期描述（如 2026-11 或發薪週期區間）。

totalBudget

Number

Mocked (無後端 Entity)

本期總生活預算。

totalSpent

Number

Existing Domain (Transaction) / Mocked

本期已消耗的生活預算。

remainingLivingBudget

Number

UI Projection / Mocked

本期剩餘可用預算。

usagePercentage

Number (0–100+)

UI Projection

預算消耗百分比，僅供進度條渲染。

healthStatus

Enum (normal | warning | exceeded | unavailable)

UI Projection

決定雷達語意色（綠/黃/紅/灰）。

3.5 GoalProgressViewModel

職責：以唯讀方式呈現財務目標達成率，完全隔離未來的 Goal Funding 分配邏輯。

欄位名稱

型別

資料來源分類

說明

goalId

String

Existing Domain (Goal.Id)

唯一識別碼。

name

String

Existing Domain (Goal.Name)

目標名稱（使用者建立，UI 不可修改）。

targetAmount

Number

Existing Domain (Goal.TargetAmount)

目標總額。

fundedAmount

Number

UI Projection (目前映射自 Goal.CurrentAmount)

已累積金額（語意上預先對齊未來的 GoalFundingSummary）。

remainingAmount

Number

UI Projection

距離目標尚差金額 (max(0, target - funded))。

progressPercentage

Number (0–100)

UI Projection

達成百分比。

deadlineDate

String | Null

Existing Domain / Optional

目標期限（若有）。

3.6 RecentTransactionItemViewModel

職責：解決舊版交易列表資訊不足的問題，提供清晰的主副標題與分類層級。

欄位名稱

型別

資料來源分類

說明

id

String

Existing Domain (Transaction.Id)

交易識別碼。

title

String

UI Projection (優先取 Note 或分類主名稱)

交易主標題（例如「11月薪資收入」、「星巴克」）。

subtitle

String | Null

Existing Domain (Transaction.Note 拆解或備註)

次要備註資訊，無資料時為 null。

categoryKey

String

Existing Domain (Transaction.Category)

分類識別鍵（供前端對應 Icon 與多語系標籤）。

accountName

String

Existing Domain (Account.Name 關聯)

實際入帳或扣款的帳戶名稱。

transactionType

Enum (income | expense | transfer)

Existing Domain (Transaction.Type)

決定金額正負號與色彩。

amount

Number

Existing Domain (Transaction.Amount)

原始交易金額（正數，方向由 transactionType 決定）。

occurredAt

String (ISO DateTime)

Existing Domain (Transaction.Date)

交易發生時間。

3.7 AccountSummaryViewModel

職責：呈現各實體帳戶的水位總覽。

欄位名稱

型別

資料來源分類

說明

accountId

String

Existing Domain (Account.Id)

帳戶識別碼。

name

String

Existing Domain (Account.Name)

帳戶名稱（如「華南銀行」、「現金」）。

balance

Number

Existing Domain (Account.Balance)

當前真實餘額（絕不由前端加總交易重算）。

accountType

String

Existing Domain / Optional

帳戶類型（用於顯示對應 Icon）。

4. Component Data Boundary

為了防止資料獲取邏輯散落各處，前端必須嚴格執行 Container (Page) vs. Presentational (Feature Widget) 的資料邊界：

[ Data Source (Mock / Future API) ]
                │
                ▼
     [ Anti-Corruption Mapper ]  ──► 將外部結構轉換為 DashboardViewModel
                │
                ▼
[ Application Hook / Dashboard Store ] ──► 管理 Loading / Error / SelectedDate
                │
                ▼
       [ DashboardPage ] (Smart Container)
         │     │     │     │     │     │
         ▼     ▼     ▼     ▼     ▼     ▼
    (僅透過 Props 傳遞對應的子 ViewModel，嚴禁反向依賴)
   Hero  Calendar Budget  Goal  Recent  Account
   Stats  Widget  Radar  Widget  Txns   Summary


嚴格邊界規範

允許存取 Application Hook / Store 的層級：

DashboardPage：唯一負責觸發資料載入、處理全域 Loading/Error 狀態、並將子 ViewModel 分派給各個 Widget 的組裝層。

CalendarSection（或其容器）：可讀取/更新「當前選中日期 (selectedDate)」與「月曆展開/收合」這類純 UI 互動狀態。

絕對禁止直接存取 API 或資料 Store 的元件：

HeroStats

MonthlyCalendar / WeeklyCalendar (純日曆格線渲染)

DayDetailPanel / DayDetailBottomSheet (資料由父層傳入，僅透過 Callback 觸發「新增交易」視窗)

BudgetRadarWidget

GoalProgressWidget / GoalCard

RecentTransactionsWidget / TransactionItem

AccountSummaryWidget

純 UI 全域偏好狀態的例外規則：

如 PrivacyMode（金額隱藏狀態）或 i18n（語系設定），屬於純 Presentation 層的顯示控制。建議透過統一的 Display Wrapper Component（例如 <AmountDisplay value={...} />）讀取 privacyStore，避免讓每個業務 Widget 各自耦合全域 Store。

5. Mock Data Strategy

目前的 V3 Scaffold 依賴 Mock Data，為了確保未來替換為正式 API 時達成 Zero UI Refactoring（UI 零重構），必須採用 Port & Adapter（介面反轉） 模式。

5.1 檔案放置位置（建議 Clean Architecture 前端目錄）

src/
├── domain/
│   └── dashboard/
│       └── viewModels/          # 放置所有 Dashboard ViewModel 型別定義 (純介面)
├── application/
│   └── dashboard/
│       ├── ports/               # 放置 IDashboardDataSource 介面定義
│       └── useDashboardData.ts  # 協調資料取得、Loading/Error 狀態與快取
├── infrastructure/
│   └── dashboard/
│       ├── dtos/                # 放置預期與後端對齊的 Raw DTO 型別
│       ├── mappers/             # 放置 DashboardMapper (Raw DTO -> ViewModel)
│       ├── mock/                # 放置 MockDashboardDataSource 與靜態 Mock JSON/Fixtures
│       └── api/                 # (未來) 放置 ApiDashboardDataSource
└── presentation/
    └── dashboard/               # DashboardPage 與純 UI Feature Components


5.2 Interface 定義方式

在 application/dashboard/ports/ 定義 IDashboardDataSource 介面，包含如「取得指定基準日的儀表板總覽資料」與「取得指定月份的日曆交易資料」等方法簽章。

關鍵規則：介面回傳的必須是標準化的 DashboardViewModel（或是經由 Mapper 轉換後的內部模型），而非特定 API 的 HTTP Response 格式。

5.3 未來替換 API 的機制

目前階段：應用程式的依賴注入點（或 Service Factory）將 IDashboardDataSource 綁定至 MockDashboardDataSource。Mock 資料應模擬真實的 DTO 結構，並同樣經過 DashboardMapper 轉換，以提前驗證 Mapper 的正確性。

串接 API 階段：新增 ApiDashboardDataSource 實作相同的 IDashboardDataSource 介面，呼叫後端 Endpoint 後交給同一個（或更新後的）DashboardMapper 輸出 DashboardViewModel。

切換成本：僅需在設定檔或 Factory 中切換實作類別，所有 React UI 元件完全不需要修改任何一行 Props。

6. Budget Radar Design (Frontend-Only Model)

由於目前後端沒有 Budget Entity，且本階段禁止新增後端模型，Budget Radar 必須被設計為一個邊界明確的前端隔離模型。

6.1 它需要哪些資料？

週期識別：目前計算的是哪一個月或發薪週期 (periodLabel)。

預算基數：本期預期花費上限 (totalBudget)。

實際支出：本期已累積的生活支出 (totalSpent)。

剩餘額度：本期還可花費的金額 (remainingLivingBudget)。

視覺指標：消耗比例 (usagePercentage) 與健康狀態 (healthStatus)。

6.2 哪些資料目前可以 Mock？

在無後端 Budget API 的現階段，整個 BudgetRadarViewModel 的數值皆由 MockDashboardDataSource 提供固定且具代表性的測試情境（建議內建 Normal / Warning / Exceeded 三種 Mock Fixture 方便切換驗證 UI）。

嚴格禁止：不要在 React Component 內寫死預算上限常數（如 const BUDGET = 30000），也不要在元件內自行拿 Transaction 陣列去推算複雜的固定責任扣除公式。所有暫時性的估算或假資料，一律封裝在 infrastructure/dashboard/mock/ 中。

6.3 未來需要哪些 API Contract？

當後端準備好支援 Budget Radar（例如 GET /api/budget/radar）時，前端期望的最小唯讀契約為：

後端直接提供計算完成後的 totalBudget、totalSpent 與 remainingLivingBudget（已排除 Soft Delete 交易與固定責任預留款）。

若後端第一階段尚未提供 usagePercentage 或 healthStatus，前端 DashboardMapper 可在過渡期於 Mapper 層進行純視覺投影，待後端正式提供狀態後再直接透傳，且同樣不影響 UI 元件。

7. Goal Progress Design

為了貫徹既有架構決策（Goal.currentAmount 不應作為領域唯一真實來源，未來將由 GoalFundingAllocation 產出 GoalFundingSummary Read Model），前端必須徹底切斷 UI 與 Goal Entity 的直接綁定。

7.1 解耦策略

語意隔離 (Semantic Decoupling)：

前端 ViewModel 不使用 currentAmount 這個容易造成「帳戶餘額」或「手動記帳數字」混淆的命名，統一使用 fundedAmount（已分配資金）。

轉換層隔離 (Adapter Isolation)：

現階段：DashboardMapper 將現有 Goal 資料（或 Mock）中的 currentAmount 映射至 GoalProgressViewModel.fundedAmount，並於 Mapper 計算 remainingAmount 與 progressPercentage。

未來階段：當後端改由 GoalFundingSummary Read Model 回傳 totalFundedAmount 時，前端只需修改 DashboardMapper 的一行欄位對應，GoalProgressWidget 與 GoalCard 完全無感。

互動邊界限制：

GoalProgressViewModel 不包含任何 linkedAccountId、transferAction 或 depositUrl 欄位，從資料結構層面根絕「在 Goal 卡片上做轉帳/存款」的錯誤設計。

8. Future API Boundary

本節定義未來後端實作正式 Dashboard API 時的邊界建議（僅定義概念與對應關係，不進行後端實作）。

8.1 建議 Endpoint 概念

主要總覽端點：GET /api/dashboard/summary?date={YYYY-MM-DD}

職責：一次性回傳首頁所需的唯讀聚合資料，避免前端發出 N+1 次請求（例如分別打 Accounts、Transactions、Goals API 再由前端自己加總）。

日曆明細端點（可選拆分）：GET /api/dashboard/calendar?year={YYYY}&month={MM}

職責：當使用者在月曆切換「上個月 / 下個月」時獨立載入，避免切換月份時重複刷新整個 Hero Stats 與 Account Summary。

8.2 預期 Response Shape (概念)

DashboardSummaryApiResponse
├── referenceDate (string)
├── assetsSummary
│   ├── totalAssets (number)
│   └── accounts[] (id, name, balance, type)
├── monthlyCashFlow
│   ├── monthlyIncome (number)
│   ├── monthlyExpense (number)
│   └── spentToday (number)
├── budgetRadar (nullable; 視後端階段逐步補齊)
│   ├── totalBudget (number)
│   ├── totalSpent (number)
│   └── remainingLivingBudget (number)
├── goalsProgress[] (goalId, name, targetAmount, fundedAmount, deadline)
└── recentTransactions[] (id, title, note, category, accountName, type, amount, date)


8.3 API Response 與 ViewModel 的關係

禁止直接將 API Response 傳給 React Component。

兩者之間必須存在 DashboardMapper (Anti-Corruption Layer)：

處理欄位名稱差異與空值防禦（例如 budgetRadar 若為 null，Mapper 負責轉為 healthStatus: 'unavailable' 的安全預設值）。

計算純視覺衍生欄位（如 GoalProgressViewModel.progressPercentage、將 recentTransactions 整理出清楚的 title 與 subtitle）。

確保後端 API 的微調被吸收在 infrastructure/ 層，不污染 presentation/ 層。

9. Implementation Recommendation

維持原始數值，延後格式化：
ViewModel 內的所有金額一律維持 number 型別，日期維持標準 ISO String。千分位符號、貨幣符號（目前單一基礎幣別）以及 Privacy Mode 的 •••• 遮罩，統一交由 Presentation 層的格式化元件處理。

狀態管理職責分離：

Server / Async Data（DashboardViewModel、Loading、Error）：使用專用 Hook（如 useDashboardData，未來可無縫接入 React Query / TanStack Query）。

Client UI State（Drawer 開關、Privacy Mode、Calendar 選中日期）：交由輕量級 Client Store（如 Zustand）或局部 Component State 管理。

保護既有測試與資料完整性：
所有對使用者資料（如交易備註、目標名稱）的讀取均為唯讀；ViewModel 轉換過程必須為純函式 (Pure Function)，方便未來針對 Mapper 撰寫單元測試。

Copilot Implementation Checklist

請依照以下清單分步驟實作前端資料架構重構，嚴格遵守邊界限制。

1. 應該新增或修改的 Frontend 檔案

[ ] 建立 ViewModel 型別定義：於 src/domain/dashboard/viewModels/ 建立 DashboardViewModel 及其六個子 ViewModel 的 TypeScript Interfaces。

[ ] 建立 Data Source Port：於 src/application/dashboard/ports/ 建立 IDashboardDataSource 介面。

[ ] 建立 Mapper 層：於 src/infrastructure/dashboard/mappers/ 建立純函式 DashboardMapper，負責將 Raw Data/DTO 轉換為標準 DashboardViewModel（包含日曆每日分組、目標進度百分比投影、交易標題與副標題整理）。

[ ] 抽離並集中 Mock Data：將目前散落在 V3 Scaffold 元件內的寫死假資料，統一移至 src/infrastructure/dashboard/mock/MockDashboardDataSource，並實作 IDashboardDataSource。

[ ] 建立 Application Hook：於 src/application/dashboard/ 建立 useDashboardData Hook，負責呼叫 IDashboardDataSource 並回傳 { data: DashboardViewModel | null, isLoading, error, refetch }。

[ ] 重構 DashboardPage 與子元件：

DashboardPage 改為透過 useDashboardData 取得資料，並處理 Skeleton Loading 與 Error State。

將 HeroStats, CalendarSection, BudgetRadarWidget, GoalProgressWidget, RecentTransactionsWidget, AccountSummaryWidget 全部改為純 Props 驅動，接收各自對應的子 ViewModel。

2. 絕對不應修改的地方 (Do Not Touch)

[ ] 禁止修改任何 Backend 檔案（包含 .NET Controllers, Services, Entities, DTOs）。

[ ] 禁止建立或修改 Database Migration。

[ ] 禁止新增 Multi-Currency / Exchange Rate 相關欄位或換算邏輯。

[ ] 禁止在 UI 元件內加入 Account.Balance 重算邏輯（不可用 Transactions 加總覆蓋帳戶餘額）。

[ ] 禁止在 GoalCard 加入 Deposit / Transfer 互動按鈕。

[ ] 禁止在 UI 元件內直接 import Mock JSON 檔案或直接呼叫 fetch / axios。

3. 驗收條件 (Acceptance Criteria)

[ ] 零硬編碼假資料：所有 presentation/dashboard/ 底下的 React 元件內，不再存在任何寫死的 Mock 陣列或測試數字。

[ ] 嚴格型別隔離：所有 Dashboard Feature Widget 的 Props 型別僅引用 domain/dashboard/viewModels/，不直接引用 API DTO 或後端 Entity 命名（如 currentAmount）。

[ ] 單點切換驗證：只需替換 IDashboardDataSource 的實作（例如切換不同的 Mock 情境：正常狀態 / 預算超支 / 空狀態），整個 Dashboard 畫面即可正確響應，無須改動任何 UI 元件程式碼。

[ ] 狀態完整性：Dashboard 能正確呈現 Loading（Skeleton）、Empty（首次使用引導）與 Normal 三種狀態，且點擊日曆日期能正確在 Day Detail Panel 顯示對應的 selectedDayDetail 資料。

[ ] 隱私模式相容：ViewModel 傳遞的皆為原始數值，切換 Privacy Display Mode 時，目標區塊（Hero Stats、Account Summary、Goal Progress）能正確遮罩而不破壞版面。