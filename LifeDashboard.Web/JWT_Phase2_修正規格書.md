# JWT 身份驗證模組 Phase 2 修正規格書

本階段著重於解決第一階段 JWT 實作遺留的資料遷移風險、環境變數安全性，以及補齊前端註冊介面與基礎驗收測試。

本階段禁止重新設計 JWT 核心流程，只針對既有架構進行補強。

---

# Phase 2 執行限制

開始修改前必須遵守：

1. 不重新設計 JWT Token 產生流程。
2. 不修改 Login API Contract。
3. 不修改 Account、Transaction、Setting 核心商業邏輯。
4. 不新增 Refresh Token。
5. 不修改已完成 Route Guard 架構。
6. 修改前必須檢查 Migration 狀態。
7. 禁止直接修改已 Apply 的舊 Migration。

---

# 1. 資料庫遷移與舊資料回補

目標：

處理 Accounts 與 Transactions 的 UserId 關聯，避免 Guid.Empty 或無效 UserId 導致 Foreign Key 問題。

## 執行限制

必須新增新的 Backfill Migration。

禁止修改已經套用至資料庫的舊 Migration。

## 執行流程

1. 檢查目前 Migration 與 Database 狀態。

確認：

- Accounts UserId 建立方式
- Transactions UserId 建立方式
- Foreign Key 現況


2. 建立 Default User。

要求：

- 使用 migrationBuilder.Sql() 建立 Default User。
- Insert 資料必須符合目前 Users Entity 必要欄位。
- 不修改 User Entity 設計。


3. 舊資料回補。

處理：

- Guid.Empty UserId
- 無效 UserId
- 尚未建立 UserId 關聯的資料


若 UserId 尚未建立：

流程：

Nullable UserId
→ 回補資料
→ 驗證資料
→ Required UserId
→ Foreign Key


4. 驗證：

確認：

- 無 NULL UserId
- 無孤兒資料
- Foreign Key 可正常建立


禁止修改：

- JWT Claim
- Token Generator
- Authentication Flow


---

# 2. JWT Secret Configuration

目標：

確認 .NET Configuration Pipeline 可以支援環境變數覆寫。

## 規則

保留：

- appsettings.json
- appsettings.Development.json

中的開發環境設定。

不要因修改 Secret 導致目前 Token 測試失效。


設定優先順序：

Environment Variable

>

appsettings.Development.json

>

appsettings.json


## 限制

本階段禁止新增：

- Azure Key Vault
- AWS Secret Manager
- 其他 Secret Provider


只需確認 IConfiguration 可以被 Environment Variable 覆寫。


---

# 3. Frontend Registration

建立：

`RegisterPage.tsx`


功能：

- Username 輸入
- Password 輸入
- 基本前端驗證


流程：

使用者註冊

↓

POST /api/auth/register

↓

成功

↓

Navigate('/login')

↓

顯示：

「註冊成功，請登入」


限制：

禁止：

- 自動 Login
- 自動跳 Dashboard
- 修改 Login Flow


失敗：

- 顯示後端錯誤訊息
- 不清除目前登入狀態


---

# 4. Phase 2 驗收標準

## 1. 多使用者資料隔離

驗證：

User A：

- Login
- 建立 Account / Transaction


Logout。


User B：

Login 後：

GET API

不得取得 User A 資料。


---

## 2. 401 Interceptor

驗證：

移除 Token。

發送 API Request。


確認：

- Axios Interceptor 攔截 401
- 清除 Auth State
- 導向 Login


---

## 3. 防禦性 Payload

檢查：

所有 Create API：

- DTO
- Controller


確認：

Client 不可指定 UserId。


UserId 必須：

JWT Claim

↓

Backend

↓

Entity


---

## 4. Migration 驗證

使用測試資料庫模擬 Migration 前狀態。


情境：

Existing Account / Transaction 沒有 UserId 關聯。


執行：

dotnet ef database update


確認：

- 資料成功掛載 Default User
- 無 Migration Error