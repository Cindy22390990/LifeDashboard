# JWT 身份驗證模組 Phase 2 修正版規格書

## 目標

本階段目的：

1. 確認 JWT 導入後 UserId Migration 狀態正確。
2. 確保正式環境 JWT Secret 可透過 Environment Variable 覆寫。
3. 補齊前端使用者註冊流程。
4. 完成基本 JWT 驗收測試。

本階段不重新設計 JWT 架構，只修正第一階段遺留問題。

---

# Phase 2 執行限制

開始修改前必須遵守：

1. 不修改 JWT Token 產生流程。
2. 不修改 Login API Contract。
3. 不新增 Refresh Token。
4. 不修改 Route Guard 架構。
5. 不修改 Account、Transaction、Setting 核心商業邏輯。
6. 不修改已 Apply 的 Migration。
7. 修改前必須確認：

- Git working tree 狀態
- Migration 清單
- Database Migration History

若 Migration 狀態未知：

禁止直接產生新的 Migration。

---

# 1. UserId Migration 狀態確認

## 目的

確認 JWT UserId Isolation Migration 是否安全。

檢查：

- Accounts UserId
- Transactions UserId
- Users Foreign Key

確認：

- 是否存在 NULL UserId
- 是否存在 Guid.Empty
- 是否存在 Orphan UserId

---

## 執行結果判斷

### 情境 A：資料正常

若確認：

- UserId 不為 NULL
- UserId 不為 Guid.Empty
- 所有 UserId 均存在 Users

則：

不新增 Backfill Migration。

原因：

目前資料已完成 UserId 關聯，不需要額外資料修復。

---

### 情境 B：存在歷史資料問題

若發現：

- NULL UserId
- Guid.Empty
- 無效 UserId

才允許：

新增一份新的 Backfill Migration。

禁止修改：

- 已 Apply Migration
- Migration Designer
- Model Snapshot

---

## Backfill Migration 要求

若需要新增：

必須包含：

1. 建立 Default User。

用途：

承接 JWT 導入前沒有 UserId 的歷史資料。

---

2. 回補資料：

處理：

- NULL UserId
- Guid.Empty UserId
- Invalid UserId

更新至 Default User。

---

3. 驗證：

確認：

- 無 NULL UserId
- 無孤兒資料
- Foreign Key 可正常建立

---

# 2. EF Core Model 一致性確認

確認：

目前 Entity、DbContext、Migration Snapshot 一致。

必要設定：

Account：

```csharp
Name -> MaxLength(50)

InitialBalance -> Precision(18,2)

CurrentBalance -> Precision(18,2)
```

驗證：

執行：

```bash
dotnet build

dotnet ef migrations has-pending-model-changes \
--project LifeDashboard.Infrastructure/LifeDashboard.Infrastructure.csproj \
--startup-project LifeDashboard.Api/LifeDashboard.Api.csproj
```

結果必須：

No changes have been made to the model since the last migration.

---

# 3. JWT Secret Configuration

## 目的

保留開發便利性，同時支援正式環境安全配置。

---

確認 ASP.NET Core Configuration Pipeline：

優先順序：

Environment Variable

>

appsettings.Development.json

>

appsettings.json


---

驗證：

設定：

```
JwtSettings__SecretKey
```

確認：

Environment Variable 可以覆寫設定。

---

禁止：

- Azure Key Vault
- AWS Secret Manager
- 修改 JWT Token 流程

---

# 4. 前端 Register Page

## 目的

補齊使用者註冊入口。

新增：

```
RegisterPage.tsx
```

---

功能：

輸入：

- Username
- Password


呼叫：

```
POST /api/auth/register
```

---

成功流程：

```
Register

↓

Navigate('/login')

↓

LoginPage 顯示：

註冊成功，請登入
```

禁止：

- 自動登入
- 直接進入 Dashboard

---

失敗：

要求：

- 顯示後端錯誤訊息
- 不清除目前登入狀態

---

# 5. JWT 驗收測試

## 5.1 多使用者隔離

流程：

User A:

新增 Account / Transaction

↓

Logout

↓

User B Login


確認：

User B 無法取得 User A 資料。

---

## 5.2 JWT 401 Handling

測試：

移除 Token

Request API


確認：

- API 回傳 401
- Axios Interceptor 執行 logout
- 導回 Login

---

## 5.3 Payload 防護

確認：

所有 Create API：

Client 不可傳入 UserId。

資料來源：

```
HttpContext.User Claim

↓

Backend Service

↓

Database
```

---

# 6. 完成條件

完成：

[ ] Migration 狀態確認完成

[ ] EF Model 與 Snapshot 一致

[ ] JWT Secret Environment Override 驗證完成

[ ] Register Page 完成

[ ] JWT E2E 驗收完成

完成後才進入下一功能階段。