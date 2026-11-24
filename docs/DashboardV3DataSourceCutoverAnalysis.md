# Dashboard V3 DataSource Cutover Analysis

> **Document Path**: `docs/DashboardV3DataSourceCutoverAnalysis.md`
>
> **Status**: Analysis Only (No Code Modification)
>
> **Goal**: 釐清 `/dashboard-v3` 路由的 DataSource 實際注入鏈，並評估從 Mock 切換至 API-only 的最小風險方案。

---

## 1. `/dashboard-v3` Route 入口分析

依據目前前端 Clean Architecture 分層，`/dashboard-v3` 路由的各層元件職責與核對重點如下：

*   **Route Component**：`DashboardV3Page`。屬於純 Presentation 層容器，負責接收已轉換的 `DashboardViewModel` 並分發給各 Widgets，不直接認識 `ApiDashboardDataSource` 或 `MockDashboardDataSource`。
*   **Loader**：目前架構採 React Hook 非同步抓取，**無**使用 React Router `loader` 預載資料（若本機路由檔有設定 `loader`，需確認是否重複抓取）。
*   **Provider / Composition Root**：`AuthApp`（或外層 Router 設定檔）。負責在應用程式進入點決定要實例化哪一個 `IDashboardDataSource`，並透過 Props 或 React Context 注入至 `DashboardV3Page`。
*   **Hook**：`useDashboardData`。負責接收 `IDashboardDataSource` 介面，管理 `isLoading`、`error`、`data` 與 `AbortController` 生命週期。

---

## 2. DataSource 取得流程

目前 `/dashboard-v3` 仍呈現 Mock 資料，代表注入鏈在最上游的 Composition Root（`AuthApp`）或 Hook 預設參數處，仍指向 `MockDashboardDataSource`：

```text
[Router: /dashboard-v3]
         │
         ▼
[AuthApp (Composition Root)]  ◄── 實際切換點（目前注入 MockDashboardDataSource）
         │ (props: dataSource 或 Context Provider)
         ▼
[DashboardV3Page]
         │
         ▼
[useDashboardData(dataSource)]
         │ (僅依賴抽象介面)
         ▼
[IDashboardDataSource]
         │
    ┌────┴────────────────────────────┐
    ▼ (目前啟用)                        ▼ (目標切換)
[MockDashboardDataSource]     [ApiDashboardDataSource]
    │                                 │
    ▼                                 ▼
[mockDashboardData]           [GET /api/dashboard/v3/summary]
                                      │
                                      ▼
                              [dashboardV3ApiMapper] (已移除 Mock fallback)
                                      │
                                      ▼
                              [DashboardViewModel]
```

---

## 3. 關鍵元件與切換點盤點

| 項目 | 預期位置 / 角色 | 涉入機制與本機核對重點 |
| :--- | :--- | :--- |
| **Mock DataSource 定義** | Infrastructure 層（如 `MockDashboardDataSource.ts`） | 實作 `IDashboardDataSource`，回傳靜態假資料。切換後應僅保留給測試與開發環境使用。 |
| **API DataSource 定義** | Infrastructure 層（如 `ApiDashboardDataSource.ts`） | 實作 `IDashboardDataSource`，呼叫後端 Summary API 並經由已淨化的 `dashboardV3ApiMapper` 輸出 ViewModel。 |
| **主要切換點** | `AuthApp`（或 `/dashboard-v3` 路由定義處） | 檢查 `DashboardV3Page` 掛載處傳入的 `dataSource` 實例，以及 `useDashboardData` 內部是否有寫死預設值為 `mockDashboardDataSource`。 |
| **DI / Factory** | Composition Root（目前可能尚未抽離獨立 Factory） | 若 `AuthApp` 直接 `import` 具體實作，建議抽離一層極簡 Factory 函式統一管理環境切換。 |
| **Context** | 視本機實作而定 | 若非透過 Props 傳遞，需檢查是否存在 `DashboardDataSourceContext` Provider 包裹 `/dashboard-v3`。 |
| **Zustand Store** | **不應涉入** | `privacyStore` 僅管理 UI 遮罩狀態；閒置的 `dashboardV3SummaryStore` 不應參與 DataSource 注入（應確認無引用後刪除）。 |
| **Hook** | `useDashboardData` | 僅負責消費 `IDashboardDataSource`，**不應**在 Hook 內部判斷要使用 Mock 還是 API。 |

---

## 4. 最小切換方案評估

針對將 `/dashboard-v3` 從 Mock 切換為 API 的三種方案進行架構權衡：

### 方案說明

*   **方案 A：直接將 Dashboard V3 改為 API DataSource**
    *   在 `AuthApp`（切換點）直接將注入對象由 `mockDashboardDataSource` 替換為 `apiDashboardDataSource`。
*   **方案 B：加入 Feature Flag（透過環境變數 + 極簡 Factory）**
    *   新增環境變數（如 `VITE_USE_MOCK_DASHBOARD`，預設 `false` 即 API-only），由 `dashboardDataSourceFactory` 依環境變數決定回傳 `ApiDashboardDataSource` 或 `MockDashboardDataSource`，`AuthApp` 改為呼叫 Factory。
*   **方案 C：保留 Mock fallback，API 成功時優先使用 API**
    *   在 `ApiDashboardDataSource` 或 `useDashboardData` 中，當 API 請求失敗（500 / 斷網 / 401）時自動 catch 並退回 `MockDashboardDataSource`。

### 綜合比較表

| 評估維度 | 方案 A（直接切換） | **方案 B（Feature Flag + Factory）** | 方案 C（API 優先 + Mock Fallback） |
| :--- | :--- | :--- | :--- |
| **修改檔案數量** | **最少**（1 個檔案：`AuthApp`） | **少**（2–3 個檔案：`AuthApp` + Factory + `.env`） | **中**（需修改 DataSource/Hook 並重新引入 Mock 依賴） |
| **上線與維運風險** | **中**（若後端異常，需改程式碼重新部署才能退回） | **最低**（預設 API-only，遇緊急異常可透過環境變數秒級切換，且不影響本地開發） | **極高（資料誠信風險）**：API 失敗時靜默顯示假餘額，嚴重誤導財務決策，且掩蓋真實 Bug |
| **是否影響 `AuthApp`** | 是（直接修改匯入與傳入物件） | **低**（`AuthApp` 改為依賴 Factory，反而解除對具體 DataSource 的耦合） | 是（或將污染移轉至 DataSource 層） |
| **是否符合 Clean Architecture** | ✅ 符合（在 Composition Root 替換實作） | ✅ **最符合**（標準 Composition Root + Factory 隔離） | ❌ **嚴重違反**（重新將測試用 Mock 綁進正式環境例外處理流程，違背 API-only 原則） |

### 架構師建議

*   **首選：方案 B（Feature Flag + Factory，預設走 API）**
    *   在極小的改動成本下（僅多一個 Factory 函式），既達成「正式環境 100% API-only」與「`AuthApp` 解耦」，又保留了開發除錯與緊急回退的安全閥。
*   **次選：方案 A（直接切換）**
    *   若目前專案處於個人/內部測試階段、不需要環境變數控管回退，直接採用方案 A 最為俐落。
*   **明確否決：方案 C**
    *   `dashboardV3ApiMapper` 好不容易才移除 Mock fallback，若在 DataSource 層又把 Mock fallback 加回來，屬於架構倒退。API 失敗時應誠實呈現 **Error State + Retry 按鈕**，絕不可用假數據掩蓋錯誤。

---

## 5. Cutover 前置檢核點（動工前必查）

在執行切換前，請於本機確認以下 3 件事：

1.  **確認無雙重預設值**：檢查 `useDashboardData` 內部沒有 `dataSource = mockDashboardDataSource` 這類預設參數，避免漏傳時悄悄退回 Mock。
2.  **確認 Error / Empty UI 已就位**：切換至真實 API 後，若遇到 401/500 或新用戶空陣列，`DashboardV3Page` 必須能正常顯示 Error State 與 Empty State。
3.  **確認 Build 產物乾淨**：若採方案 A，確認 `AuthApp` 已移除對 `MockDashboardDataSource` 的 unused import。