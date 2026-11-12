# Coverage Verified

- `CreateGoalDepositThenDashboardShowsGoalProgress` 透過 `POST /api/goals` 建立 Emergency Fund，沒有直接建立 Goal entity，也沒有直接更新 Goals table。
- 兩次存入都呼叫 `POST /api/goals/{id}/deposit`：第一次 30,000 後檢查 API/持久化 Goal 金額為 30,000 且狀態為 `Active`；第二次 70,000 後檢查金額為 100,000、狀態為 `Completed`，且 API 與持久化資料的 `CompletedDate` 非 null。因測試只經過 deposit API 變更 Goal，這驗證了 Domain deposit 流程的外部結果。
- Transaction 確實由資料庫讀回。第一次存入檢查到關聯 Goal 的 Transaction，並驗證其 `GoalId` 指向新 Goal、`Type` 為 `Transfer`；第二次後確認總共有兩筆關聯該 Goal 的 Transaction。
- 兩個 Account 的餘額均有在第一次及第二次存入後驗證：Source 為 170,000 再到 100,000；Goal Account 為 30,000 再到 100,000。
- 兩個階段都透過 `GET /api/dashboard/summary?currentDate=...` 驗證回應中的 Goal 金額及狀態。測試使用真實 API host 和預設 DI；[CustomWebApplicationFactory](../LifeDashboard.Tests/Helpers/CustomWebApplicationFactory.cs) 只將 DbContext 換成 SQLite，沒有替換 Dashboard、Goal 或 Repository service。因此 summary 讀取實際資料，經由 GoalRepository → GoalService → DashboardService，而不是 mock 或由測試直接組出 response。
- 測試隔離性良好：每個案例建立自己的 `CustomWebApplicationFactory`；factory 建立並持有新的 SQLite `:memory:` connection，`EnsureCreated` 初始化新資料庫。User 由 Entity 建構子產生新的 Guid，測試沒有固定 UserId；帳戶與 Goal 都只存在該測試的資料庫中。
- [DashboardControllerApiTests](../LifeDashboard.Tests/Dashboard/DashboardControllerApiTests.cs) 中確有 mock `IDashboardService` 的案例，但那是另一個 test class；本案例沒有套用其 mock factory。

# Potential Weakness

1. Transaction 欄位未逐筆完整驗證。第一次只斷言 `GoalId` 與 `Type`，沒有斷言 Transaction 的 `Amount`、`AccountId`、`ToAccountId`；第二次只斷言兩筆數量及兩筆 `GoalId`。因此若 Transaction metadata 錯誤，但應用服務仍正確調整 Goal 和帳戶餘額，測試仍可能通過。
2. Pyramid 分層欄位未驗證。summary assertions 檢查名稱、CurrentAmount、TargetAmount 和 Status，沒有檢查 `Tier == Tier1_Safety`。若 Goal DTO mapping 或 summary 對 Tier 的輸出回到預設值，測試仍會通過，但 GoalPyramid 可能將它放入錯誤層級。
3. 第一次 deposit 未確認 `CompletedDate` 為 null。案例檢查 `Active`，但未檢查 API response 和持久化 Goal 的完成日期尚未設定；若重構在尚未達標時錯誤地設定日期、同時保留 Active 狀態，第一階段仍可能通過。第二階段目前只檢查完成日期非 null，未檢查時間值符合完成時點。

# Recommended Improvements

- 對兩筆關聯 Transaction 逐筆斷言：Amount 分別為 30,000 和 70,000、Type 均為 Transfer、來源與目的 Account IDs 正確，並保留兩筆 `GoalId` 關聯及筆數斷言。這能讓未來重構 GoalDepositService 或 TransactionService 時，直接捕捉交易內容與餘額/Goal 更新不一致。
- 在建立後與兩次 summary assertions 加入 Tier 斷言，至少確認 `Tier1_Safety`；這直接保護 GoalPyramid 所依賴的分類契約。
- 第一次 deposit 後斷言 API response 與 persisted Goal 的 `CompletedDate` 都是 null；第二次後則保留非 null 斷言。這會鎖定狀態轉換僅於達標 deposit 發生。
- 此案例只驗證成功路徑。未來若修改 deposit 的交易邊界或 rollback 行為，應保留並持續執行同檔既有 `Deposit_rejects_insufficient_source_balance_without_partial_changes` 測試；本案例本身不驗證失敗後的 atomic rollback 或 concurrency 行為。