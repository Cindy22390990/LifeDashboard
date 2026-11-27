# ADR-V4 Transaction Default Resolver Decision

## Status

Accepted

---

# Context

Phase 2 已支援：

- `UserPreference.DefaultExpenseAccountId`
- `UserPreference.DefaultCategoryId`
- Quick Create transaction resolver

Phase 2 驗證發現 Category fallback priority、No Cashflow Account 錯誤語意，
以及 Preference Account 類型限制需要明確決策。本 ADR 記錄已採用的決策。

---

# Decision 1: Category Resolver Priority

目前程式實際流程：

```text
Preference
↓
Recent Category
↓
Most Used Category
↓
Uncategorized
```

現有文件中的流程：

```text
Recent Category
↓
Most Used Category
↓
Preference
↓
Uncategorized
```

Decision：

- **Preference 優先**。Resolver 採用程式目前已實作的順序：有效 User Default Category → Recent Category → Most Used Category (30 days) → Uncategorized。
- 既有設定仍會驗證 Category 的 User ownership；無效設定會進入後續 fallback。

---

# Decision 2: No Cashflow Account Behavior

採用的流程：

```text
Resolver
↓
NoCashflowAccountException
↓
V4ProblemDetailsExceptionHandler
↓
HTTP 422
```

Decision：

```text
422 Unprocessable Entity
```

ProblemDetails：

```text
type: https://api.lifedashboard/errors/no-cashflow-account
title: Cashflow Account Required
```

- 使用者需要先建立至少一個有效的 Cashflow Account。
- 此情況是可由使用者修正的商業驗證錯誤，不是找不到任意資源。

---

# Decision 3: Preference Account Validation

目前已驗證：

- Account 必須存在。
- Account 必須屬於目前 User。
- Account 不可為 soft-deleted。
- Account 不可為 System Account。

Decision：

- `DefaultExpenseAccountId` 必須指向目前使用者擁有、未刪除、非系統且 `AccountType == Cashflow` 的帳戶。
- UserPreference Application Service 拒絕儲存不符合條件的設定。
- Resolver 對既有無效設定不使用該值，繼續採用最近 30 天使用最多的 Expense Cashflow Account，再 fallback 至第一個有效 Cashflow Account。

---

# Impact

這些決策會影響：

- Quick Create 的預設 Account。
- Quick Create 的預設 Category。
- Quick Create 失敗時的 HTTP status 與 ProblemDetails。
- Dashboard V4 Integration 的 error handling contract。
- Frontend 是否保留 Sheet、導向 Account 建立流程或顯示一般錯誤。

以上決策為 Quick Create 的最終 Contract，Integration 應依此處理。
