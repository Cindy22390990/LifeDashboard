Architecture Decision Record: Goal Funding Domain Model

Decision Status: Proposed (Ready for Acceptance)

1. Context

在 LifeDashboard Dashboard V2 Phase 0 完成後，我們必須為「Goal Funding（目標資金累積）」確立穩固的底層領域模型。

目前最大的架構盲點在於 Goal.currentAmount 的語意不明。為了解決這個問題，我們需要做出一系列核心決策：

Goal 實體本身是否應該保存資金狀態 (State)？

Transaction (金流事件) 與 Goal (目標) 之間的耦合度與依賴方向？

Funding Allocation 作為中介領域 (Intermediate Domain) 的歸屬與運作機制？

2. Considered Options

Option A: Transaction.GoalId (在交易直接加外鍵) - ❌ 不採用。缺乏彈性，無法一筆金流對應多目標。

Option B: GoalFundingAllocation Mapping Entity (分配中介模型) - ✅ 採用。完美實現責任分離。

Option C: Account Bucket Model (帳戶子集合模型) - ❌ 不採用。

Option D: Hybrid Manual Adjustment (手動+自動混合模式) - ❌ 不採用。

3. Recommended Domain Model

強烈建議採用 Option B 的延伸架構，將其定位為 Capability，並透過 Application Event 解耦：

       Transaction Domain 
               |
               | (Application Events / Notifications)
               ↓
       Goal Funding Capability (Application Concept)
               |
         GoalFundingAllocation
               |
               | (Update)
               ↓
       GoalFundingSummary (Read Model)
               |
               | (Belongs to)
               ↓
          Goal Domain


領域責任與邊界說明：

Transaction Domain：僅負責記錄真實金流事實（收入、支出、轉帳）。它不依賴 Goal Domain，也不包含 Goal 的商業規則。

限制：絕對禁止在 Domain Entity 建立雙向 ORM Navigation Property（如 Transaction.FundingAllocations），以防開發者透過 transaction.FundingAllocations.Add() 破壞 Aggregate Boundary。僅保留單向的 GoalFundingAllocation.TransactionId。

Goal Funding Capability：目前系統規模尚不需獨立為 Subdomain，將其定位為應用層級的能力 (Capability)。負責協調客觀金流與主觀目標的連接。

Goal Domain：負責目標的生命週期管理（期望金額、期限、狀態）。

4. CurrentAmount Decision

決策：currentAmount 不屬於 Goal Aggregate 的核心狀態，不應作為 Single Source of Truth (唯一真實來源)。

架構規範：若要保留 currentAmount 概念，它必須存在於 Application Projection，而非 Domain Entity。

正確的資料結構範例：

Domain Model: Goal { Id, Name, TargetAmount }

Read Model: GoalFundingSummary { GoalId, FundedAmount, UpdatedAt } 或 GoalProgressDto { GoalId, TargetAmount, CurrentAmount, ProgressPercentage }

5. Boundary Decision & Allocation Lifecycle

Transaction 是否知道 Goal？ 否 (No)。

Goal 是否知道 Transaction？ 否 (No)。

Funding Allocation 是誰建立的？ (Lifecycle)

決策：採用 Explicit Application Command (明確的使用者行為)。

流程：使用者建立 Transaction -> (獨立動作) 使用者發起 CreateGoalFundingAllocationCommand。

原因：避免系統過度推論（如誤判轉帳為存錢）。Transaction 的建立與 Allocation 的建立是分開的邊界。

6. Consequence

獲得的能力：完整的 Audit Trail；支援資金拆分；明確區分 Transfer 與 Funding。

增加的複雜度：需要維護跨 Aggregate 的狀態協調與 Read Model。

7. Application Event Strategy (Soft Delete 與資料同步)

在導入完整的 Event Bus 基礎建設前，採用 Application Event / Domain Notification 機制來處理同步：

Transaction Application Service 執行修改或 Soft Delete 後，發出 Notification (TransactionDeleted)。

Goal Funding Capability 內的 Handler 接收通知。

Handler 重新計算或撤銷失效的 GoalFundingAllocation，並更新 GoalFundingSummary。

8. MVP Implementation Boundary

Phase 1 MVP 不包含 (Explicitly Out of Scope)：

Automatic funding rules (自動存錢規則)

AI allocation suggestion (AI 建議配置)

Account bucket synchronization (帳戶子集合同步)

Investment valuation tracking (投資估值追蹤)

Phase 1 僅支持 (MVP Scope)：

User creates Transaction.

User explicitly creates GoalFundingAllocation.

Dashboard reads GoalFundingSummary.

鐵律：Funding must be explicit. System must not infer user intention.