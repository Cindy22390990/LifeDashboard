# Dashboard V4 UX Architecture

> **Document Path**: `docs/DashboardV4UXArchitecture.md`
>
> **Version**: 1.1 (Domain Boundary & API Contract Refined)
>
> **Status**: Draft — V4 Information Architecture Baseline
>
> **Product Positioning**: 從「財務控制中心（ERP）」轉型為「日常消費決策工具」，同時保留 LifeDashboard 核心理念——**「錢不是剩多少，而是分配後還剩多少自由。」**

---

## 1. Information Architecture (IA)

底部導覽列收斂為四個符合日常心智模型的 Tab：

```text
Bottom Navigation
├── 首頁 Home（今天的財務狀態）
│   ├── 今日可花（dailyRemaining 主數字）＋ 今日已花（dailySpent）
│   ├── 本月財務進度（monthlyProgress：已儲蓄 / 已投資）
│   ├── 今天紀錄（精簡列表）
│   └── + 記一筆（Bottom Sheet）
│
├── 記帳 Transactions（找交易＋快速補記）
│   ├── 搜尋列 ＋ 月曆篩選器（純 Filter UI，預設收合）
│   ├── 月份切換 ＋ 單月收入／支出摘要
│   ├── 時間軸交易列表（依日期分組）
│   └── + 浮動按鈕（FAB，喚起 Bottom Sheet）
│
├── 規劃 Plan（控制未來）
│   ├── [預算] 視圖（Budget Bounded Context）：本月剩餘預算、固定支出預留摘要、分類進度條
│   └── [目標] 視圖（Goal Bounded Context）：目標進度清單（達成百分比與進度條）
│
└── 我的 Me（長期安全感與資料管理）
    ├── 資產總覽（總資產 ＋ 生活／儲蓄／投資帳戶分組）
    ├── 分類管理
    ├── 偏好設定（隱私遮罩、語系、提醒）
    └── 備份與匯出
```

---

## 2. Page Purpose

每個頁面嚴格只解決**一個核心問題**：

| Tab | 頁面目的 | 回答使用者的核心問題 |
| :--- | :--- | :--- |
| **首頁 Home** | **今天的財務狀態** | 「我今天能不能買？我的長期儲蓄與投資方向有沒有前進？」 |
| **記帳 Transactions** | **找交易（與快速記帳）** | 「錢花去哪了？剛買完的這筆我要立刻記下來。」 |
| **規劃 Plan** | **控制未來** | 「這個月會不會超支？我離夢想目標還有多遠？」 |
| **我的 Me** | **管理自己的資料** | 「我的總資產與各帳戶安全水位如何？如何管理分類與設定？」 |

---

## 3. Component Inventory

### 共用互動元件（Cross-Tab）
* **`BottomNavigation`**：四分頁底部導覽列（`Home` / `Transactions` / `Plan` / `Me`）。
* **`TransactionBottomSheet`**：統一記帳入口（由 Home 主按鈕與 Transactions FAB 共用，支援 `defaultDate`）。
* **`PrivacyToggle`**：全域金額遮罩開關（連結 `privacyStore`）。
* **`Toast`**：非阻斷式記帳成功回饋。

### 各頁面元件清單

| 頁面 | 必要元件 | 職責說明 |
| :--- | :--- | :--- |
| **Home** | `HomeHeader` | 顯示業務日期（如 `11/25`）與 `PrivacyToggle`。 |
| | `DailyDecisionHero` | 顯示 **今日可花（`dailyRemaining`）**、**今日額度（`dailySpendingLimit`）** 與 **今日已花（`dailySpent`）**。 |
| | `MonthlyProgressCard` | 顯示 **本月財務進度（`monthlyProgress.saved` / `invested`）**，維持長期方向感且避免「儲蓄」語意混淆。 |
| | `TodayTransactionList` | 僅顯示今日紀錄（精簡列：圖示＋標題＋金額），無資料顯示極簡空狀態。 |
| | `QuickLogButton` | 底部顯眼 `+ 記一筆` 按鈕，觸發 `TransactionBottomSheet`。 |
| **Transactions** | `SearchBar` | 關鍵字搜尋 ＋ 右側月曆篩選按鈕（`CalendarFilterPopover` 預設收合）。 |
| | `MonthSummaryStrip` | 月份切換器 ＋ 單月「收入 / 支出」一行摘要。 |
| | `TransactionList` | 依日期分組（今天、昨天、日期）之時間軸列表。 |
| | `AddTransactionFab` | 右下角固定 `+` 浮動按鈕，支援直接進交易頁記帳的習慣。 |
| **Plan** | `PlanSegmentControl` | 頂部 `[預算] [目標]` 切換器（純前端 UI 組合）。 |
| | `BudgetView` | 消費 Budget Context：顯示本月剩餘預算、固定支出預留摘要與分類橫向進度條。 |
| | `GoalView` | 消費 Goal Context：顯示目標卡片清單（名稱、百分比、進度條，點擊進第二層看詳情）。 |
| **Me** | `AssetOverview` | 顯示 **資產總覽（總資產）**，提供長期安全感。 |
| | `AccountList` | 依 `Cashflow`（生活）、`Saving`（儲蓄）、`Invest`（投資）分組顯示帳戶。 |
| | `SettingsMenuList` | 入口清單：分類管理、設定、備份。 |

---

## 4. Removal & Relocation List

為消除「財務 ERP」的視覺壓迫感，以下項目從首頁或 V3 介面移除／搬遷：

| 原有項目（V3 / Legacy） | 處置方式 | 原因與去向 |
| :--- | :--- | :--- |
| **首頁總資產卡片（Total Assets）** | **移至 `Me`** | 總資產代表長期安全感而非當日消費決策，移至 `Me` 頂部作為「資產總覽」。 |
| **首頁帳戶列表（AccountSummaryWidget）** | **移至 `Me`** | 帳戶餘額屬低頻檢視資訊，移至 `Me` 並依生活／儲蓄／投資分組。 |
| **首頁完整月曆（CalendarSection）** | **降級為 `Transactions` Filter UI** | 平時不佔版面，改為 `Transactions` 頂部可點開的日期篩選器，不再作為獨立頁面核心。 |
| **預算雷達圖（Budget Radar）** | **淘汰** | 手機端難以直覺判讀剩餘金額，改為 `Plan → BudgetView` 的橫向進度條。 |
| **目標金字塔（Goal Pyramid）** | **淘汰** | 手機端佔用過多垂直空間，改為 `Plan → GoalView` 的極簡進度條清單。 |
| **「責任」與「報表」頂層 Tab** | **淘汰／重組** | 系統思維分類造成迷航；固定責任併入 `Plan → BudgetView`，報表降級為次要層。 |
| **Legacy 獨立新增交易頁面** | **淘汰** | 統一收斂至 `TransactionBottomSheet`，消除路由跳轉摩擦。 |

---

## 5. API Requirements

嚴格遵守 **「每頁只取得必要資料」** 與 **「Bounded Context 分離」** 原則，禁止為了 UI Tab 硬造肥大的聚合 API。

### 5.1 Home API（首屏極簡消費決策）
* **設計原則**：
  * **計算責任歸屬 Backend**：Frontend 不得理解「今日可花」的演算法（無論後端採月預算平均、扣除固定責任與儲蓄後除以剩餘天數，或依 `AllocationRatio` 計算），Backend 直接提供具備明確業務語意的數值。
  * **拆解「儲蓄」語意**：以 `monthlyProgress` 明確區分 `saved`（儲蓄）與 `invested`（投資），對齊 App 原本的資金分配理念。
* **需要的 Payload**：
  ```json
  {
    "businessDate": "2026-11-25",
    "dailySpendingLimit": 650,
    "dailySpent": 350,
    "dailyRemaining": 300,
    "monthlyProgress": {
      "saved": 8000,
      "invested": 4000
    },
    "todayTransactions": [
      {
        "id": "guid",
        "note": "咖啡",
        "amount": 145,
        "transactionType": "Expense",
        "transactionDate": "2026-11-25"
      }
    ]
  }
  ```
* **明確排除（不可塞入 Home API）**：`accounts[]`、`goals[]`、`radar[]`、`charts[]`。

### 5.2 Transactions API（交易導向 RESTful 契約）
* **設計原則**：**不複用或擴充 `/api/dashboard/v3/calendar`**。Calendar 是日期導向，而 Transactions 是交易導向（含搜尋、篩選、分頁），硬擴 Calendar API 會導致端點再度肥大。Calendar 在 V4 僅為前端 Filter UI。
* **端點設計**：
  * `GET /api/transactions?year=2026&month=11&keyword=coffee&type=expense`（支援日期、關鍵字、類型篩選）
  * `GET /api/transactions/{id}`
  * `POST /api/transactions`
  * `DELETE /api/transactions/{id}`

### 5.3 Plan APIs（維持獨立 Bounded Context）
* **設計原則**：`Plan` 只是 UI Tab，**不是 Backend Domain**。**禁止建立 `GET /api/plan`** 將 Budget 與 Goal 綁在一起；維持兩個獨立後端領域，由前端 `PlanPage` 組合呈現。
  * **Budget Context API（Planned）**：回傳本月總剩餘預算、固定支出預留總額與各分類預算進度。
  * **Goal Context API（Planned）**：依賴 `GoalFundingSummary` Read Model，回傳目標進度清單。

### 5.4 Me API（獨立資產總覽）
* **設計原則**：**不複用 `GET /api/dashboard/v3/summary`**，避免打開 `Me` 時誤載入整個 Dashboard 的聚合資料，維持每頁最小資料集原則。
* **端點設計**：`GET /api/me/assets`
* **Response Example**：
  ```json
  {
    "totalAssets": 1620000,
    "accounts": [
      {
        "id": "guid",
        "name": "生活帳戶",
        "accountType": "Cashflow",
        "balance": 30000
      }
    ]
  }
  ```

---

## 6. Frontend Routing Boundary

為避免 Copilot 或後續開發直接把 V4 元件繼續堆進既有的 `dashboard-v3/` 目錄造成耦合，V4 必須建立清晰的獨立呈現層邊界：

```text
src/presentation/
├── pages/
│   ├── HomePage.tsx
│   ├── TransactionsPage.tsx
│   ├── PlanPage.tsx
│   └── MePage.tsx
│
└── components/
    └── dashboard-v4/
        ├── common/
        │   ├── PrivacyToggle.tsx
        │   ├── TransactionBottomSheet.tsx
        │   └── Toast.tsx
        ├── home/
        │   ├── DailyDecisionHero.tsx
        │   ├── MonthlyProgressCard.tsx
        │   └── TodayTransactionList.tsx
        ├── transactions/
        │   ├── TransactionList.tsx
        │   └── SearchBar.tsx
        ├── plan/
        │   ├── BudgetView.tsx
        │   └── GoalView.tsx
        └── me/
            ├── AssetOverview.tsx
            └── AccountList.tsx
```

* **隔離原則**：`dashboard-v4/` 內的元件可透過共用 Domain / Store 取得資料，但**禁止反向依賴 `dashboard-v3/` 的專屬版面元件**，確保未來淘汰 V3 時可整包安全移除。