# Dashboard V4 Architecture Decision Record (ADR)

> **Document Path**: `docs/DashboardV4ArchitectureDecisionRecord.md`
>
> **Status**: Accepted
>
> **Context**: 記錄 LifeDashboard 從 V3（財務控制中心）演進至 V4（日常消費決策工具）的關鍵架構決策與取捨理由，防止後續開發回退為 V3.5 混血架構。

---

## ADR-001：為什麼放棄 V3 Summary API，改採每頁最小資料集端點

* **Status**: Accepted
* **Context**:
  * V3 的 `GET /api/dashboard/v3/summary` 屬於單一聚合端點，承載總資產、今日支出、帳戶清單與近期交易。
  * V4 將產品定位收斂為「日常消費決策工具」，首頁（`Home`）只需要「今日可花、今日已花、本月進度、今日紀錄」，而總資產與帳戶清單已移至低頻的 `Me` 分頁。
* **Decision**:
  * 放棄在 V4 複用 `GET /api/dashboard/v3/summary`。
  * 拆分為兩個輕量專屬端點：`GET /api/dashboard/v4/home`（高頻消費決策）與 `GET /api/me/assets`（低頻資產安全感）。
* **Consequences**:
  * **優點**：首屏 Payload 極小化、消除過度抓取（Over-fetching）、避免 `Me` 打開時誤觸發整頁儀表板聚合查詢。
  * **代價**：後端需多維護一支輕量 Read Query，且 V3 與 V4 並存期間需維持舊端點不破壞。

---

## ADR-002：為什麼 Transaction 不繼續擴充 Calendar API

* **Status**: Accepted
* **Context**:
  * V3 已有 `GET /api/dashboard/v3/calendar` 與 `/calendar/day`，皆以「日期格子」為核心設計。
  * V4 的 `Transactions` 分頁以「找交易與快速補記」為目的，需要支援關鍵字搜尋、類型篩選與分頁（`page` / `pageSize`），月曆僅降級為前端 Filter UI。
* **Decision**:
  * 停止擴充 `/api/dashboard/v3/calendar`。
  * 交易查詢與建立全面回歸獨立的跨版本 Domain API：`/api/transactions`（含分頁查詢與 `POST /api/transactions/quick`）。
  * **API 版本策略（採方案 B）**：頁面專屬聚合 API 帶版本號（如 `/api/dashboard/v4/home`）；核心領域 API（`/api/transactions`、`/api/accounts`、`/api/goals`）為跨版本 Domain API，不綁定 Dashboard 版本號，並統一採小寫複數 REST 命名（逐步取代舊版 `POST /api/Transaction`）。
* **Consequences**:
  * **優點**：避免 Calendar API 膨脹成包含搜尋與分頁的怪物端點；明確區分「View API」與「Domain API」。
  * **代價**：新舊大小寫路由（`/api/Transaction` vs `/api/transactions`）於過渡期需並存。

---

## ADR-003：為什麼 Plan 不建立 Backend Plan Domain

* **Status**: Accepted
* **Context**:
  * V4 導覽列將「預算（Budget）」與「目標（Goal）」合併於同一個 `Plan` Tab 下，以符合使用者「控制未來」的心智模型。
* **Decision**:
  * **禁止建立 `GET /api/plan` 或後端 `Plan` 領域**。
  * 後端嚴格維持 `Budget`（`/api/budgets/current`）與 `Goal`（`/api/goals/summary`）兩個獨立 Bounded Context，由前端 `PlanPage` 透過 Segmented Control 組合 `BudgetView` 與 `GoalView`。
* **Consequences**:
  * **優點**：UI 改版不會反向污染後端領域邊界，兩個模組可獨立開發、獨立快取、獨立上線。
  * **代價**：使用者切換 `[預算]` 與 `[目標]` 時各自發出獨立請求（屬預期內的按需載入）。

---

## ADR-004：為什麼 V4 不依賴 V3 Component 與 V3 Store

* **Status**: Accepted
* **Context**:
  * 若直接在 `dashboard-v3/` 目錄內修改版面或沿用 `dashboardV3Store`，會導致新舊邏輯纏繞，最終變成「換皮不換骨」的 V3.5，且未來無法安全刪除 V3。
* **Decision**:
  * 建立完全隔離的呈現層目錄 `src/presentation/components/dashboard-v4/` 與獨立頁面（`HomePage`、`TransactionsPage`、`PlanPage`、`MePage`），**零 import `dashboard-v3/` 任何檔案**。
  * **Store 邊界策略**：僅保留跨頁純 UI 狀態（`privacyStore`）；嚴禁搬移或共用 `dashboardV3Store`；V4 各頁面資料流各自獨立（獨立 V4 Hooks / Stores 或後續統一導入 TanStack Query）。
* **Consequences**:
  * **優點**：V3 與 V4 可透過 Feature Flag 安全並存與瞬間切換，Phase 4-B 清理時可直接整包刪除 `dashboard-v3/`。
  * **代價**：過渡期會有少量 UI 元件並存，但架構債為零。