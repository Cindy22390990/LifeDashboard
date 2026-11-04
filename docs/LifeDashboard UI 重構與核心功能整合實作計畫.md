# LifeDashboard UI 重構與核心功能整合實作計畫

版本：v1.0

狀態：Implementation Plan Approved

專案：LifeDashboard

技術架構：
- Frontend：React + TypeScript + Vite + PWA
- Backend：.NET 10 Web API
- Architecture：Clean Architecture
- Database：SQL Server + EF Core
- Authentication：JWT
- State Management：Zustand
- HTTP Client：Axios

---

# 一、正式決策

本階段依照以下決策執行：

| 項目 | 正式決策 |
|---|---|
| 導覽方式 | 四大底部 Tab |
| 固定責任 | 改為直接關聯 Transaction |
| 收割今日結餘 | 先完成後端規格設計，暫不實作 |

四大主要分頁：

1. Dashboard
2. Responsibilities
3. Goal
4. Stats

Account 與 System Settings 改為獨立入口，透過 Dashboard 或其他適當位置開啟 Modal，不列為主要底部 Tab。

注意：Account 與 System Settings 原有功能必須保留，不得因導覽調整而刪除。

---

# 二、實作原則與修改限制

## 2.1 架構原則

必須維持：

- Clean Architecture 分層。
- Controller 不直接處理資料庫邏輯。
- Service 負責業務規則。
- Repository 負責資料存取。
- UnitOfWork 管理需要原子性的交易。
- UserId 一律從 JWT Claim 取得。
- 不信任前端傳入的 UserId。
- 所有資料查詢及異動必須遵守使用者隔離。
- 前端沿用既有 Axios、Repository、Zustand Store 架構。

## 2.2 Copilot 修改限制

每個 Phase 僅允許修改該階段必要的檔案。

禁止：

- 擅自重新設計 Clean Architecture。
- 大規模重構無關 Service 或 Repository。
- 修改 JWT Authentication。
- 修改既有帳戶負餘額政策。
- 修改 Goal Deposit / Goal Purchase 的業務規則。
- 修改 AllocationRatio 計算邏輯。
- 將前端計算結果取代後端正式計算結果。
- 引入新的前端 UI Framework。
- 新增與本階段無關的 API。
- 刪除既有功能或既有測試。
- 覆蓋、還原或刪除原本未提交的 Git 變更。
- 未經核准執行資料庫 Migration。
- 未經核准執行 Update-Database。

若發現現有程式與規格衝突，先列出問題及影響，不可自行擴大修改範圍。

## 2.3 Git 安全要求

開始前必須：

1. 執行 git status。
2. 記錄目前分支。
3. 記錄所有已修改、未追蹤及已刪除檔案。
4. 確認目前工作目錄是否存在其他未完成工作。
5. 不得使用 git reset、git clean、git restore 等可能破壞既有工作的操作。
6. 不得擅自覆蓋使用者已有修改。

本階段只允許修改明確核准的檔案。

---

# 三、Phase 0：實作前規格確認

狀態：Pending

目標：

確認目前 Repository 真實狀態，並建立實作依據。

Copilot 必須唯讀檢查：

1. 四大分頁目前的 Page、Component、Store、Repository。
2. 現有 React Router 路由。
3. Dashboard 與 TransactionPage 的功能重疊。
4. FixedCommitment Entity。
5. Transaction Entity。
6. FixedCommitment Service / Repository。
7. Transaction Service / Repository。
8. BudgetRadarService。
9. Monthly Report API。
10. Goal Deposit / Goal Purchase。
11. Account 負餘額政策。
12. 既有 Migration 與資料庫關聯。

產出：

- 現有架構圖。
- 功能與頁面對照表。
- FixedCommitment 與 Transaction 現有資料關係。
- 需要修改的檔案清單。
- Migration 影響分析。
- 潛在資料相容性問題。

限制：

本階段只能讀取及分析，不修改程式碼、Migration 或資料庫。

完成後等待核准。

---

# 四、Phase 1：四大底部 Tab 導覽重構

狀態：Pending

目標：

將目前七個頂部導覽入口整合成四大底部 Tab。

主要導覽：

Dashboard | Responsibilities | Goal | Stats

## 1. Dashboard

保留及整合：

- Budget Radar。
- Quick Add。
- 帳戶摘要。
- 最近交易。
- 今日結餘資訊。
- Account 入口。
- System Settings 入口。

注意：

收割今日結餘僅預留 UI 規劃，不實作業務操作。

## 2. Responsibilities

整合：

- FixedCommitment 列表。
- 固定責任新增。
- 固定責任編輯。
- 固定責任刪除。
- 固定責任付款狀態。
- 本期固定責任摘要。
- 閒錢預算資訊。

付款功能依 Phase 2 新規格實作。

## 3. Goal

整合：

- Goal 列表。
- Tier 分層。
- Goal 進度。
- Goal 新增。
- Goal 編輯。
- Goal Deposit。
- Goal Purchase。
- Goal 狀態。

必須沿用既有後端 API 及驗證規則。

不得重新實作 Goal 的金額計算。

## 4. Stats

整合：

- 年月切換。
- 收入統計。
- 支出統計。
- 分類統計。
- 收支結餘。
- Bucket Analysis。
- 收入／支出切換。

## 5. Account 與 System Settings

改為獨立 Modal 入口。

必須保留：

- Account CRUD。
- Account 負餘額設定。
- System Setting CRUD。
- 既有資料載入及儲存行為。

## 前端導覽驗收

- [ ] 底部固定顯示四大 Tab。
- [ ] Tab 切換正常。
- [ ] 目前選取 Tab 有明確狀態。
- [ ] Dashboard 可進入 Account。
- [ ] Dashboard 可進入 System Settings。
- [ ] 所有原有功能都有新入口。
- [ ] 不產生重複的交易表單。
- [ ] 登入保護仍正常。
- [ ] 手機及桌面版顯示正常。
- [ ] 不影響 PWA。

---

# 五、Phase 2：FixedCommitment 與 Transaction 關聯重構

狀態：Pending

優先級：P0

目標：

固定責任付款必須能直接關聯實際 Transaction。

本階段正式改變原本 FixedCommitment 與 Transaction 分離的設計。

## 2.1 資料模型設計

預計評估：

FixedCommitment：
- Id
- UserId
- Name
- Amount
- CategoryId
- EffectiveStartDate
- EffectiveEndDate
- IsActive
- 其他既有欄位

Transaction：
- 新增 Nullable FixedCommitmentId。
- 建立 FixedCommitment Navigation Property。
- 建立必要的 Foreign Key 與 Index。

上述欄位為設計方向，Copilot 必須先確認現有 Entity、DTO 及資料庫結構，再提出最終修改方案。

不得直接假設現有欄位名稱或關聯方式。

## 2.2 固定責任付款

使用者付款時：

1. 選擇固定責任。
2. 選擇付款帳戶。
3. 確認付款金額。
4. 確認付款日期。
5. 建立 Expense Transaction。
6. Transaction 設定 FixedCommitmentId。
7. 扣除付款帳戶餘額。
8. 更新固定責任付款狀態。

所有必要操作必須具備原子性。

不能發生：
- Transaction 建立成功但帳戶沒有扣款。
- 帳戶扣款成功但 Transaction 沒有建立。
- 固定責任已付款但沒有對應 Transaction。

## 2.3 取消付款

取消付款不能只是將 FixedCommitment 狀態改回未付款。

必須確認：

- 對應 Transaction 是否存在。
- 是否允許取消。
- 是否需要刪除原交易。
- 是否需要恢復 Account 餘額。
- 是否存在已被其他功能引用的交易。
- 是否會影響 Budget Radar 與 Monthly Report。

必須沿用既有 Transaction 刪除及餘額回補規則，不能自行重複實作另一套金額更新邏輯。

## 2.4 防止重複付款

必須設計：

- 同一固定責任在同一付款期間的唯一性。
- 重複 API 請求防護。
- 已付款項目的再次付款限制。
- Transaction 與 FixedCommitment 的一致性檢查。

必須先定義付款期間的識別方式。

不可只依賴前端按鈕 Disabled 防止重複付款。

## 2.5 舊資料 Migration

必須先分析舊資料：

- 既有 FixedCommitment。
- 已存在的 Expense Transaction。
- 付款月份。
- Transaction Category。
- UserId。
- 是否存在重複記帳。

不得自行將相同金額、相同日期的 Expense 判定為固定責任付款。

若無法可靠判斷既有交易歸屬，必須保留未關聯狀態，並提出人工回補方案。

## 2.6 Budget Radar 與 Reports

必須確認：

- 固定責任付款只計算一次。
- 不會同時被 FixedCommitment 與 Living Expense 重複扣除。
- Responsibility 類別付款仍能正常分類。
- Monthly Report 的收入、支出及分類統計正確。
- Dashboard Radar 使用一致的後端計算結果。

## Phase 2 驗收

- [ ] FixedCommitment 可建立關聯 Transaction。
- [ ] 付款可建立 Expense。
- [ ] 付款與 Account 扣款具備原子性。
- [ ] 同一期間不能重複付款。
- [ ] 取消付款能正確回補帳戶餘額。
- [ ] 不允許錯誤刪除或修改受固定責任管理的交易。
- [ ] 舊資料不會被錯誤關聯。
- [ ] Budget Radar 不重複扣算。
- [ ] Monthly Report 不重複統計。
- [ ] 多使用者資料隔離正常。

---

# 六、Phase 3：Goal 前端功能整合

狀態：Pending

優先級：P0

目標：

補齊後端已存在但前端尚未完整使用的 Goal 功能。

範圍：

1. Goal 新增 Modal。
2. Goal 編輯 Modal。
3. Goal Purchase Modal。
4. Goal Deposit Modal 整合。
5. Goal 狀態顯示。
6. Tier 分層顯示。

限制：

- 沿用現有 Goal API。
- 沿用既有 Goal CRUD Store / Repository。
- Purchase 必須呼叫既有後端 API。
- 不在前端自行計算 Goal.CurrentAmount。
- 不允許繞過後端 Purchase 驗證。
- 不修改 Goal Deposit / Purchase 核心業務規則。

驗收：

- [ ] Goal CRUD 正常。
- [ ] Deposit 正常。
- [ ] Purchase 正常。
- [ ] 完成狀態正常。
- [ ] 帳戶餘額正常。
- [ ] Transaction 正常。
- [ ] 使用者資料隔離正常。

---

# 七、Phase 4：Stats 與 Bucket Analysis

狀態：Pending

優先級：P1

目標：

整合月報及資產分配分析。

功能：

- 年月選擇。
- 收入／支出切換。
- 分類統計。
- Bucket Analysis。
- 月收入。
- 月支出。
- 月結餘。

實作前確認：

- Monthly Report DTO 欄位。
- BucketType 定義。
- AllocationRatio 資料來源。
- BudgetRadarService 的計算結果。
- 前端是否已經取得所需資料。

原則：

後端為 Single Source of Truth。

前端只負責顯示與互動，不自行重新計算正式預算或資產分配比例。

若現有 DTO 不足，先提出最小必要 API 修改方案。

不得為了 UI 方便而複製一套計算邏輯。

驗收：

- [ ] 年月切換正常。
- [ ] 收入／支出切換正常。
- [ ] 分類統計正確。
- [ ] Bucket Analysis 正確。
- [ ] 與後端計算結果一致。
- [ ] 無資料時有合理空狀態。

---

# 八、Phase 5：Dashboard 資訊整合

狀態：Pending

優先級：P1

目標：

將 Dashboard 整理成主要財務資訊入口。

功能：

1. 資產總覽。
2. 帳戶餘額摘要。
3. Budget Radar。
4. 今日結餘。
5. 最近交易。
6. Quick Add。
7. Account 入口。
8. System Settings 入口。

限制：

- 優先使用現有 Account、Transaction、Radar API。
- 不重複建立已有 API。
- 不在前端自行計算後端負責的正式金額。
- 不影響既有記帳流程。
- 不將未完成的 Harvest 假功能當作正式操作。

驗收：

- [ ] Dashboard 資訊完整。
- [ ] 帳戶餘額與 Account API 一致。
- [ ] 最近交易與 Transaction API 一致。
- [ ] Radar 與 BudgetRadarService 一致。
- [ ] Quick Add 正常。
- [ ] 不重複建立交易。

---

# 九、Phase 6：Harvest 今日結餘後端規格設計

狀態：Specification Only

優先級：P2

本階段只設計，不實作。

目標：

定義如何將當日可收割結餘轉入指定帳戶。

必須先定義：

## 9.1 結餘來源

- 今日結餘如何計算。
- 收入與支出採用哪些資料。
- 固定責任如何納入。
- 是否排除已分配預算。
- 是否排除 Goal 資金。
- 是否允許負數。

## 9.2 收割目的

- 收割至哪個 Account。
- 是否允許多個目的帳戶。
- 是否允許分配比例。
- 是否支援部分收割。
- 是否允許收割後再次調整。

## 9.3 防止重複收割

- 每日收割識別。
- 重複請求防護。
- 同一天多次收割的處理方式。
- 已收割後新增交易的處理方式。
- 取消或回復收割的規則。

## 9.4 後端設計

評估：

- Harvest Entity。
- Harvest Transaction 關聯。
- Harvest API。
- Harvest Service。
- Repository。
- UnitOfWork。
- Migration。
- 交易原子性。
- 多使用者隔離。

## 9.5 最終產出

- Harvest 業務規格。
- 計算公式。
- API Request / Response。
- Entity 設計。
- 資料庫關聯圖。
- 邊界條件。
- 測試案例。
- Migration 方案。
- 風險清單。

本階段禁止修改正式程式碼。

等待使用者核准後才進入實作。

---

# 十、全階段驗收標準

每個 Phase 完成後必須提供：

1. 修改檔案清單。
2. 每個檔案的修改原因。
3. 新增及修改的 API。
4. Entity / DTO / Migration 變更。
5. 單元測試結果。
6. 前端 Build 結果。
7. Git diff 檢查。
8. 未完成項目。
9. 已知風險。
10. 是否可以進入下一階段。

測試原則：

- 不得刪除既有測試來讓測試通過。
- 不得以 Mock 測試取代必要的 SQL Server 整合測試。
- Migration 必須先審查 Up / Down。
- 未經核准不得更新正式或共用資料庫。
- 測試失敗必須先分析原因，不得擅自擴大修正範圍。

---

# 十一、建議執行順序

| 順序 | Phase | 優先級 | 執行方式 |
|---|---|---|---|
| 1 | Phase 0 規格確認 | P0 | 唯讀 |
| 2 | Phase 1 四大 Tab | P0 | 前端 |
| 3 | Phase 2 固定責任關聯 | P0 | 後端優先，接續前端 |
| 4 | Phase 3 Goal 整合 | P0 | 前端 |
| 5 | Phase 4 Stats | P1 | 前後端視 DTO 需求 |
| 6 | Phase 5 Dashboard | P1 | 前端整合 |
| 7 | Phase 6 Harvest 規格 | P2 | 唯讀設計 |

注意：

Phase 2 必須先完成資料模型與業務規格核准，才能進入實作。

---

# 十二、Copilot 每階段執行指令

每次只提供一個 Phase 的工作。

Copilot 必須遵守：

「先盤點、再提出修改計畫、等待核准、最後實作。」

不得自行從下一個 Phase 延伸工作。

當前階段驗收完成後停止，等待下一個明確指令。

最終目標：

讓 LifeDashboard 具備一致的四大導覽、完整的固定責任交易關聯、完整 Goal 操作、可驗證的統計分析，以及可獨立規劃的 Harvest 功能。

所有功能必須維持既有財務資料正確性、交易原子性與使用者資料隔離。