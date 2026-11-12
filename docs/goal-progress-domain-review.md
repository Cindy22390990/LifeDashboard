# Current Flow

- 建立 Goal 時，[Goal entity](../LifeDashboard.Domain/Entities/Goal.cs) 將 `CurrentAmount` 設為 0、`Status` 設為 `Active`。`GoalService.CreateAsync` 只負責建立和保存 Goal；Create API 不接受目前金額或狀態。
- Goal 進度由 `POST /api/goals/{id}/deposit` 更新，不是由一般帳戶轉帳推算。[GoalDepositService](../LifeDashboard.Application/Services/GoalDepositService.cs) 依使用者載入 Goal、驗證狀態和金額，開啟 unit of work，呼叫 `TransactionService.CreateTransactionWithinExistingTransactionAsync` 建立轉帳，接著呼叫 `Goal.ApplyDeposit`，最後保存並提交。
- [TransferCore](../LifeDashboard.Application/Services/TransferCore.cs) 驗證來源和目的帳戶均屬於使用者，扣除來源帳戶餘額、增加目的帳戶餘額並建立 Transfer Transaction。Goal deposit 呼叫交易服務時傳入 Goal，交易會設定 `GoalId`；一般交易或一般轉帳未傳入 Goal，不會更新任何 Goal。
- [Goal.ApplyDeposit](../LifeDashboard.Domain/Entities/Goal.cs) 累加實際存入金額。若 Goal 類型是 `Saving` 且累計金額大於或等於目標金額，Domain entity 會將狀態設為 `Completed` 並設定 `CompletedDate`。`Purchase` Goal 不會因 deposit 達標而完成；購買由 `GoalPurchaseService` 建立關聯的 Expense Transaction，再呼叫 `Goal.CompletePurchase` 完成。
- [Transaction entity](../LifeDashboard.Domain/Entities/Transaction.cs) 有 nullable `GoalId`，因此 Goal 可關聯多筆 Transaction，而每筆 Transaction 最多關聯一個 Goal。Goal 的進度異動流程會建立此關聯；一般資金移動不會自動關聯。刪除 Goal 關聯交易會被 `TransactionService` 拒絕。
- 帳戶應用服務 [AccountService](../LifeDashboard.Application/Services/AccountService.cs) 負責帳戶讀取與 CRUD；交易造成的餘額異動由 TransactionService/TransferCore 負責。Goal 進度不是從 AccountService 計算。
- Dashboard 是唯讀組合流程：[GoalRepository](../LifeDashboard.Infrastructure/Repositories/GoalRepository.cs) 依 `UserId` 查詢 Goal → [GoalService](../LifeDashboard.Application/Services/GoalService.cs) 映射為 Goal DTO → [DashboardService](../LifeDashboard.Application/Services/DashboardService.cs) 組合 accounts、budget、goals → [DashboardController](../LifeDashboard.Api/Controllers/DashboardController.cs) 回傳 `DashboardSummaryDto` → DashboardPage 將 `summary.goals` 傳給 GoalPyramid。這條路徑不負責改變 Goal 狀態或金額。

# Problems Found

## A. Saving Goal 存入 30,000

若使用者先建立 `TargetAmount = 100000` 的 Saving Goal，再呼叫該 Goal 的 deposit endpoint 存入 30,000，系統會建立關聯的 Transfer Transaction、更新來源與目的 Account，並將 `CurrentAmount` 設為 30,000。此流程已由 `GoalDepositService` 協調，不需要新增 Domain Event。

若使用者只透過一般 Transfer API 在兩個帳戶間移動 30,000，系統沒有足夠資訊判斷這筆轉帳屬於哪一個 Goal，因此不會更新 Goal。這是目前交易 API 的明確語義，不是 GoalDepositService 漏更新。若未來要求一般交易也能推進 Goal，必須讓操作明確帶有 `GoalId`，並由應用服務在同一交易中載入、更新 Goal；不應僅依帳戶或交易金額推測。

## B. Deposit 超過 TargetAmount

目前會保留實際累計金額並完成 Saving Goal。例如 target 為 100,000、存入 120,000 時，`CurrentAmount` 會成為 120,000，`Status` 會成為 `Completed`，並設定完成日期。Domain 使用 `CurrentAmount >= TargetAmount` 判斷，不會把金額截斷為 target。

既有 [GoalDepositApiTests](../LifeDashboard.Tests/Goal/GoalDepositApiTests.cs) 已以 target 50、deposit 75 驗證超額時 Goal 完成、Transaction 關聯及兩個 Account 餘額更新；[GoalDepositServiceTests](../LifeDashboard.Tests/Goal/GoalDepositServiceTests.cs) 也驗證達標後的狀態。故此情境目前沒有缺少的 Domain 行為。

## C. Dashboard Summary Goals

`GoalRepository → GoalService → DashboardService → DashboardSummaryDto → DashboardPage → GoalPyramid` 符合 Domain Responsibility：Repository 查詢使用者範圍內的持久化資料，GoalService 封裝 Goal 查詢與 DTO mapping，DashboardService 組合不同讀取服務，Controller/API 負責輸出，前端只呈現。summary 回傳目前狀態是讀取既有資料，不應在這條鏈上修正或推導進度。

目前的測試資料問題在資料建立方式：直接 SQL 修改 `CurrentAmount`/`Status` 可以呈現 UI，但略過了 deposit 對 Transaction 和 Account 的一致性驗證。這不表示 Dashboard 的資料鏈或 Goal Domain deposit 流程本身缺失。

# Recommended Minimal Fix

- 不新增 Domain Event，也不改 Goal、Dashboard API 或前端。對現有設計而言，`GoalDepositService` 已是存入用例的 application-service orchestration 邊界，`Goal.ApplyDeposit` 已負責累加金額及判斷 Saving Goal 是否完成。
- 建立真實流程測試資料時，先使用既有 Goal create API 建立 Goal，再對每筆 Goal 呼叫其 deposit endpoint。測試應使用隔離的測試資料庫與來源/目的帳戶，並由一般轉帳規則驗證來源資金；不要直接改 Goal 欄位，也不要對共用的 test001 帳戶執行未預期的資金移動。
- 若要補足可重複的自動化驗證，可在既有 Goal deposit API integration test 中加入「建立 Goal → 一次或多次 deposit → 驗證 Goal、關聯 Transaction、兩個 Account」案例。現有 API 測試已涵蓋超額 deposit；不需新增測試 framework。
- 如果產品政策要求 deposit 不可超額，需另行決定拒絕超額或限制存入額度，並讓交易金額與 Goal 金額採同一政策。現況是允許超額並記錄實際金額，不建議只將 Goal 金額截斷而仍轉移較大金額。

# Files To Change

- 目前 Goal 進度與 Dashboard 投影不需要 production code 修改；本次直接 SQL 測試資料應改由既有 API deposit 流程建立。
- 若要把上述完整流程固化為自動化回歸測試，建議只擴充既有 [GoalDepositApiTests.cs](../LifeDashboard.Tests/Goal/GoalDepositApiTests.cs)。該測試目前直接 seed User、Goal、Account，再呼叫 deposit；可改為或另加透過 API 建立 Goal 的案例，然後驗證資料庫中的 Goal、Transaction、Account。
- 現有 [DashboardControllerApiTests.cs](../LifeDashboard.Tests/Dashboard/DashboardControllerApiTests.cs) 使用 mock 的 `IDashboardService`，驗證 summary response 組合，不驗證 Goal deposit 寫入流程。若要測試寫入後的 summary 投影，可在上述整合測試加入 summary 呼叫，或另行規劃整合測試；不必修改 Dashboard production code。

# Risk Analysis

- Deposit 會真實扣除來源 Account、增加目的 Account，並新增 Transfer Transaction。來源餘額不足時會失敗並回滾；`AllowNegativeBalance` 設定也會影響扣款結果。因此測試必須使用隔離帳戶/資料庫，避免碰觸共用帳戶。
- Goal、Transaction、Account 的更新由同一 unit of work/資料庫交易提交或回滾。若將 `ApplyDeposit` 移出此協調流程、改成事後非同步事件，可能出現轉帳已完成但 Goal 進度未更新，或反向不一致；目前同步 orchestration 能避免這種落差。
- 一般 Transaction/Transfer 沒有 GoalId 時不更新 Goal 是必要的明確性；將所有帳戶移轉自動視為 Goal deposit 會導致錯誤歸屬。
- Goal 的刪除與目標編輯仍有額外一致性風險：GoalService 允許刪除未完成 Goal，而資料庫對 Transaction.GoalId 使用 `ON DELETE SET NULL`，可能使既有交易失去 Goal 關聯；`UpdateDetails` 也允許改變 target/type，但不會依新的值重算狀態。這些不是本次 deposit/summary 修正所必需的改動，應另立需求決定刪除政策及更新後狀態規則。
- `Goal.UpdateDetails` 沒有驗證 `TargetAmount` 必須大於 0。若要補此 Domain invariant，需另行處理既有資料及 API 驗證；它不改變目前 `TargetAmount = 100000` 的 deposit 行為。