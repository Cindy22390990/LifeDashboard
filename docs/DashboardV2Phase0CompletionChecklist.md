# Dashboard V2 Phase 0 Completion Checklist

## 已完成項目

- [x] Dashboard 以 Financial Overview、Budget Radar Summary、Goal Summary
  作為主要資訊層級，依序呈現。
- [x] Financial Overview 顯示既有總資產、本月收入、本月支出，以及
  Dashboard summary 提供的今日剩餘預算額度。
- [x] 總資產採 Dashboard 本地 UI 遮蔽狀態，預設隱藏，可由使用者切換顯示。
- [x] Budget Radar 以摘要呈現，保留 Payday 預算週期語義。
- [x] Goal Summary 保留並顯示所有既有 Goal，未限制筆數。
- [x] Recent Activity 與 Account Details 在手機模式預設收合，並保留展開查看。
- [x] Dashboard Quick Add 改為入口按鈕，沿用既有交易新增流程。
- [x] 手機長 Goal 名稱及帳戶名稱可換行；次要內容可透過漸進揭露查看。

## 已驗證項目

- [x] 390px 手機 viewport：無水平溢位；Financial Overview、Budget Radar、
  Goal Summary 順序正確；Recent Activity 與 Account Details 預設收合。
- [x] 390px 手機 viewport：兩個收合區塊均可展開；Goal 清單完整保留，
  長名稱可換行；展開帳戶資料後，固定底部導覽未遮住最後一個帳戶。
- [x] 768px 平板及 1280px 桌面 viewport：無水平溢位；Recent Activity 與
  Account Details 展開顯示，且不顯示手機版收合控制。
- [x] 前次實作驗證紀錄：Frontend build 與 ESLint 通過。Build 有既有
  bundle size 警告；本次未重新執行 build 或測試。

## 未包含項目

- Backend、API、API contract、DTO、Domain、資料庫或 migration 變更。
- Spendable Amount、任何新財務公式或新增計算。
- API 隱私過濾、全站 Privacy Mode、權限機制或跨頁隱私狀態。
- Fixed Commitment 的新 Dashboard 呈現或計算。
- Goal Account Linking、Goal funding 行為或 Goal 與 Transaction 的新關係。
- 修改既有交易、帳戶或 Goal 的資料流與 CRUD 行為。
- 要求所有 Summary 區塊必須同時出現在第一個 viewport；手機捲動屬預期 UX。

## 未來 Phase 項目

- [ ] 若要擴大隱私遮蔽範圍或加入 API 支援，先完成 Privacy Mode 決策及
  相應 contract 設計。
- [ ] Spendable Amount 須待業務規則、保留語義及 ADR 核准後另行設計；
  Phase 0 不提供該數值。
- [ ] Goal 未來資金來源概念（Manual Contribution、Linked Account
  Allocation）須先完成 ADR、API 與 Domain 設計；必須區分帳戶餘額、
  Goal 配置金額與 Goal 支出金額。
- [ ] 隨 Goal 數量增加，未來可評估 Goal 分頁、收合或摘要模式；目前
  Phase 0 保留所有 Goal，不截斷清單。
