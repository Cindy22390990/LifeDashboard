# Dashboard V3 Backend Production Readiness Review

**結論：READY（可進入 Production Integration Phase — Summary Scope）**

以目前前端已完成 API-only 整合的狀態回推，Backend 在 **Dashboard V3 Summary 核心範圍**已滿足上線條件，無 P0 阻塞項；未實作領域亦已透過獨立 Feature API 策略安全隔離。

---

## 1. Dashboard Summary API 是否完整支援目前 Frontend Contract

**結論：完整支援（Fully Aligned）**

`GET /api/dashboard/v3/summary` 已完整覆蓋目前前端首屏所需的全部核心契約，四層對齊（Backend DTO → Swagger → Frontend DTO → Mapper）無 Drift：

| 檢查維度 | 審查結果 | 說明 |
| --- | --- | --- |
| **Endpoint** | ✅ Ready | `GET /api/dashboard/v3/summary`，採 JWT Bearer 驗證，無額外 Query Parameters。 |
| **核心摘要欄位** | ✅ Ready | `totalAssets`、`todayExpense`、`businessDate` 皆已具備，前端不需自行重算帳務。 |
| **Canonical Enum** | ✅ Ready | `accounts[].accountType`（`Cashflow` / `Saving` / `Invest`）與 `recentTransactions[].transactionType`（`Expense` / `Income` / `Transfer`）皆提供字串規格，並保留 legacy `type` 維持向後相容。 |
| **日期與空值契約** | ✅ Ready | `businessDate` 與 `transactionDate` 統一採 `YYYY-MM-DD`；`note` 明確標示為 `string | null`，且集合欄位無資料時回傳 `[]`。 |
| **Swagger NRT** | ✅ Ready | 已啟用 `SupportNonNullableReferenceTypes()`，OpenAPI Schema 與 C# Nullable 行為一致。 |

---

## 2. Budget Radar / Goal Progress / Calendar 是否有明確 API Strategy

**結論：有明確策略，且符合 Clean Architecture 演進原則**

專案已確立 **「Summary + Independent Feature APIs」** 策略，避免將不同領域的聚合查詢無限制塞入 `summary` 造成效能瓶頸與耦合：

| 區塊 | API Strategy | 目前狀態 |
| --- | --- | --- |
| **Calendar** | 拆分為兩支獨立按需端點：`GET /api/dashboard/v3/calendar`（月聚合）與 `GET /api/dashboard/v3/calendar/day`（單日明細），直接複用既有 `Transaction` 欄位，免改 Domain。 | 契約已定義，排入下一階段（Sprint 2）串接。 |
| **Budget Radar** | 獨立 Feature API，待 Budget Domain 與 Cash Flow 計算口徑確立後定義 Read Model。 | 前端呈現 `Pending` State，不阻塞上線。 |
| **Goal Progress** | 獨立 Feature API，依賴未來 `GoalFundingSummary` Read Model 聚合實際配置資金。 | 前端呈現 `Pending` State，不阻塞上線。 |

---

## 3. 是否仍存在 Backend Contract Gap

依據嚴重程度分類，目前**沒有任何阻塞 Production Integration 的 Gap**：

* **Blocking Gap（P0）**：**無**。
* **Non-blocking Gap（P1 — 屬後續 Sprint 範疇）**：
* `Calendar`、`Budget Radar`、`Goal Progress`、`Fixed Commitment` 的獨立 Feature API 尚未全數接入（前端已安全降級為 `Pending`）。
* `RecentTransactionDto` 目前未包含 `categoryName` 與 `accountName`（前端已以 `note` / `transactionType` + `transactionDate` + `amount` 安全渲染）。


* **Enhancement / Tech Debt（P2）**：
* **`businessDate` 時區落差**：後端目前以 UTC 日期作為基準日，UTC+8 使用者在凌晨 00:00–08:00 看到的「今日支出」會落後本地日期一天。此屬體驗優化，不阻塞本次上線。



---

## 4. 是否可以進入 Production Integration Phase

**決策：GO（建議直接進入 Production Integration Phase）**

在正式環境放量或合併前，只需完成以下 **4 項後端 Pre-flight Smoke Check**（約 5 分鐘）：

1. **新用戶空資料測試**：以無帳戶、無交易的新帳號呼叫 `GET /api/dashboard/v3/summary`，確認回傳 `200 OK`、`totalAssets: 0`、`todayExpense: 0`、`accounts: []`、`recentTransactions: []`（絕不可回傳 `null` 陣列）。
2. **`Note` 為 `NULL` 測試**：確認資料庫 `Transaction.Note IS NULL` 時，API 正常序列化為 `"note": null` 且不觸發 500 例外。
3. **錯誤與權限邊界**：確認無 Token 或過期 Token 回傳 `401 Unauthorized`，非預期例外回傳標準 RFC 7807 `ProblemDetails` 且不洩漏 Stack Trace。
4. **環境組態確認**：確認正式環境的 CORS 白名單與 JWT Issuer/Audience 與前端部署網域一致。