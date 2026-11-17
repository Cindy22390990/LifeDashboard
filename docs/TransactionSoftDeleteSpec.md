Phase 3B Transaction Soft Delete

Goal

將 Transaction Delete 從 Physical Delete 改為 Soft Delete，以保留財務系統的完整稽核軌跡 (Audit Trail)，防止金流紀錄不可逆地遺失。

Domain Changes

Transaction Entity 新增：

IsDeleted (bool): 標記該筆交易是否已被刪除。預設為 false。

DeletedAt (DateTime?): 記錄執行刪除操作的 UTC 時間。

DeletedReason (string?): 記錄刪除原因。

說明：目前不要求前端輸入。Service 自動填入固定原因，例如: "User deleted transaction"。保留欄位以支援未來 audit/debug。

Delete Transaction Flow

在 TransactionService 中，DeleteTransactionAsync 必須嚴格遵循以下原子性 (Atomic) 流程：

Validate transaction state
確認：

Transaction 存在

User ownership 正確

IsDeleted == false
若已刪除，不允許重複 Delete，回傳 NotFound 或 Conflict。

Load related Account: 載入與該交易相關的 Account (若是轉帳，需同時載入 SourceAccount 與 TargetAccount)。

Calculate balance reversal: 根據交易類型 (TransactionType) 計算需要反向補償的金額。

Update Account balance: 將反向補償的金額應用到對應的帳戶餘額上。

Mark Transaction deleted: 將實體的 IsDeleted 設為 true，DeletedAt 設為當下 UTC 時間，並填入 DeletedReason。

Commit UnitOfWork: 呼叫 _unitOfWork.CommitAsync()，確保帳戶餘額更新與交易標記刪除在同一個 Database Transaction 中完成。

Balance Rules

定義刪除不同類型的交易時，帳戶餘額的回補規則：

Expense delete:

Balance + amount

Income delete:

Balance - amount

Transfer delete:

Source restore (轉出戶恢復)：SourceAccount.Balance += Amount

Destination reverse (轉入戶扣回)：TargetAccount.Balance -= Amount

驗證限制：刪除前需驗證 Destination Account reversal 後不違反帳戶的餘額規則。若會造成非法餘額：

不執行刪除

回傳 Conflict

保持原交易狀態

Repository Changes

說明：

Delete method 如何改變: ITransactionRepository.Delete 的實作不再呼叫 _context.Transactions.Remove(entity)，應改為更新狀態 (如 _context.Transactions.Update(entity))。

QueryFilter 如何加入: 在 DbContext 設定中加入全域過濾器：modelBuilder.Entity<Transaction>().HasQueryFilter(t => !t.IsDeleted);

IgnoreQueryFilters 使用情境: 僅用於判斷是否已被刪除的後台稽核或 Delete workflow exception 中。

Migration Plan

Rollback 不負責恢復財務資料。

Database migration: 執行 EF Migration 將欄位加入資料庫。

Existing records default value: 確保舊紀錄的 IsDeleted 預設為 false，DeletedAt 為 NULL。

Rollback strategy:

Schema rollback: 使用 EF migration rollback。

Data recovery: 使用 Domain compensation script 或 Manual correction procedure。不可單靠 migration rollback 修復財務資料。

Testing Plan

重新設計 E-03 測試案例以驗證軟刪除邏輯：

Before:

Account.Balance = 9000

Transaction count = 1 (一筆金額為 1000 的 Expense)

Execute:
DELETE transaction API

Expected:

Database raw verification:

執行：SELECT IsDeleted, DeletedAt FROM Transactions WHERE Id = targetTransactionId

預期結果：IsDeleted = true 且 DeletedAt IS NOT NULL

Account:

Balance = 10000

API:

GET Transaction 不返回 deleted transaction

Query Behavior After Soft Delete

Default query behavior:

All normal user-facing queries must respect:
HasQueryFilter(t => !t.IsDeleted)

Affected:

Transaction list

Dashboard calculation

Budget Radar calculation

Monthly reports

Delete workflow exception:

DeleteTransactionAsync may use IgnoreQueryFilters()
only to determine whether the target transaction was already deleted.

No user-facing API may expose deleted transactions.