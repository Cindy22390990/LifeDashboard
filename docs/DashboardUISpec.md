# Phase 4 Dashboard UI 設計前置文件

## 目的與範圍

本文整理 Dashboard UI 的資訊架構、元件責任及 Budget Radar API 串接方式，供後續實作與設計確認使用。

本文件只描述前端規劃，不變更 Frontend Code、Backend API 或 API Contract。資料欄位與錯誤狀態以目前 Budget Radar 實作為準。

## 1. Dashboard 頁面資訊架構

建議在既有已登入 Dashboard 內容中新增預算雷達區塊，放在頁面主要內容上方，讓使用者優先看到今日可用預算。

由上而下建議排列：

1. **頁面標題與日期**
   - 顯示「Dashboard」或「預算雷達」。
   - 顯示目前查詢日期；可提供重新整理動作。
2. **每日預算摘要**
   - 今日安全額度 `DailySafeLimit`。
   - 今日已支出 `TodaySpent`。
   - 今日剩餘 `TodayRemaining`。
   - 剩餘天數 `RemainingDays`。
3. **薪資週期預算摘要**
   - 本週期生活預算 `TotalLivingBudget`。
   - 若 `TodayRemaining` 或每日額度為負值，顯示超支狀態。
4. **未分類支出提醒**
   - 當 `UncategorizedExpenseAmount > 0` 時顯示提醒與金額。
   - 可提供前往交易記帳頁的導引；不應將未分類金額混入生活預算計算。

目前 API 沒有回傳薪資週期起迄日期或未分類交易明細，因此第一版不應將週期日期或未分類交易筆數呈現為 API 資料。

## 2. React Component 拆分建議

建議以頁面容器負責資料請求，展示元件只接收明確 props：

```text
DashboardPage
├── DashboardHeader
├── BudgetRadarPanel
│   ├── DailySafeLimitCard
│   ├── TodaySpentCard
│   ├── TodayRemainingCard
│   └── RemainingDaysCard
├── TotalLivingBudgetCard
├── BudgetOverspendingNotice
└── UncategorizedExpenseNotice
```

責任建議：

- **DashboardPage**：管理日期、載入／錯誤狀態，呼叫 API 並將 DTO 傳給子元件。
- **DashboardHeader**：呈現頁面標題、目前日期及重新載入操作。
- **BudgetRadarPanel**：組合每日摘要卡片，不負責 API 呼叫。
- **每日摘要卡片**：各自呈現單一金額或天數；金額只負責格式化，不重算預算。
- **BudgetOverspendingNotice**：根據傳入的負值呈現警示樣式與文字，不改寫數值。
- **UncategorizedExpenseNotice**：金額大於零時提示使用者補上分類；不推算筆數或交易清單。

可按專案現有慣例將 API 存取與 DTO 型別放在 infrastructure/domain 層，避免頁面直接散落 HTTP 細節。此為建議，不要求新增全域 Store。

## 3. API 呼叫流程

### Endpoint

```http
GET /api/budget/radar?currentDate=yyyy-MM-dd
Authorization: Bearer <JWT>
```

### 流程

1. Dashboard 初次顯示時，以瀏覽器當地日期建立 `currentDate`，格式化為 `yyyy-MM-dd`。
2. 呼叫 `GET /api/budget/radar`，將日期作為必填 query parameter。
3. 使用既有 HTTP client；其 request interceptor 會從登入狀態附加 JWT。
4. 不傳送 `UserId`。API 依 JWT Claims 決定使用者。
5. 收到成功回應後，依 DTO 欄位更新畫面。
6. 使用者手動重新載入時，可用同一日期再次請求；若日期可由使用者變更，日期改變後重新請求。

格式化日期時應使用瀏覽器當地年月日組成日期字串，避免先轉成 UTC ISO timestamp 後因時區造成日期偏移。缺少日期或格式錯誤時 API 回傳 400；前端不得以伺服器日期假裝成功取得資料。

### 狀態碼

| HTTP | 意義 | UI 建議 |
|---|---|---|
| 200 | 回傳 RadarStateDto | 顯示預算資料 |
| 400 | `currentDate` 缺少或格式錯誤 | 顯示日期／請求錯誤，允許重試 |
| 401 | JWT 缺少或無效 | 交由既有認證流程導回登入 |
| 404 | 使用者尚無 SystemSetting | 說明需先完成系統設定，提供前往設定頁的導引 |
| 其他／網路錯誤 | API 或連線失敗 | 顯示一般載入失敗狀態，提供重試 |

## 4. DTO Mapping

API 成功回應的 DTO 欄位如下。ASP.NET Core 預設 JSON 命名會以 camelCase 呈現：

```json
{
  "dailySafeLimit": 322.58,
  "todaySpent": 85.50,
  "todayRemaining": 237.08,
  "remainingDays": 10,
  "totalLivingBudget": 5000.00,
  "uncategorizedExpenseAmount": 120.00
}
```

| API 欄位 | 型別 | Dashboard 顯示 |
|---|---|---|
| `dailySafeLimit` | number / decimal | 今日安全額度 |
| `todaySpent` | number / decimal | 今日已支出 |
| `todayRemaining` | number / decimal | 今日剩餘 |
| `remainingDays` | number / integer | 本週期剩餘天數（包含今天） |
| `totalLivingBudget` | number / decimal | 本薪資週期生活預算 |
| `uncategorizedExpenseAmount` | number / decimal | 本薪資週期未分類支出金額 |

金額均依 API 的兩位小數規則顯示。前端可以使用 `Intl.NumberFormat` 等方式套用貨幣與地區格式，但不可再次執行預算計算或將負值歸零。實際貨幣代碼／符號應依產品既有設定，不由此 DTO 推定。

## 5. Loading / Error / Empty State

### Loading

- 首次請求：顯示摘要卡片 skeleton 或明確的載入狀態，不顯示預設的 0 取代尚未取得的資料。
- 重新載入：可保留上一筆成功資料並標示更新中，避免畫面閃爍成空值。
- 請求完成後才解除 loading。

### Error

- 400：顯示「無法取得預算資料，請確認查詢日期」並提供重試。
- 401：使用既有登入逾時／未授權處理，不顯示一般空資料。
- 404：顯示尚未完成系統設定的說明與設定頁入口。
- 網路或 5xx：顯示無法載入及重試操作；錯誤狀態不得被轉成成功但空白的預算畫面。

若重新載入失敗且保留舊資料，需明確標示資料未能更新，避免使用者誤認為是當前結果。

### Empty

目前 RadarStateDto 沒有 `isEmpty`、交易筆數或資料存在旗標；API 成功回傳的零值是有效數值，不能單憑金額為零判定 API 空回應。

- 六個欄位完整且為 0：仍依成功狀態顯示數值 0。
- `UncategorizedExpenseAmount` 為 0：不顯示未分類提醒。
- 真正的空 body 或無法解析的 response：視為 API／資料錯誤，不當成零值。
- `remainingDays` 應為 API 回傳值；前端不自行推算或用零值替代。

## 6. 負值預算顯示規則

規格允許 `DailySafeLimit` 與 `TodayRemaining` 為負值；負值代表預算超支，API 不會將其 clamp 成 0。

- 原樣呈現負號及金額，例如 `-$25.00`（實際符號依產品貨幣設定）。
- 使用警示色、圖示或清楚的「已超出每日額度」／「預算已超支」文字補充語意。
- 負數不應以一般正向綠色狀態呈現。
- 不得將負值改成 `$0.00`、取絕對值，或只顯示警告而隱藏實際金額。
- `TodaySpent` 是支出總額，依 API 值呈現；不以負數邏輯反向解讀。

## 7. 未分類支出提醒 UI

當 `uncategorizedExpenseAmount > 0`：

- 顯示醒目的資訊／提醒卡片，例如「本薪資週期有未分類支出」。
- 呈現未分類支出總額，金額使用兩位小數格式。
- 說明補上分類有助於正確檢視支出。
- 可提供前往交易頁的操作入口；目前 API 不提供未分類交易清單，因此不要假設可由 Radar 回應直接定位特定交易。

當金額為 0 時不顯示提醒，或以低干擾方式呈現「目前沒有未分類支出」。此金額只作提醒用途，不得再從 `TotalLivingBudget`、`DailySafeLimit` 或 `TodayRemaining` 扣除。

