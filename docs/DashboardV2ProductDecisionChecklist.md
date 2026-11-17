# Dashboard V2 Product Decision Checklist

## 1. Decision Summary

The decisions below were confirmed by the product owner on 2026-11-17 and
define the Phase 0 frontend scope.

| Decision | Current Status | Options | Recommended |
|---|---|---|---|
| Dashboard 首頁定位 | 已核准 | A. 財務健康總覽；B. 每日記帳操作中心；C. 預算控制中心 | A. 財務健康總覽 |
| 第一屏資訊密度 | 已核准 | A. 3 個核心區塊；B. 5 個資訊區塊；C. 全部保留後重新排版 | A. 3 個核心區塊 |
| Total Assets 呈現 | 已核准；UI-only | A. 永遠顯示；B. 預設遮蔽、點擊查看；C. 設定頁控制；D. Dashboard 移除 | B. 預設遮蔽，可點擊查看 |
| Quick Add | 已核准 | A. 完整表單；B. 快速入口按鈕；C. 移出 Dashboard | B. 快速入口按鈕；完整表單不嵌入首頁 |
| Goal 顯示 | 已核准 | A. 首頁保留摘要；B. 保留完整 GoalPyramid；C. 移至 Goals 頁 | A. 首頁保留摘要 |
| Mobile Dashboard 層級 | 已核准 | A. Summary First；B. Accordion；C. Bottom Sheet／次級導覽 | A. 首屏優先呈現 Financial Overview、Budget Radar Summary、Goal Summary；其餘漸進揭露 |
| Mobile 次要資訊 | 已核准 | Recent Activity／Account Details 保持展開或收合 | 手機模式收合兩者；保留內容與功能 |
| Goal Summary 完整度 | 已核准 | 顯示全部 Goal；只顯示 Top 3；移至 Goals 頁 | 顯示全部 Goal，不限制 Top 3 |
| Goal 資金來源未來方向 | 概念方向已記錄；不屬 Phase 0 | Manual Contribution；Linked Account Allocation | 僅記錄概念；未來需 ADR/API/Domain 設計 |
| Privacy 範圍 | 已核准；限 Phase 0 Dashboard UI | A. Total Assets UI mask；B. 全站 UI mask；C. API／Hybrid 隱私處理 | A. 只遮蔽 Total Assets；不做 API 過濾、全站 Privacy Mode 或權限系統 |
| Account Details | 已核准 | A. 第一層；B. 第二層；C. 移出 Dashboard | B. 第二層 |
| Fixed Commitment 明細 | Phase 0 明確排除 | A. 不新增呈現；B. 新增呈現並規劃 API | A. 不新增呈現；保留既有 Budget Radar 行為 |
| Spendable Amount | Phase 0 明確排除；ADR-0001 仍 Proposed | A. 不提供；B. 以既有資料推算；C. 待領域決策 | A. 不計算、不顯示、不以其他欄位代替 |
| Full Transaction Management | 已核准移出首頁 | A. 保留完整管理；B. 僅保留入口 | B. 完整管理移出首頁 |

## 2. Dashboard 首頁定位

| 選項 | 使用者打開首頁想知道什麼？ | 第一眼資訊 | 主要認知負擔 |
|---|---|---|---|
| **A. 財務健康總覽** | 目前整體財務狀況如何？ | 有清楚標示語義與期間的 Total Assets、Budget 摘要及月收支概況 | 若把預算餘額說成可花用金額，會誤導；各數值期間必須清楚 |
| **B. 每日記帳操作中心** | 如何快速新增一筆收支？ | 新增交易入口、常用操作、近期活動 | 表單與操作會壓過財務概況；完整表單使首頁變長 |
| **C. 預算控制中心** | 本期預算還剩多少？ | Payday-period Budget Radar 與相關支出資料 | 使用者可能將預算餘額誤認為帳戶現金或 Spendable Amount |

**已核准：A. 財務健康總覽。**月曆月收支、Payday-period Budget 和
Total Assets 必須保留各自語義及期間，不得推導 Spendable Amount。

## 3. Dashboard 第一屏內容決策

| 方案 | 使用體驗 | 開發成本 | 未來擴充 |
|---|---|---|---|
| **A. 3 個核心卡片／區塊** | 首屏聚焦、容易快速掃描；細節需到次要區域查看 | 低至中；主要是重排現有元件 | 高；可將明細放入對應區段，不必增加首屏卡片 |
| **B. 5 個資訊區塊** | 提供較多上下文，但手機首屏可能需捲動，競爭感較高 | 中；需清楚處理不同版面寬度 | 中；新增資訊容易令首屏再次膨脹 |
| **C. 全部保留，只重新排版** | 使用者仍可直接看到所有內容，但首頁資訊負擔仍高 | 低至中 | 低；新增功能會繼續擠壓首頁 |

**已核准：A。**第一層採三個核心資訊區塊：Financial Overview、
Budget Radar Summary、Goal Summary。Quick Add 以入口呈現，不計為
主要資訊區塊；近期交易與帳戶明細列於第二層。

## 4. Total Assets 顯示決策

| 方案 | 隱私 | 使用頻率 | 財務焦慮 | 實作成本 |
|---|---|---|---|---|
| **A. 永遠顯示** | 最低；任何看得到畫面的人都能看到，且其他金額仍可透露財務狀況 | 最高，無需額外操作 | 可能讓不想常看總額的使用者感到壓力 | 最低，維持現況 |
| **B. 預設遮蔽，可點擊查看** | 可降低旁人直接看到總額的機會，但遮蔽其他金額前仍非完整隱私 | 查看時多一次操作 | 可減少預設曝光；預設遮蔽也可能降低資訊可見性 | 低至中；僅 UI 遮蔽不會移除 API 值 |
| **C. 設定頁控制** | 取決於設定是否全域，以及其他金額是否一併處理 | 偏低；變更後不必每次操作 | 可由使用者決定，但設定範圍不清會造成誤解 | 中；需決定範圍、預設與偏好保存方式 |
| **D. Dashboard 移除，只在資產頁查看** | 降低首頁曝光，但其他 Dashboard 數值仍可能揭露資訊 | 首頁較低；查看時需前往資產頁 | 對不想在首頁看到總額者較友善 | 低至中；需調整首頁及導覽，不改 API 就不代表 API 不再提供 |

**已核准：B，限 Total Assets 的 Dashboard UI mask。**預設遮蔽，使用者
可點擊查看。其他金融數值維持現有顯示行為。此決策不代表 API 過濾、
全站 Privacy Mode 或權限控制；遮蔽 UI 不等於 API 保護。ADR-0002
仍可保留 Proposed 狀態，供未來較廣泛的隱私決策使用。

## 5. Quick Add 決策

| 方案 | Dashboard 操作角色 | 首頁乾淨程度 | 開發／行為影響 |
|---|---|---|---|
| **A. 保留完整表單** | 首頁同時是記帳操作中心 | 最低；欄位與驗證佔據版面 | 最小改動，但最可能延續資訊過載 |
| **B. 改成快速入口按鈕** | 保留常用操作，首頁主要仍是總覽 | 較高；表單需要時才開啟 | 低至中；應重用既有表單及提交流程，不改 CRUD |
| **C. 移出 Dashboard** | 首頁只負責資訊呈現 | 最高 | 可能增加導覽步驟，需確認既有交易新增入口是否足夠 |

**已核准：B。**首頁顯示快速入口按鈕，不嵌入完整 Quick Add 表單；
既有新增流程與 CRUD 行為保持不變。

## 6. Goal 顯示決策

| 方案 | 首頁資訊量 | 使用者價值 | 範圍 |
|---|---|---|---|
| **A. 首頁保留摘要** | 中低；保留 Goal 脈絡而不呈現全部細節 | 支援總覽，完整細節仍可在 Goals 頁查看 | 只呈現既有資料；不新增 Goal 計算 |
| **B. 保留完整 GoalPyramid** | 高；名稱、狀態、金額及進度佔據較多注意力 | 可直接查看全部 Goal 細節 | 需確保不暗示 Goal 金額等於保留現金 |
| **C. 移至 Goals 頁** | 最低 | Dashboard 不再提供 Goal 即時脈絡 | 導覽更直接，但首頁失去摘要 |

**已核准：A。**Dashboard 保留 Goal Summary，並完整顯示目前所有 Goal，
不改成 Top 3、不修改 Goal Domain，也不新增 Goal 計算。手機仍將 Goal
Summary 列為第一層，不因手機版而截斷 Goal 清單。

**風險：**Goal 數量增加時，手機頁面可能過長。未來可評估 Goal 分頁、
收合或摘要模式；任何改變需另行確認，不是本次核准內容。

## 7. Mobile Dashboard and Goal Future Decisions

### 7.1 Mobile Dashboard 決策

**已核准：Summary First。**手機 Dashboard 第一層依序優先呈現：

1. Financial Overview
2. Budget Radar Summary
3. Goal Summary（完整顯示所有 Goal）

Recent Activity 與 Account Details 在手機模式預設收合，內容與功能仍
保留。其他明細以漸進揭露呈現。Budget Radar 數值保留 Payday-period
語義，不得改稱可支配現金或 Spendable Amount。

### 7.2 Goal Future Direction

Goal 未來可能支援 Manual Contribution 與 Linked Account Allocation。
Linked Account 只代表使用者明確配置的 Goal 資金，不代表該帳戶全部
餘額屬於 Goal。未來設計需分清 Account Balance、Goal Allocated Amount
及 Goal Spending Amount。此方向僅記錄概念，詳見
[DashboardV2GoalFundingConcept.md](./DashboardV2GoalFundingConcept.md)；
不核准新增 Entity、Transaction 關係、API、DTO 或 migration。

## 8. Phase 0 明確 Scope

### Approved Phase 0

**產品決策已確認。**以下列出已核准的 Phase 0 實作邊界：

- 第一層為 Financial Overview、Budget Radar Summary、Goal Summary；
  Goal Summary 完整顯示所有現有 Goal，不限制 Top 3。第二層為 Recent
  Transactions、Account Details；手機模式預設收合這兩個區塊。
- 手機採 Summary First；其他資訊漸進揭露，不刪除功能或資料。
- Full Quick Add Form 與 Full Transaction Management 移出首頁；首頁保留
  Quick Add 快速入口。
- Total Assets 預設以 UI mask 遮蔽，可由使用者點擊查看；其他金額不遮蔽。
- 可重構 DashboardPage、拆分純呈現元件並重整 CSS。
- 保留現有 API response、data flow、CRUD、交易行為與財務語義。
- 不計算、不顯示 Spendable Amount；不加入 Fixed Commitment 新呈現。
- 不修改 Backend API、DTO、Domain、Database 或 migration。
- 不新增 Goal 資金來源模型或 Goal／Transaction 關聯。

### Future / Blocked

- 全站或跨頁 Privacy Mode、非 Total Assets 金額遮蔽、API 過濾、
  權限系統，以及偏好同步／持久化不在已核准 Phase 0 範圍內。
- Fixed Commitment 明細需要未來另行核准資料來源及 API contract。
- Spendable Amount 仍受 ADR-0001 阻擋，不可進入 Phase 0。
- Goal 資金來源概念尚未形成可實作規則；須先完成未來 ADR/API/Domain
  設計。

## 9. Decision Questions

Phase 0 的 Dashboard 定位、Summary First、手機次要區塊收合與完整
Goal 顯示決策均已確認，沒有因此新增的 Phase 0 產品阻擋問題。Goal
資金來源、Spendable Amount 與全站 Privacy Mode 屬未來決策，不影響
本文件所列 Phase 0 範圍。

## References

- [DashboardV2DesignProposal.md](./DashboardV2DesignProposal.md)
- [DashboardV2ImplementationPlan.md](./DashboardV2ImplementationPlan.md)
- [DashboardV2ApiContract.md](./DashboardV2ApiContract.md)
- [DashboardV2Phase0UISpec.md](./DashboardV2Phase0UISpec.md)
- [DashboardV2MobileUXProposal.md](./DashboardV2MobileUXProposal.md)
- [DashboardV2GoalFundingConcept.md](./DashboardV2GoalFundingConcept.md)
- [ADR-0001-SpendableAmount.md](./ADR-0001-SpendableAmount.md)
- [ADR-0002-FinancialPrivacyMode.md](./ADR-0002-FinancialPrivacyMode.md)
