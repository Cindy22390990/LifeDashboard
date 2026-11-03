# Category & Reporting Phase 3-4 Frontend UI Specification


# 目標

建立 LifeDashboard 前端報表介面。

本階段目的：

1. 串接後端 Monthly Report API。
2. 顯示使用者月度收入與支出。
3. 顯示 Category 分類分析。
4. 建立未來 Dashboard 與 Chart 擴充基礎。


---

# Phase 3-4 開發限制


開始前必須遵守：

1. 不修改 JWT Token 架構。
2. 不修改 Login/Register。
3. 不修改 Backend API Contract。
4. 不在 Frontend 計算報表。
5. 不修改 Transaction Backend 流程。
6. 不新增 Database Migration。
7. 不把商業邏輯寫在 React Component。
8. 所有報表資料來源必須來自 Backend API。


---

# 開發前確認


開始修改前確認：

- React 專案結構
- Router 設計
- Zustand Store
- Axios Instance
- API Service Pattern
- Existing Page Pattern
- Existing Component Pattern


---

# Backend API


使用：


GET
/api/reports/monthly?year={year}&month={month}


Request：

Query Parameter:

|名稱|型態|
|-|-|
|year|number|
|month|number|


禁止：


userId

由 JWT 自動處理。


---

# Frontend Data Flow


架構：



Page
|
Component
|
Store / Hook
|
API Service
|
Backend API


禁止：

Component 直接呼叫 Axios。


---

# API Service


新增：



reportService.ts


責任：

- 呼叫 Report API
- 處理 Response Type


不負責：

- 計算收入
- 計算支出
- 分類排序


---

# Type Definition


新增：


## MonthlyReport


欄位：

|欄位|型態|
|-|-|
|year|number|
|month|number|
|totalIncome|number|
|totalExpense|number|
|balance|number|
|categories|CategoryBreakdown[]|


---

## CategoryBreakdown


欄位：


|欄位|型態|
|-|-|
|categoryId|string|null|
|categoryName|string|
|bucketType|string|null|
|income|number|
|expense|number|
|transactionCount|number|


---

# UI Layout


建立：



MonthlyReportPage


頁面包含：


## 1. 月份選擇


功能：

- 選擇年份
- 選擇月份
- 重新取得資料


---

## 2. Summary Card


顯示：


收入：


TotalIncome


支出：


TotalExpense


結餘：


Balance


注意：

前端只顯示。

禁止重新計算。


---

## 3. Category Breakdown


顯示：


|分類|收入|支出|筆數|
|-|-|-|-|


資料來源：


categories


---

# Chart


本階段只建立資料結構。


可以使用：

- Recharts
- Chart.js


允許：

- Bar Chart
- Pie Chart


禁止：

- 前端自行計算比例
- 前端重新 Group


---

# State Management


如果需要全域狀態：

使用既有 Zustand Pattern。


例如：



reportStore


負責：

- report data
- loading
- error
- fetch action


---

# Error Handling


需要處理：


401：

重新登入流程。


400：

顯示月份錯誤。


500：

顯示系統錯誤。


---

# Responsive


至少支援：

- Desktop
- Tablet


Mobile 不要求完整最佳化。


---

# Phase 3-4 完成條件


完成：

- MonthlyReport Page
- API Service
- Type Definition
- Loading State
- Error State
- Summary UI
- Category List
- Chart Component


不包含：

- 預算系統
- AI 分析
- 投資建議
- 預測模型
- 完整 Dashboard

Transfer 時：

若來源帳戶餘額不足：

請依目前系統規則處理。

如果目前系統允許負數，

維持一致。

如果目前系統禁止負數，

回傳 Validation Error。

不要新增新的商業規則。