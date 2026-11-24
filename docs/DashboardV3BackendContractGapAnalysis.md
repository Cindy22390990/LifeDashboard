# Dashboard V3 Mock Data → Backend API Contract Gap Analysis

> **Role**: Backend Integration Architect
> **Scope**: 盤點 Dashboard V3 六大核心區塊從 Mock Data 遷移至 API-only 時的後端契約缺口、DTO 審查、計算責任邊界與後續 Sprint 執行計畫（僅分析，不修改或產生任何程式碼）。

---

## 1. Existing API Coverage

| Dashboard Component | Required Data | Existing API | Status |
| --- | --- | --- | --- |
| **Hero** | Total Assets、Today's Expense、Budget Status、Allocation Summary | `GET /api/dashboard/v3/summary`（已提供 `totalAssets`、`todayExpense`、`businessDate`、`accounts[]`） | ⚠️ Partial |
| **Budget Radar** | Budget Limit、Spent Amount、Remaining Budget、Daily Available Budget | 無（尚未建立對應 Read Model / Endpoint） | ❌ Missing |
| **Calendar** | Daily Spending、Income Events、Day Detail、Fixed Commitments、Important Dates | `GET /api/dashboard/v3/calendar`、`GET /api/dashboard/v3/calendar/day`（已涵蓋每日收支與交易明細，尚缺固定承諾與重要日期） | ⚠️ Partial |
| **Fixed Commitment** | Fixed Commitment 清單、本月應付總額、到期日、繳費狀態 | 無（尚未建立對應 Read Model / Endpoint） | ❌ Missing |
| **Goal Pyramid** | Goal 清單、目標金額、已配置金額（Funding）、截止日、層級優先度 | 無（尚缺 `GoalFundingSummary` Read Model） | ❌ Missing |
| **Transaction Summary** | Recent Transactions（金額、類型、分類、帳戶、備註、交易日期） | `GET /api/dashboard/v3/summary`（已提供 `recentTransactions[]`） | ✅ Ready |

> **備註**：`Transaction Summary` 標示為 ✅ Ready 係以 `RecentTransactionDto` 已包含 `transactionType` 為前提；若實際 Swagger Schema 尚未暴露 `transactionType`，則此項目降級為 ⚠️ Partial（P0 阻塞項）。

---

## 2. DTO Contract Review

| DTO | Problem | Recommendation |
| --- | --- | --- |
| **`DashboardSummaryDto`** | UI Hero 區塊預期包含 `Budget Status` 與 `Allocation Summary`，但目前 DTO 僅涵蓋 `Account` 與 `Transaction` 聚合資料。 | **維持現狀，不硬塞空殼欄位**。在 Budget 與 Goal Read Model 尚未定案前，禁止加入 nullable placeholder；前端應將缺失區塊呈現為 `Pending` 狀態。 |
| **`DashboardSummaryDto` (`businessDate`)** | 後端目前以 UTC 日期作為 `businessDate`，對 UTC+8（台灣）使用者而言，凌晨 00:00–08:00 的「今日支出」會落後本地日期一天。 | Sprint 1 維持現有 `DateOnly` 契約並明確回傳 `businessDate` 供前端對齊；長期（P2）再評估導入使用者時區設定。 |
| **`RecentTransactionDto`** | 1. 缺乏 `title` 欄位（僅有可為 `null` 的 `note` 與 `categoryName`）。<br>

<br>2. 需確認 `transactionType` 是否已明確包含於 Swagger Schema 中。 | 1. 維持由 Backend 提供原始 `note` 與 `categoryName`，由前端 Mapper 決定顯示標題（`note || categoryName`）。<br>

<br>2. 強制確認 `transactionType` 存在於 DTO，禁止前端以金額正負號推測收支方向。 |
| **`AccountSummaryDto`** | `accountType` 若未明確定義序列化格式（字串 vs 整數），前端 Enum Mapping 容易因後端新增列舉而崩潰。 | 確認 OpenAPI/Swagger 統一採用字串或固定 Enum 契約，前端 Mapper 必須保留 `Unknown` 安全降級路徑。 |
| **`Calendar` / `CalendarDay` DTO** | 現有 Calendar API 僅聚合 `Transaction` 資料，未包含 `Fixed Commitments` 到期提醒與 `Important Dates`。 | **不於單一查詢強行耦合未完成領域**。先以純交易聚合上線，待 Fixed Commitment 領域完成後，再以獨立欄位或疊加查詢擴充月曆事件。 |

---

## 3. Calculation Responsibility Review

### Backend 責任（Single Source of Truth）

所有涉及**跨筆資料聚合、領域過濾規則與帳務口徑**的計算，必須完全由 Backend Query Layer 執行：

* **Total Assets**：於資料庫層過濾 `!IsDeleted && !IsSystemAccount` 後加總 `CurrentBalance`。
* **Today's Expense**：依 `businessDate` 過濾 `!IsDeleted && TransactionType == Expense` 加總。
* **Daily Income / Expense Summary**：月曆每日收支總額聚合，並嚴格排除 `Transfer` 不計入收支。
* **Budget Remaining & Spending Trend（未來）**：依後端正式 Budget / Cash Flow 規則計算已支出與剩餘額度。
* **Fixed Commitment Summary（未來）**：計算當月固定承諾總額、已付與待付金額。
* **Goal Funding Totals（未來）**：由 `GoalFundingSummary` Read Model 聚合各目標實際配置資金。

### Frontend 責任（Presentation & UX）

前端僅負責**視覺格式化、純展示衍生比例與互動狀態**：

* **Display Formatting**：透過 `Intl.NumberFormat` 格式化純數值金額（單一基準幣別，不做匯率換算）與安全 `DateOnly` 日期字串格式化。
* **Privacy Masking**：依據 `privacyStore` 統一將敏感金額轉換為遮罩符號（如 `••••••`）。
* **Pure UI Derived Metrics**：純顯示用的簡易計算（例如目標達成率 `current / target * 100`、每日可用預算 `remainingBudget / remainingDays`）。
* **Animation & UI State**：骨架屏（Skeleton）、展開收合動畫、選取日期狀態、Empty / Pending / Error 狀態切換與 `AbortController` 請求取消。

### 目前設計風險

1. **口徑分裂風險**：若前端自行加總 `accounts[]` 取代後端 `totalAssets`，一旦未來導入帳戶篩選或分頁，前後端總資產將會不一致。
2. **日期偏移風險**：若前端使用 `new Date("YYYY-MM-DD")` 解析後端 `DateOnly`，在負時區（如 `pt-BR`）會發生日期減一天的嚴重 Bug。
3. **真假資料混合風險（Frankenstein UI）**：若為了讓未完成區塊（Budget / Fixed Commitment / Goal）有畫面而繼續在 Mapper 保留 Mock fallback，會導致使用者看到「真實銀行餘額 + 假預算/假承諾」，嚴重破壞財務系統信任度。

---

## 4. API Missing List

| Priority | Missing API / Data | Reason |
| --- | --- | --- |
| **P0** | **`RecentTransactionDto.TransactionType`**（需核對是否已補齊） | 若缺少交易類型，前端無法正確區分收入（`+`）、支出（`-`）與轉帳，直接阻礙 Transaction Summary 與 Calendar Day Detail 正確渲染。 |
| **P1** | **Fixed Commitment Read API**（清單、月總額、到期日） | 缺失將導致 `Fixed Commitment` Widget 無法脫離 Mock，且 `Calendar` 無法顯示固定支出到期標記。 |
| **P1** | **Budget Radar Read API**（預算上限、已支出、剩餘額度） | 缺失將導致 `Budget Radar` Widget 與 `Hero` 的 Budget Status 只能維持 Pending 狀態。 |
| **P1** | **Goal Pyramid Read API**（`GoalFundingSummary` Read Model） | 缺失將導致 `Goal Pyramid` Widget 與 `Hero` 的 Allocation Summary 無法呈現真實目標配置進度。 |
| **P2** | **Calendar Important Dates API** | 屬於進階日程提醒功能，不影響核心帳務查詢，可於固定承諾與交易月曆穩定後擴充。 |
| **P2** | **Spending Trend / Historical Analytics** | 屬於趨勢分析增強功能，不阻礙 Dashboard V3 API-only 上線。 |
| **P2** | **User Timezone-aware `BusinessDate**` | 解決跨時區凌晨「今日支出」基準日落差，屬體驗優化項目。 |

---

## 5. Recommended Sprint Plan

### Sprint 1：Backend Contract Completion

* **鎖定既有契約**：核對並確認 `GET /api/dashboard/v3/summary`（含 `TransactionType`、`Accounts`、`BusinessDate`）與 `GET /api/dashboard/v3/calendar` / `day` 的 Swagger Schema 與 Integration Tests 全數通過。
* **定義 P1 領域 Read Model 規格**：依序確立 `Fixed Commitment`、`Budget Radar` 與 `Goal Pyramid`（`GoalFundingSummary`）的獨立 Feature API 契約，維持「Summary + Feature APIs」策略，不將新領域塞入 Summary。
* **實作 P1 Feature APIs**：依業務優先度逐一補齊上述唯讀 Query Layer（不修改核心 Domain Entity）。

### Sprint 2：Frontend API Migration（API-only Cutover）

* **徹底移除 Mock 依賴**：清除前端 Mapper 中所有 `mockDashboardData` fallback，將 `AuthApp` 正式切換至 `ApiDashboardDataSource`。
* **對接已就緒 API**：完成 `Hero`（Total Assets / Today's Expense）、`Transaction Summary` 與 `Calendar`（每日收支與單日明細）的 100% 真實資料串接。
* **補齊生命週期與狀態防呆**：為尚未接入 API 的 `Budget Radar`、`Fixed Commitment`、`Goal Pyramid` 實作明確的 `Pending` 狀態；全面補齊 `AbortController`、Empty State、Error State 與 `Retry` 機制。
* **漸進接入 P1 Widgets**：隨 Sprint 1 後端 Feature APIs 就緒，將對應區塊由 `Pending` 平滑切換為 `Ready`。

### Sprint 3：UX Optimization

* **載入與視覺體驗調優**：優化各 Widget 等高 Loading Skeleton，消除 API 載入時的版面跳動（CLS）。
* **隱私與多語系一致性驗收**：全面檢核 `privacyStore` 遮罩覆蓋率（Hero、Accounts、Transactions、Calendar），並驗證 `DateOnly` 與金額格式在五語系下皆無偏移。
* **P2 增強功能評估**：依實際使用回饋，評估導入使用者時區對齊 `BusinessDate`、Calendar Important Dates 與 Spending Trend 分析。