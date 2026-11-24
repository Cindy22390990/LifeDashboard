# Architecture Review

LifeDashboard V3 前端目前已完成高度解耦的架構：`MockDashboardDataSource → Mapper → IDashboardDataSource → useDashboardData() → ViewModel → Widgets`。這層邊界設計非常乾淨，成功將 UI 與資料來源隔離，使後續替換為真實後端 API 時，Presentation 層幾乎不需要更動。

然而，後端目前仍停留在以 `Account`、`Transaction`、`Goal` 為主的 CRUD 基礎上。若直接將現有 Entity 暴露給 Dashboard V3，將會導致嚴重的效能瓶頸、前端業務邏輯外洩以及領域邊界汙染。本報告將針對現況缺口、API 粒度、BFF 定位、多平台擴展性與實作路線進行全面評估。

---

# Current Assessment

## 1. 現有 Backend 對 Dashboard V3 的支援度分析

目前 Backend **不足以直接支援** Dashboard V3，必須透過 Application 層的 Query 與 Projection 補齊聚合邏輯。

| Widget | 所需資料 | 現有 Entity 支援度 | 缺口與處理方式 |
| --- | --- | --- | --- |
| **Hero Stats** | 總資產、今日花費、本月剩餘可用 | 部分支援 | 需跨 `Account` 加總餘額、篩選今日 `Transaction` 支出；「本月剩餘」缺乏預算基準，需依賴設定值或暫時以「本月收入 - 本月支出」推算。 |
| **Budget Radar** | 本月已花費、預算上限、健康度狀態 (Safe/Warning/Exceeded) | **不支援** | 目前無 Budget Entity。已花費可由 `Transaction` 聚合，但上限與狀態門檻需由設定檔或暫存 Read Model 提供。 |
| **Goal Progress** | 目標名稱、目標金額、已存金額、百分比、剩餘金額 | 部分支援 | 依既有 ADR，`Goal.currentAmount` 非唯一真實來源。需透過 `GoalFundingSummary` Read Model 提供進度，不可由 Entity 即時掃描交易。 |
| **Account Summary** | 活躍帳戶名稱、餘額、類型 | **高度支援** | 可直接從 `Account` 投影（Projection）出輕量摘要，過濾掉已封存帳戶。 |
| **Recent Transactions** | 最新 5 筆交易（標題、副標題、分類、帳戶名、日期、金額） | **高度支援** | 需 Join `Transaction`、`Category`、`Account`，以 Projection 輸出扁平化列表。 |
| **Calendar** | 指定月份每日的收入總額、支出總額、是否有轉帳；點擊日期後的當日明細 | 部分支援 | 需新增按「年月」群組聚合（Group By Date）的查詢，避免將整月原始交易全部丟給前端計算。 |

## 2. Entity vs. Query / Projection / Read Model 分層判定

Clean Architecture 嚴禁將 Domain Entity 直接回傳給 API，Dashboard 屬於高頻讀取、跨聚合（Cross-Aggregate）場景，應全面採用 CQRS 的 Query 側思維：

* **直接投影 (Application Query + Projection → DTO)**：適用於 `Account Summary`、`Recent Transactions`、`Calendar Daily Summary`。直接透過 EF Core `.Select()` 在資料庫層完成 Join 與 Group By，直接映射到 Read DTO，不載入完整 Entity 進記憶體。
* **獨立 Read Model**：適用於 `Goal Progress`（對應既有 ADR 的 `GoalFundingSummary`）。避免每次開啟首頁都對歷史 `GoalFundingAllocation` 進行全表加總。
* **Application 聚合計算 (Query Service)**：適用於 `Hero Stats` 與 `Budget Radar`。由專門的 Query Handler 協調多個 Read Repository 組合最終 DTO。

## 3. Budget Radar 是否應新增 Domain？

**結論：目前不需要新增完整的 Budget Domain（`Budget`、`BudgetAllocation`、`BudgetSummary`）。**

* **原因 1（避免過早複雜化）**：目前的優先級是打通 Dashboard V3 串接與 Goal Funding 模型。若現在引入完整的 Budget Domain，會立刻面臨「分類預算 vs. 總預算」、「週期重置」、「固定責任（Fixed Commitments）防重複計算」等複雜業務規則，拖慢整體進度。
* **原因 2（MVP 替代方案）**：Dashboard V3 的 Budget Radar 只需要「本月總預算上限」與「本月累計支出」。現階段只需在 `UserSettings` 或輕量級設定中提供一個 `MonthlyBudgetLimit` 數值，由 Dashboard Query 層動態計算使用率與狀態即可。待未來進入「預算管理模組」開發時，再獨立建立 Budget Domain。

---

# Risks

| 風險項目 | 風險原因 | 改善方式 |
| --- | --- | --- |
| **1. API 過胖 (Fat API) 與效能拖累** | 若把 Hero、Budget、Goals、Accounts、Recent 與整個月的 Calendar 全部塞進單一 `GET /dashboard`，其中任何一個慢查詢（如月曆聚合）都會拖慢整個首頁渲染。 | 將「首頁核心摘要」與「月曆 / 單日明細」拆分為獨立 API，允許前端分層載入與獨立 Skeleton。 |
| **2. Mapper 放錯層導致架構汙染** | 若將 `API DTO → ViewModel` 的轉換邏輯寫在 React Component 或 Hook（Presentation 層），一旦後端欄位更名，UI 元件將被迫大量修改。 | 嚴格將 Mapper 留在 **Infrastructure / Data 層**（即 `ApiDashboardDataSource` 內部），確保 `IDashboardDataSource` 對外永遠只吐出乾淨的 `ViewModel`。 |
| **3. ViewModel 與 Backend DTO 混用** | 為了省事直接讓前端元件吃後端回傳的 `DashboardResponseDto`，會導致後端命名風格（如 PascalCase 或扁平結構）與 UI 狀態（如格式化文字、狀態色票）耦合。 | 維持 `Backend DTO`（傳輸合約）與 `Frontend ViewModel`（UI 視圖模型）的嚴格分離，透過 Mapper 作為防腐層（Anti-Corruption Layer）。 |
| **4. Calendar 查詢引發 N+1 或全表掃描** | 若前端切換月份時，後端撈出該月所有 Transaction Entity 再於記憶體中分組，隨資料量成長會造成嚴重延遲。 | 在資料庫層使用索引（`UserId` + `TransactionDate`）並直接執行 `GROUP BY Date` 投影出每日收支摘要；單日明細則於點擊日期時才按需查詢（Lazy Load）。 |
| **5. Dashboard 聚合責任過重 (God Service)** | 若把所有計算邏輯塞進單一 `DashboardService`，該類別將依賴所有 Repository，迅速膨脹為難以測試與維護的 God Class。 | 採用細粒度的 Query Handler（如 `GetHeroStatsQuery`、`GetCalendarSummaryQuery`），或將聚合邏輯拆分為獨立的 Read Provider，由薄層的 Facade 組合。 |
| **6. 跨端邏輯不一致（多平台隱憂）** | 若「預算健康度狀態（Safe/Warning/Exceeded）」或「今日花費加總」交由前端計算，未來開發 iOS / Android 時必須重寫一次商業規則，極易產生數據不一致。 | **數值計算與業務狀態判定（如 Status Enum）一律由後端計算**；前端僅負責語系翻譯（i18n）、幣別符號格式化與隱私遮罩。 |

---

# Recommended Design

## 1. Single Aggregate API vs. Widget-based API

從 Clean Architecture 與實際效能權衡，**不建議走極端**，應採用 **「核心聚合 + 互動拆分 (Pragmatic Hybrid)」** 策略：

| 維度 | Single Aggregate API (`GET /dashboard`) | Widget-based API (每個卡片一支) | **推薦：Hybrid 策略** |
| --- | --- | --- | --- |
| **網路開銷** | 最佳（1 次請求） | 較差（6+ 次請求，行動網路易卡頓） | 佳（首屏 2 次請求，按需載入） |
| **快取與刷新** | 差（新增一筆交易需重撈全部） | 佳（可局部刷新） | 佳（月曆與明細可獨立切換與刷新） |
| **Clean Architecture** | 易產生過胖的 Query Service | 邊界最清晰，但前端組合複雜度高 | **平衡點最佳**：核心首屏聚合、互動區塊獨立 |

## 2. 是否需要 BFF (Backend for Frontend) 或 Dashboard Query Service？

**結論：不需要獨立的 BFF 服務，應在現有 .NET Backend 的 Application 層建立 `Dashboard Query Service`（或 CQRS Query Handlers）。**

* **為何不需要 BFF**：目前系統為單一 .NET Modular Monolith，沒有微服務架構。引入獨立的 Node.js/.NET BFF 會增加部署、驗證轉發、型別同步與維運成本，屬於典型的過度設計（Over-engineering）。
* **為何需要 Dashboard Query Service**：在 Application 層建立專屬的 `Queries/Dashboard` 模組，直接對接唯讀的 `IApplicationDbContext`（或 Read Repository），能以最低成本實現 CQRS 的讀寫分離，且不汙染 Domain Entity。

## 3. 未來支援 Web、PWA、Android、iOS 的多平台適配性

若要讓目前的 API 設計未來無縫支援 Web、PWA 與原生 App（Android / iOS），必須遵守以下三項設計鐵律：

1. **API 回傳「純數據與語意狀態」，不回傳「UI 呈現」**：
* 回傳 `amount: 125000`（而非 `"NT$125,000"`）。
* 回傳 `budgetStatus: "Warning"`（而非 `"#F59E0B"` 或 `"接近上限"`）。
* 各平台（React、Swift、Kotlin）各自負責本地化翻譯與視覺樣式映射。


2. **時間格式統一使用 ISO 8601 與明確時區處理**：
* 「今日花費」與「月曆日期」高度依賴使用者當地時區。API 應接受前端傳入的 `date` / `year` / `month` 或 `timezoneOffset`，避免 Web 與 App 因伺服器 UTC 時間差異導致「今天」的定義不同。


3. **分頁與按需載入（Lazy Loading）**：
* 行動端對 Payload 大小敏感。點擊月曆日期才呼叫單日明細 API，能大幅降低行動端首屏流量消耗。



## 4. ViewModel 與 DTO 設計定位

* **前端保留現有細粒度 ViewModel**：`HeroStatsViewModel`、`BudgetRadarViewModel`、`GoalProgressViewModel`、`RecentTransactionViewModel`、`AccountSummaryViewModel` 設計非常合理，讓每個 Widget 只依賴自己需要的型別。
* **需要頂層 `DashboardViewModel**`：作為 `useDashboardData()` 的統一回傳型別，封裝上述子 ViewModel。
* **需要 `DashboardSummaryResponseDto`（後端與前端 Infrastructure 層）**：作為 API 傳輸合約。注意：**DTO 只能存在於 `Infrastructure` 層，絕不可傳遞至 `Presentation` 層。**

## 5. Mapper 應該放在哪一層？

**結論：放在 Frontend 的 `Infrastructure` 層（資料存取層）。**

```text
[Backend API]
      ↓ (JSON / DashboardResponseDto)
[Infrastructure Layer]
  ApiDashboardDataSource  ──▶  DashboardMapper (DTO → ViewModel)
      ↓ (implements IDashboardDataSource)
[Application / Hook Layer]
  useDashboardData()
      ↓ (DashboardViewModel)
[Presentation Layer]
  DashboardV3Page ──▶ Widgets

```

* **原因**：在 Clean Architecture 中，依賴方向必須向內。`DTO` 是外部系統（Backend API）的細節，屬於 Infrastructure 層。將 Mapper 放在 Infrastructure 層，能確保當後端 API 欄位異動時，修改範圍被嚴格鎖死在 `ApiDashboardDataSource` 與 `DashboardMapper` 內，Application 與 Presentation 層完全免疫。

---

# Backend Contract

基於 Hybrid 策略與多平台適配原則，建議後端提供以下 **3 支唯讀 API**（不含寫入操作，寫入沿用既有 Transaction API）：

### 1. `GET /api/v1/dashboard/summary`

* **用途**：一次取得首屏核心靜態/高頻摘要資料。
* **Query Params**：`targetDate`（ISO Date，由前端傳入當地今日日期，用於精準計算「今日花費」與「當月」範圍）。
* **回傳內容概念**：
* `heroStats`: 總資產、今日支出、本月剩餘可用金額。
* `budgetRadar`: 本月預算上限、已使用金額、使用百分比、狀態列舉 (`Safe` | `Warning` | `Exceeded`)。
* `goalProgress`: 活躍目標摘要清單（來源為 `GoalFundingSummary` Read Model）。
* `accountSummary`: 活躍帳戶名稱與餘額清單。
* `recentTransactions`: 最新 5 筆交易摘要（含分類識別與帳戶名稱）。



### 2. `GET /api/v1/dashboard/calendar`

* **用途**：取得指定月份的每日金流摘要，供月曆/週曆渲染。
* **Query Params**：`year`, `month`。
* **回傳內容概念**：
* `days[]`: 僅回傳「有交易發生」的日期陣列，包含 `date`、`totalIncome`、`totalExpense`、`hasTransfer` (boolean)。



### 3. `GET /api/v1/dashboard/calendar/day`

* **用途**：使用者點擊特定日期時，按需載入 Day Detail Panel / Bottom Sheet 內容。
* **Query Params**：`date` (YYYY-MM-DD)。
* **回傳內容概念**：
* `date`、`dailyIncome`、`dailyExpense`、`transactions[]`（當日完整收支與轉帳清單）。



---

# Roadmap

以下為不破壞現有 Domain 與前端 UI 的漸進式整合路線：

* **Sprint 1：Backend Contract & Read Model 凍結**
* 確立上述 3 支 API 的 OpenAPI / Markdown 規格合約。
* 定義 `MonthlyBudgetLimit` 的暫存來源（如 UserSettings）與 `GoalFundingSummary` 的查詢介面。


* **Sprint 2：Dashboard Query Layer (Application 層)**
* 在 .NET Application 層建立專屬的 Dashboard Query Handlers 與 EF Core Projection。
* 撰寫針對跨聚合計算（總資產、今日支出、月曆 Group By）的單元測試。


* **Sprint 3：Dashboard API Endpoints (WebApi 層)**
* 建立唯讀的 `DashboardController`，串接 Query Handlers。
* 驗證時區參數處理、空資料（Empty State）回傳格式與 API 回應效能。


* **Sprint 4：Frontend Data Source 替換**
* 在前端 Infrastructure 層實作 `ApiDashboardDataSource` 與 `DashboardMapper`。
* 透過環境變數或 DI 切換，將 `MockDashboardDataSource` 替換為 `ApiDashboardDataSource`。


* **Sprint 5：Integration Test & 效能調校**
* 執行前後端端對端整合測試（含 Loading Skeleton、Error State、Privacy Mode 與多語系驗證）。
* 檢查資料庫查詢執行計畫（Query Plan），針對 `TransactionDate` 補齊必要索引。



---

# ADR Suggestions

在進入 Sprint 1 實作之前，建議補齊以下 3 份輕量級 ADR 以鎖定架構決策：

1. **`ADR-Dashboard-API-Granularity.md`**：正式記錄為何採用「Summary + Calendar + Day Detail」三支 API 的 Hybrid 策略，以及為何不採用獨立 BFF。
2. **`ADR-Budget-Radar-MVP-Strategy.md`**：明確記錄 Phase 1 不建立 Budget Domain，暫以單一設定值計算 Budget Radar 的邊界，防止開發期間發生 Scope Creep。
3. **`ADR-Client-Timezone-Handling.md`**：定義 Web、PWA 與未來 Mobile App 在查詢「今日花費」與「月曆聚合」時，前端傳遞本地日期與後端 UTC 轉換的統一標準。