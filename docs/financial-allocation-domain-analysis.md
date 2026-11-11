# Financial Allocation Domain Analysis

Date: 2026-11-10
Status: Domain design analysis only. No application behavior, API, UI, or database schema was changed.

## 1. 現況模型分析

### Account

`Account` 是使用者擁有的資金帳戶，包含 `InitialBalance`、`CurrentBalance`、`Type`、`AllowNegativeBalance`、系統帳戶與軟刪除資訊。一般交易由 Application service 依交易類型調整 `CurrentBalance`；Transfer 透過 `TransferCore` 扣來源、加目的帳戶。`AccountDebitService` 只負責扣款與不足餘額檢查。

帳戶目前代表帳面資金位置，不是資金用途配置。沒有 earmarked/reserved amount、不可花用額或被哪些目標佔用的欄位。`AccountType.Saving` / `Investment` 的註解稱其資金隔離，但交易扣款流程不依 AccountType 限制；一般 Expense 可從任何使用者帳戶扣款。故帳戶類型目前是分類資訊，不是防止誤花的強制隔離機制。

另外，Account constructor 設定 `InitialBalance`，但目前沒有同步設定 `CurrentBalance`；這是既有帳戶語意問題，Financial Allocation 不應默默把它當成可用現金或資金分配來源。

### Transaction

`Transaction` 是實際資金流紀錄，核心資料有 `UserId`、`AccountId`、`ToAccountId`、`Amount`、`Type`、`TransactionDate`、Category 與可選 `GoalId`。

- Income 增加指定 Account 的 `CurrentBalance`。
- Expense 透過 debit service 扣除指定 Account。
- Transfer 扣來源並增加目的 Account。
- 新增交易及餘額變更由 TransactionService/TransferCore 在 UnitOfWork transaction 中保存。
- 一般 Transaction API 目前提供查詢、新增與刪除，沒有更新 endpoint。刪除一般交易會反向調整餘額；Goal-linked 交易不可直接刪除。

Transaction 可以作為「收入或資金移動已發生」的來源，但目前無法表達一筆收入分成多種用途、某部分已確認配置、剩餘未配置金額或配置規則版本。Category 是交易分類，不是對收入來源的用途承諾。

### Goal

`Goal` 是使用者的目標進度模型，欄位包含 `TargetAmount`、`CurrentAmount`、Tier、GoalType、Status、Note 與完成日期。

- Saving Deposit 以 Transfer 實際搬移帳戶資金，Transaction 關聯 Goal，並在同一 transaction 更新 `Goal.CurrentAmount`。
- Saving Goal 達標時可完成。
- Purchase Goal 以 Goal-linked Expense 記錄實際支出並完成目標；目前需指定付款帳戶與非 Living Category。
- Goal 的進度/狀態由後端維護；前端顯示而不應自行推導業務狀態。

因此 Goal 不只是單純 `TargetAmount` 欄位，也有累積進度與完成生命週期；但它仍主要是「目標進度追蹤」，不是資金保留或資產鎖定模型。`CurrentAmount` 不等同某帳戶的可用餘額，亦不保證對應的資金仍留在可辨識的位置。Deposit 選擇一般來源與目的 Account，Domain 沒有 Goal 專屬資金池或禁止其他交易花用該目的帳戶的規則。

Goal-linked Transaction 讓部分 Goal 活動可追溯，但不能涵蓋所有歷史來源、分配規則、未配置收入或更正/撤銷語意；也不應只靠 Goal 當前的 `CurrentAmount` 反推全部資金來源。

### AllocationRatio

`AllocationRatio` 是 `SystemSetting` 內的 Value Object，包含 Living、Savings、Investment、Entertainment 四個百分比，驗證總和為 100 且不得為負。`SettingService` 讀取及更新這些數值；目前資料模型只有一份當前值，沒有生效日期、版本、核准紀錄或套用對象。

前端設定頁可編輯比例，但 TransactionService、GoalService 與 BudgetRadarService 不會依比例拆分收入或建立分配紀錄。現階段比例是有效的使用者設定資料，並非執行中的 allocation rule engine。

### Budget Radar

Budget Radar 依 `SystemSetting.Payday` 建立含首尾日的薪資週期，並使用：

```text
TotalLivingBudget = payroll-period Income - active FixedCommitment total
RemainingLivingBudget = TotalLivingBudget - payroll-period Living Expenses
DailySafeLimit = RemainingLivingBudget / inclusive remaining days
TodayRemaining = DailySafeLimit - today's Living Expenses
```

Income/Expense 由 Transaction 查詢；Living Expense 由 Category.BucketType 判斷；Transfer 不計入收入或支出；未分類 Expense 另行回報而不扣 Living budget。FixedCommitment 是依週期有效的計畫預留，不自動產生 Transaction。AllocationRatio 不參與現有計算。

Budget Radar 呈現的是週期預算額度，不是實際 Account 現金，也不是已配置至 Goals 的金額。Responsibility Expense 需使用 Responsibility bucket，否則某筆固定責任支出若誤分類為 Living，可能與預留金額重複扣算。

### 現有主要資料流

```text
SystemSetting.Payday + AllocationRatio
  └─ Payday 用於週期；AllocationRatio 僅讀寫，未進入計算

Income Transaction -> Account.CurrentBalance 增加
  └─ Budget Radar 取週期收入

Expense Transaction -> Account.CurrentBalance 扣除
  └─ Living Category 才扣 Budget Radar 的 Living budget

Transfer Transaction -> 來源 Account 扣除、目的 Account 增加
  └─ Goal Deposit 可關聯 Goal，並增加 Goal.CurrentAmount

FixedCommitment -> Budget Radar 依 payroll month 預留 MonthlyAmount
  └─ 不自動建立或關聯付款 Transaction
```

## 2. 發現問題

### Goal 問題

Goal 有進度與完成狀態，能追蹤目標金額累積及完成與否；它不具備資金用途控制或帳戶資金隔離能力。

目前缺少：

- **保留金額：** 沒有承諾／鎖定額或尚可配置額，無法避免同一筆資金同時被視為 Goal 資金與可自由花費資金。
- **完整資金來源：** `GoalId` 可標記 Goal Deposit/Purchase 交易，但沒有通用配置記錄指出某筆收入的金額分配到哪些用途。
- **分配紀錄：** 沒有 planned/suggested/confirmed/reversed 等分配狀態，也沒有規則版本或使用者確認時間。
- **目標資金隔離：** Deposit 的目的 Account 可被一般交易使用；沒有 Goal-to-Account 約束、預留額或扣款限制。
- **進度對帳：** `CurrentAmount` 是 Goal 上的累積欄位，並非由完整 allocation ledger 即時推導；若相關資金透過未關聯的 Expense/Transfer 移出，進度不會自動下降。

因此目前 Goal 可回答「累積進度是多少、是否已完成」，不能可靠回答「這筆餘額仍被保留給這個目標嗎」或「有多少收入尚未分配」。

### Budget Radar 問題

- **目前比例修改不會重新計算 Radar。** BudgetRadarService 不讀 `AllocationRatio`，因此現在修改比例不會改變歷史或目前 Radar 結果。
- **未來若用當前比例回算歷史收入，會造成歷史重算。** SystemSetting 只保存目前比例，沒有 rule version 或有效期間，無法重現某歷史期間當時採用的比例。
- **可用額度容易被誤讀。** Radar 的 `TotalLivingBudget` 是週期收入減固定責任，`RemainingLivingBudget` 再扣 Living Expense；它不是現金餘額，也不是尚未配置收入。若比例計畫、實際配置、現金餘額共用「可用」或「剩餘」字樣，使用者可能誤以為可花用資金增加。
- **承諾資金與自由資金語意不同。** FixedCommitment 在週期開始時計入預留，不表示帳戶已付款或現金已隔離；未建置實際支付關聯。若未清楚區分 Planned 與 Actual，容易把計畫、付款和剩餘現金混為一談。
- **Category 規則不是承諾對帳。** Responsibility bucket 是避免固定責任付款再扣 Living budget 的分類慣例，不代表該 Expense 已連到某筆 FixedCommitment。

## 3. 建議 Domain 模型（只設計，不實作）

建議把「分配規則」、「已確認資金用途」和「Goal 累積貢獻」分成不同語意。不能把 AllocationRatio、Account balance、Goal.CurrentAmount 當成同一個狀態的三種表示。

### AllocationRule / AllocationRuleVersion

代表使用者對未來收入的配置計畫，例如 Living 60%、Goal 30%、Investment 10%。這是 suggestion 的輸入，不是已發生的資金流。

建議概念欄位：

- `Id`, `UserId`, `Version`
- `EffectiveFrom`，必要時有明確的 `EffectiveTo` 或 superseded reference
- 分配項目與比例
- 建立/確認時間與有效狀態

建議採不可變版本：修改比例建立新版本，不覆寫舊版。新增收入的 suggestion 記錄使用的 rule version；歷史 suggestion/confirmed allocation 不因目前比例改變而重算。`SystemSetting.AllocationRatio` 可在未來作為「目前預設規則」的來源或編輯介面，但不應同時成為已確認配置的歷史來源。

產品仍需決定規則按「收入 Transaction 日期」或「使用者確認分配日期」選取版本、跨月/追溯生效如何處理，以及比例套用到 gross income 還是扣除 FixedCommitment 後的基數。

### Allocation

代表對一筆實際來源 Transaction 金額所做、已確認的用途分配。它應是使用者確認後的 ledger record，不是每次讀取時重算出的比例結果。

可能欄位：

- `Id`
- `UserId`
- `SourceTransactionId`（來源收入 Transaction）
- `Amount`
- `AllocationType` 或目的分類
- 可選 `GoalId`（只在分配到 Goal 時使用）
- `AllocationRuleVersionId` 或 rule version snapshot reference
- `CreatedAtUtc`、`ConfirmedAtUtc`
- 如採 append-only 更正，包含 reversal/compensation reference

核心不變條件候選：來源必須為該使用者可配置的 Income Transaction；單一來源所有有效 Allocation 金額總和不得超過可配置來源金額；同一筆確認命令需具備冪等性；更正以反向/補償事件保留歷史，不直接改寫已確認金額。

需先決定 Allocation 表達的是「用途 earmark」還是「實際帳戶間移轉」。建議第一版將它定義為用途帳，不再次調整 `Account.CurrentBalance`；若使用者實際轉帳，仍由現有 Transfer Transaction 記錄。兩者不可因同一動作重複扣/加餘額。

### GoalContribution

代表一筆已確認的 Goal 專屬貢獻。它可作為 Allocation 的明確 subtype/target，也可作為 Allocation ledger 的 read-model 投影。

建議優先避免在 `Allocation.Amount` 和獨立 `GoalContribution.Amount` 同時保存同一金額，造成雙重 source of truth。若未來 Goal 需要獨立的撤回、到帳、凍結或稽核生命週期，再評估獨立 Entity；否則 `AllocationType = Goal` + `GoalId` 可作 canonical contribution ledger，`Goal.CurrentAmount` 維持為相容性 projection/cache，或逐步轉為由有效貢獻總和推導。

Contribution 本身只表示已配置給目標，不會自動隔離 Account 中的現金。若產品要求「避免誤花」，還須另外定義可花用餘額控制、專用帳戶策略或扣款授權規則；單一 GoalId 關聯不足以提供資金鎖定。

### AllocationSuggestion

Suggestion 是依某個 AllocationRuleVersion 對尚未配置收入產生的提案。它可以是暫時計算結果，不必立即持久化；只有使用者確認後才建立 Allocation。

若需要追蹤被接受/拒絕的建議，另定義 suggestion lifecycle；不可把尚未確認的 suggestion 當作 Actual、扣除 Available，或推進 Goal.CurrentAmount。

## 4. 核心流程設計

### 收入進帳

```text
Income Transaction persisted
  -> Application 查詢已配置金額及適用 AllocationRuleVersion
  -> 產生 AllocationSuggestion（不改 Account/Transaction/Goal）
  -> 使用者確認或調整項目
  -> 驗證來源歸屬、金額總和、目標使用者與重複確認
  -> 持久化 confirmed Allocation records
  -> 若有 Goal allocation，建立/更新 GoalContribution ledger projection
```

設計原則：

- Income Transaction 是可配置金額的來源，不代表配置已完成。
- suggestion 與 actual allocation 分開；只有 confirmed event 進入 Actual。
- Allocation 來源與 Goal 必須屬於同一 User。
- 建議以 transaction 金額減去已確認、未撤銷配置金額得到該收入來源的未配置餘額；不可將此數字直接稱為 Account 可用現金。
- 若產品決定配置時實際搬錢到另一帳戶，該資金移動另有 Transfer Transaction；Allocation ledger 不得再次變動帳戶餘額。

### 修改比例與歷史保存

例如 11 月 Living 60% / Savings 40%，12 月 Living 50% / Savings 50%：

1. 建立新的 AllocationRuleVersion，設定 12 月起適用；保留 11 月 rule version。
2. 11 月已確認 Allocation 保留其 source transaction、amount 與 rule version，不隨新比例變動。
3. 11 月尚未確認的 suggestion，產品需決定是保留舊版提案或依新版明確重產；不可靜默重寫 confirmed data。
4. 12 月符合新版本生效條件的收入套用新版；不得重新分配 11 月收入或改寫舊 Goal contribution。
5. 對補登/更正日期與跨週期收入，需以明確版本選擇規則處理，不用伺服器目前比例回算所有歷史。

### Account、Goal、Allocation 關係

建議保持 Account 與 Goal 不直接互相綁定。Allocation 對來源 Transaction 與可選 Goal 建立用途關聯即可；若要求物理隔離，需另行設計 reserve/withdrawal policy。多個 Goal 可由多筆 allocation 共享一個或多個帳戶，避免假設一個 Goal 專屬一個 Account。

## 5. Dashboard 顯示模型

Dashboard 未來應把以下三種資料明確分開，且值由後端 Application 提供，不由 React 推導：

### Planned

依指定 AllocationRuleVersion 與 eligible income 算出的計畫/建議值。需要帶上規則版本或適用期間；尚未確認，不可描述為已存入、已保留或實際配置。

### Actual

使用者已確認且未被 reversal/compensation 抵銷的 Allocation 合計。Goal 實際貢獻只計入已確認的 Goal-directed allocation/contribution，不以 ratio 推估。

### Available

此欄位必須先明確命名與定義。建議初期只顯示 `UnallocatedIncomeAmount`：符合規則的收入來源金額減去其有效 confirmed allocations。它不是銀行現金、Account.CurrentBalance、Budget Radar 的 `TodayRemaining`，也不保證可花/可存。

若產品要顯示實際可動用現金，需另行定義基於 Account balance、保留額、未清承諾、已配置金額與允許負餘額政策的模型；不可直接以 `Income - Allocation` 代替。

避免使用沒有限定詞的「你還可以存多少錢」。可能較準確的語意是「尚未配置的收入」；「計畫可配置」、「已確認配置」、「帳戶現金」分欄表達。

## 6. 風險分析

1. **Account 與 Goal 直接綁定：** 帳戶可能同時支援多個用途；專屬帳戶也可能被一般 Expense/Transfer 使用。若只把 Goal 綁定 Account 而沒有扣款限制，仍無法防止誤花，並會限制多對多資金來源。
2. **Transaction 修改/刪除造成歷史不一致：** 目前沒有一般 Transaction update endpoint，Goal-linked Transaction 禁止刪除，但一般 Transaction 可刪除並反向調整 Account balance。未來若 Allocation 引用來源交易，需定義已分配交易的刪除策略與 ledger reversal，不能留下懸空或失真的 Actual。
3. **AllocationRule 變更造成重新計算：** 若只保存一份可覆寫比例，歷史預算/建議無法重現。需版本化規則，並在 confirmed Allocation 記錄來源規則版本；禁止以現行規則重算歷史 Actual。
4. **Domain 複雜度增加：** Planned/Actual/Available、收入 eligibility、固定責任扣除順序、Goal contribution、帳戶現金、轉帳、撤銷與 concurrency 各有不同語意。若混入 Transaction、Goal.CurrentAmount 或 BudgetSnapshot，會造成重複計算與不一致。應分階段引入，先定義最小不可變事件與 reconciliation policy。
5. **重複配置/競態：** 同一 Income Transaction 可能由兩個請求同時確認分配，必須保證 sum 不超額並防止重複確認；需 transaction-level concurrency/idempotency 設計。
6. **Legacy 資料不完整：** 現有 Goal.CurrentAmount 與 Goal-linked Transactions 未必能證明所有歷史資金來源或當前仍保留餘額，不應無條件回填成完整 allocation history。

## 7. Migration 建議（未實作）

若未來核准實作，候選 schema 元素如下：

### 新增 Entity / Table 候選

- **AllocationRuleVersion：** User、版本、生效期間、比例項目、建立時間；舊版本保留不可覆寫。
- **Allocation：** User、來源 Income Transaction、用途類型、金額、可選 Goal、rule version、確認時間與更正/撤銷關聯。此為建議的 canonical confirmed-allocation ledger。
- **GoalContribution：** 只有在需要獨立生命週期/稽核語意時新增。若它與 Goal-targeted Allocation 表達同一事實，避免重複存 amount；可由 Allocation 投影出貢獻紀錄。
- **AllocationSuggestion：** 若產品要求保存被接受/拒絕的預測提案，再新增；若只需要即時計算建議，不需持久化。

### Migration/資料遷移注意事項

- 新表需有 User 隔離及適當 FK；Allocation 的 SourceTransaction 必須是同一 User 可配置的 Income Transaction，Goal 若有值也必須屬同一 User。
- 建立必要的 user/source transaction/goal/version 索引與 sum <= eligible source amount 的交易一致性策略；跨列總額限制不能只依賴一般單列 check constraint。
- 既有 SystemSetting AllocationRatio 沒有歷史版本；首版可建立使用者初始 rule version，但生效日期與歷史回填政策須先由產品確認。
- 不要僅以 Goal.CurrentAmount 回填 Allocation/GoalContribution 明細。只有可確定 Goal-linked Transfer/相關紀錄時，才可制定經核對的遷移；無法判定的部分應標示 legacy/unreconciled，而非偽造明細。
- 不要為 Allocation 直接增加 Account-to-Goal FK，除非另有可花用限制與轉帳一致性規則。

### 建議保持不變的現有 Entity

- **Transaction：** 保持實際 Income/Expense/Transfer 的事實來源；是否新增更新/反向事件語意需另外規格化。
- **Account：** 保持帳戶與實際餘額模型；不以 AccountType 宣稱資金已被鎖定。
- **Goal：** 保持目標定義與 lifecycle；`CurrentAmount` 是否改為貢獻 ledger projection，需分階段處理並保留 API compatibility。
- **SystemSetting：** 可在過渡期保留目前 AllocationRatio 作當前預設值來源；新版本歷史由 AllocationRuleVersion 保存，不覆寫過往規則。
- **FixedCommitment：** 保持 planned obligation；Allocation Engine 不自動建立付款 Transaction，也不把固定責任當作 Goal contribution。

## 8. 建議下一步實作順序

1. 產品先決定 Allocation 是用途 earmark 還是實際資金移轉，以及 Available 的精確語意。
2. 定義 eligible source Transaction、可配置上限、規則生效日期/版本選擇與 Goal contribution 的 canonical ledger。
3. 先做 Application/domain use-case 與 read-only proposal，區分 Suggestion/Confirmed Actual；不變更既有 Budget Radar 公式。
4. 以不可變 rule version 和 append-only allocation/reversal 記錄保護歷史。
5. 再設計 account reserve/withdrawal controls（若產品確實要求禁止誤花）。
6. 完成資料遷移與對帳策略後，另行核准 schema/API/UI phases。

## 9. 來源與限制

指定的 `docs/phase2-implementation-plan.md` 在目前 repository 中不存在。本分析以現有 Account/Transaction/Goal/SystemSetting/FixedCommitment/BudgetSnapshot 實作，以及 `Goal_Pyramid_Specification.md`、`CoreTransactionBudgetRadarSpec.md` 與 `phase3-budget-radar-spec.md` 為依據。
