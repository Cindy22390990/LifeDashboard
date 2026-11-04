# Account Negative Balance Policy
## 正式實作規格書 v1.0

**專案：** LifeDashboard  
**模組：** Account / Transaction / Goal  
**階段：** Account Negative Balance Policy  
**狀態：** Business Rules Approved，待實作  
**目標：** 建立每個帳戶獨立的負餘額管理政策。

---

# 一、功能目標

讓使用者可以針對每個 Account 獨立設定是否允許餘額低於 0。

設定不屬於全域 UserSettings，而是 Account 本身的屬性。

例如：

| Account | AllowNegativeBalance |
|---|---|
| 銀行帳戶 A | false |
| 現金帳戶 B | true |
| 儲蓄帳戶 C | false |

使用者可以同時擁有允許負數及不允許負數的帳戶。

所有扣款行為均依照實際被扣款帳戶的設定決定。

# 二、已確認的業務規則

## 2.1 Account 欄位

Account Entity 新增：

```csharp
public bool AllowNegativeBalance { get; set; } = false;
```

規則：

- false：不允許帳戶餘額低於 0。
- true：允許帳戶餘額低於 0。
- 新建立帳戶預設 false。
- 既有帳戶 Migration 後預設 false。
- 每個 Account 獨立儲存設定。
- 不使用 UserSettings 儲存此設定。

## 2.2 扣款判斷

統一業務規則：

```csharp
if (!account.AllowNegativeBalance &&
    account.CurrentBalance < amount)
{
    throw new InsufficientFundsException();
}

account.CurrentBalance -= amount;
```

此程式僅為規則示意，實際實作應使用共用扣款核心，避免各 Service 重複實作。

## 2.3 交易操作適用範圍

| 操作 | 檢查帳戶設定 | 說明 |
|---|---|---|
| 一般 Expense | 是 | 檢查付款帳戶 |
| Goal Purchase | 是 | 檢查付款帳戶 |
| 一般 Transfer 轉出 | 是 | 檢查來源帳戶 |
| Goal Deposit | 是 | 檢查來源帳戶 |
| Income 建立 | 否 | 增加帳戶餘額 |
| Transfer 轉入 | 否 | 增加帳戶餘額 |
| Expense 退款／刪除 | 否 | 恢復帳戶餘額 |
| Income 刪除 | 是 | 刪除收入會扣回帳戶餘額 |
| 一般 Transfer 刪除 | 是 | 檢查被扣回款項的轉入帳戶 |

補充：

Goal Deposit 與 Goal Purchase 的既有 Goal 關聯交易刪除保護必須保留。

## 2.4 各操作詳細規則

### A. 一般 Expense

付款帳戶：

- AllowNegativeBalance = true：允許扣款至負數。
- AllowNegativeBalance = false：餘額不足時拒絕。

### B. Goal Purchase

付款帳戶遵守相同規則。

不得因為 Purchase 是 Goal 功能而繞過 Account 的負餘額政策。

其他既有規則維持：

- Goal 必須屬於目前使用者。
- GoalType 必須為 Purchase。
- Goal 必須 Active。
- CurrentAmount 必須達到 TargetAmount。
- Category 必須屬於目前使用者。
- 不允許 Living Category。
- Purchase 成功後 Goal 完成。
- CurrentAmount 保留。

### C. Transfer

來源帳戶必須依 AllowNegativeBalance 判斷。

轉入帳戶不檢查負餘額設定，因為轉入是增加餘額。

### D. Goal Deposit

沿用 TransferCore 的扣款規則。

來源帳戶依 AllowNegativeBalance 判斷。

不得另外建立一套不同的餘額檢查。

### E. Income

新增收入不受 AllowNegativeBalance 限制。

收入增加 Account.CurrentBalance。

### F. Expense 退款／刪除

一般 Expense 刪除後，恢復原付款帳戶餘額。

退款不得因為 AllowNegativeBalance = false 而遭到阻擋。

### G. Income 刪除

刪除 Income 時，系統需要扣回原本增加的金額。

因此：

- AllowNegativeBalance = true：允許刪除，即使扣回後餘額為負數。
- AllowNegativeBalance = false：若扣回後會低於 0，拒絕刪除。
- 拒絕刪除時不得刪除 Transaction。
- 不得部分修改 Account 餘額。

### H. 一般 Transfer 刪除

既有 Transfer 刪除會：

1. 恢復來源帳戶餘額。
2. 扣回轉入帳戶餘額。
3. 移除 Transfer Transaction。

因此應檢查轉入帳戶的 AllowNegativeBalance。

若不允許負數且扣回後會低於 0：

- 拒絕刪除。
- 不修改任一帳戶餘額。
- 不刪除 Transaction。

Goal 關聯 Transfer 維持既有禁止一般刪除政策。

---

# 三、帳戶設定切換規則

使用者可以隨時開啟或關閉 AllowNegativeBalance。

不限制目前帳戶餘額。

例如：

| 原設定 | 原餘額 | 新設定 | 結果 |
|---|---:|---|---|
| true | -500 | false | 保留 -500，禁止新的不足額扣款 |
| false | 500 | true | 立即允許透支 |
| true | 500 | false | 立即禁止透支 |
| false | -500 | true | 保留 -500，允許後續透支 |

重要：

1. 關閉允許負數時，不得自動將餘額調整成 0。
2. 不得自動修改 InitialBalance。
3. 不得新增 Adjustment Transaction。
4. 設定變更只影響後續交易。
5. 已存在的負餘額保留原值。
6. 使用者關閉設定後，若餘額仍為負數，新的扣款一律拒絕，直到餘額足以支付該筆扣款。

---

# 四、Backend 架構規格

## 4.1 Account Entity

修改：

`LifeDashboard.Domain/Entities/Account.cs`

新增：

`AllowNegativeBalance`

預設 false。

## 4.2 DTO

修改：

`LifeDashboard.Application/Models/DTOs/AccountDtos.cs`

需要確認：

- AccountDto 回傳 AllowNegativeBalance。
- CreateAccountDto 支援新增設定。
- UpdateAccountDto 支援更新設定。

前端不能只修改 UI 而沒有將欄位傳入 API。

後端必須以資料庫中的 Account 設定作為實際判斷依據。

## 4.3 Account Service

修改：

`AccountService.cs`

負責：

- 建立帳戶時保存設定。
- 更新帳戶時保存設定。
- 回傳 AccountDto 時帶出設定。

## 4.4 共用扣款核心

評估並新增：

- IAccountDebitService
- AccountDebitService

建議放在 Application 層。

責任：

1. 驗證扣款金額。
2. 檢查 AllowNegativeBalance。
3. 檢查帳戶餘額。
4. 修改 Account.CurrentBalance。

不負責：

- 查詢帳戶。
- 建立 Transaction。
- SaveChanges。
- BeginTransaction。
- Commit。
- Rollback。

呼叫端必須傳入已經透過 Repository 查詢、且確認屬於目前使用者的 Account。

不得自行建立另一個 DbContext 或獨立 UnitOfWork。

## 4.5 必須整合的 Service

### TransactionService

- 一般 Expense 改用共用扣款核心。
- Transfer 透過 TransferCore 使用共用扣款核心。
- Income 不使用扣款核心。
- Expense 退款不使用扣款核心。
- Income 刪除時使用扣款核心。
- 一般 Transfer 刪除時，對轉入帳戶使用扣款核心。

### TransferCore

- 來源帳戶依 AllowNegativeBalance 判斷。
- 轉入帳戶不檢查。
- Goal Deposit 自動沿用此規則。

### GoalPurchaseService

- 付款帳戶依 AllowNegativeBalance 判斷。
- 不自行重複實作餘額判斷。
- 保留既有 Goal 驗證及交易原子性。

---

# 五、Transaction Atomicity

本次重構不得破壞既有 UnitOfWork 架構。

規則：

- TransactionService 繼續管理自己的交易邊界。
- GoalPurchaseService 繼續管理自己的交易邊界。
- GoalDepositService 繼續管理自己的交易邊界。
- TransferCore 不新增獨立交易。
- AccountDebitService 不新增獨立交易。

所有帳戶餘額、Goal 狀態、Transaction 寫入必須在原本呼叫端的同一資料庫交易中完成。

任一操作失敗時，必須 Rollback。

---

# 六、資料庫 Migration

修改：

`LifeDashboardDbContext.cs`

設定 AllowNegativeBalance 為非 Nullable Boolean，資料庫預設 false。

新增 Migration：

建議名稱：

`AddAccountAllowNegativeBalance`

預期 SQL 欄位：

- Column：AllowNegativeBalance
- Type：bit
- Nullable：false
- Default：false

Migration 必須：

1. 對既有 Account 回填 false。
2. 不修改 CurrentBalance。
3. 不修改 InitialBalance。
4. 不刪除任何 Transaction。
5. 不修改 Goal。
6. 更新 Model Snapshot。

注意：

既有帳戶可能已有負餘額，Migration 不得自動修正。

Migration 產生後先檢查 Up / Down，不得自行執行 Database Update，等待確認。

---

# 七、Frontend 規格

## 7.1 Account 新增表單

修改：

- `src/domain/Account.ts`
- `src/infrastructure/api/AccountRepository.ts`
- `src/application/stores/accountStore.ts`
- `src/presentation/pages/AccountPage.tsx`

新增勾選項：

「允許帳戶餘額為負數」

預設不勾選。

## 7.2 Account 編輯功能

目前前端尚未找到完整的 Account 編輯流程。

需要補上：

- 編輯帳戶入口。
- 編輯表單或 Modal。
- 讀取既有 AllowNegativeBalance。
- 修改設定後呼叫既有 PUT API。
- 更新 Store。
- 更新畫面。

不得只修改新增表單而忽略既有帳戶的設定管理。

## 7.3 使用者體驗

當使用者關閉允許負數：

若目前餘額為負數，可以顯示提醒：

「目前帳戶餘額為負數，關閉此設定後，新的扣款將受到餘額限制。」

不要求自動調整餘額。

---

# 八、併發控制

目前尚未確認現有架構具有完整的 Account 併發保護。

本次實作必須評估：

- 同一 Account 的並行扣款。
- 同一 Account 同時執行 Expense 與 Transfer。
- 同一 Account 同時執行 Goal Purchase 與一般 Expense。
- 同一 Account 同時更新 AllowNegativeBalance 與執行扣款。

不能只依靠記憶體中的 CurrentBalance 比較，便宣稱已解決併發問題。

需提出適合 SQL Server 的方案，例如：

- RowVersion 樂觀併發控制。
- 原子條件式 SQL Update。
- 適當的資料庫鎖定與交易隔離。

必須確保：

1. 不允許負數的帳戶不會因並行扣款而變成負數。
2. 不會發生 Lost Update。
3. 發生併發衝突時不留下部分更新。
4. API 回傳一致且可理解的錯誤結果。

不得只依靠 SQLite 測試宣稱 SQL Server 併發安全。

---

# 九、測試驗收標準

## Account

- 新增帳戶預設 false。
- 新增帳戶可以設定 true。
- 編輯帳戶可以切換設定。
- API 正確回傳設定。
- 不同帳戶設定互不影響。

## Expense

- false + 餘額足夠：成功。
- false + 餘額不足：拒絕。
- true + 餘額不足：成功並允許負數。

## Goal Purchase

- false + 餘額不足：拒絕。
- true + 餘額不足：成功。
- 其他 Goal 驗證維持原規則。

## Transfer

- 來源帳戶 false + 餘額不足：拒絕。
- 來源帳戶 true + 餘額不足：成功。
- 轉入帳戶 false：不影響收款。

## Goal Deposit

- 來源帳戶 false + 餘額不足：拒絕。
- 來源帳戶 true + 餘額不足：成功。
- Goal 與 Transaction 必須保持一致。

## Income

- 新增 Income 不受設定影響。
- 刪除 Income 必須遵守原帳戶設定。
- 刪除失敗時 Transaction 必須保留。

## Delete Regression

- 一般 Expense 退款不受設定影響。
- 一般 Transfer 刪除須檢查轉入帳戶。
- Goal Purchase 刪除維持禁止。
- Goal Deposit 刪除維持禁止。

## Account Setting Toggle

- 負餘額時可以關閉設定。
- 關閉後保留負餘額。
- 關閉後新的不足額扣款遭拒絕。
- 重新開啟後允許負數。

## Atomicity

所有失敗情境檢查：

- Account.CurrentBalance
- Goal.CurrentAmount
- Goal.Status
- Goal.CompletedDate
- Transaction 是否存在

不得留下部分更新。

## Migration

- 舊 Account 預設 false。
- 不修改舊帳戶餘額。
- Up / Down Migration 正確。
- Model Snapshot 正確。

---

# 十、實作順序

建議依序進行：

Phase 1：Account Entity、DTO、Service、Migration。

Phase 2：AccountDebitService 共用扣款核心。

Phase 3：TransactionService 一般 Expense、Income Delete、Transfer Delete。

Phase 4：TransferCore、GoalPurchaseService 整合。

Phase 5：Account Frontend 新增與編輯功能。

Phase 6：完整單元測試與整合測試。

Phase 7：SQL Server 併發安全驗證。

Phase 8：Migration 檢查、資料庫更新及完整回歸測試。

---

# 十一、Copilot 執行限制

- 嚴格依照本規格執行。
- 不擴大到 Dashboard、Goal UI 或其他模組。
- 不重構無關的 Service。
- 不改變 TransactionType。
- 不改變 Goal 業務規則。
- 不新增 Refresh Token、OAuth 或其他無關功能。
- 不刪除既有測試。
- 不以修改測試預期結果的方式掩蓋 Production Code 錯誤。
- 每個 Phase 完成後執行對應測試。
- 發現規格衝突或既有資料風險時，停止並回報。
- Migration 產生後先停止，不自行執行 Database Update。
- 不得宣稱完成未實際測試的功能。

**最終目標：每個 Account 獨立管理負餘額政策，所有扣款入口遵守同一規則，同時維持既有財務交易原子性及多使用者資料隔離。**