# JWT Authentication & Identity 重構狀態紀錄

## 目前階段

本文件用於記錄 LifeDashboard 專案 JWT 身份驗證模組目前完成狀態，以及下一階段交由 Copilot 執行的工作範圍。

開發流程：

- Gemini：需求分析、架構規格、Code Review
- Copilot：依規格修改 Repository、實作功能、測試
- 開發者：確認範圍，避免 AI 擴大需求

---

# 一、目前已完成項目

## 後端 Authentication

已完成：

- User Entity 建立
- Users Table Migration
- PasswordHash 儲存
- BCrypt 密碼加密
- JWT Token Generator
- Login API
- Register API

API：

```
POST /api/auth/register
POST /api/auth/login
```

JWT Token 包含：

- UserId
- Username

Claim：

```
ClaimTypes.NameIdentifier
```

---

## 後端授權

已完成：

- Program.cs JWT Authentication 設定
- UseAuthentication()
- UseAuthorization()

已套用：

- AccountController
- TransactionController
- SettingController

使用：

```csharp
[Authorize]
```

---

## 使用者資料隔離

目前架構：

```
JWT Token
    ↓
Controller 取得 UserId
    ↓
Service 接收 UserId
    ↓
Repository 使用 UserId 查詢資料
```

目前未發現：

- Controller 信任前端 UserId
- Service 使用固定使用者 ID
- Guid.Empty 作為執行時 UserId

---

# 二、目前尚未完成項目

## 1. 前端登入流程

目前需要確認與補齊：

- LoginPage
- RegisterPage
- Auth Store
- Token 儲存
- 登入狀態管理
- Logout

---

## 2. React Router 權限保護

需要完成：

未登入：

```
/dashboard
        ↓
/login
```

登入：

```
/login
        ↓
/dashboard
```

---

## 3. Axios 401 處理

目前需要確認：

收到：

```
HTTP 401 Unauthorized
```

後是否：

- 清除 Token
- 清除登入狀態
- 導向 Login Page

---

## 4. Migration 資料安全

目前發現：

Migration 曾使用：

```
Guid.Empty
```

作為舊資料 UserId。

需要確認：

- 舊資料是否能正常 Migration
- UserId 是否應改為 Required
- 是否需要資料回補策略

---

## 5. JWT Secret 安全

目前：

JWT Secret 在：

```
appsettings.json
```

正式環境應改：

- Environment Variable
- User Secrets
- Secret Manager

---

# 三、JWT 完成標準

完成後應符合：

## Backend

- [ ] Register 可建立使用者
- [ ] Login 可取得 JWT
- [ ] JWT 包含 UserId Claim
- [ ] Account API 必須驗證 Token
- [ ] Transaction API 必須驗證 Token
- [ ] Setting API 必須驗證 Token
- [ ] 不接受前端傳入 UserId
- [ ] 不存在固定 Guid 使用者

---

## Frontend

- [ ] Login Page
- [ ] Token 儲存
- [ ] Axios 自動加入 Bearer Token
- [ ] 401 自動登出
- [ ] 未登入禁止進入 Dashboard
- [ ] 登入後可正常使用原功能

---

# 四、目前不要處理項目

以下不屬於 JWT 第一階段：

- Refresh Token
- OAuth 登入
- 多角色 RBAC
- Goal 模組
- Dashboard 統計
- 財務核心重構

等待 JWT 穩定後再處理。

---

# 五、下一步 Copilot 任務

目標：

完成 JWT 前後端整合驗證，不新增額外功能。

執行順序：

1. 檢查目前 Repository 真實狀態。
2. 找出 JWT 規格與目前程式差異。
3. 補齊缺少的 Authentication Frontend Flow。
4. 修正登入錯誤狀態碼。
5. 測試完整登入流程。

---

# 六、驗收測試

完成後測試：

## 註冊

輸入：

```
username
password
```

確認：

- 建立 User
- PasswordHash 不為明碼

---

## 登入

確認：

成功：

```json
{
  "token": "...",
  "userName": "..."
}
```

---

## Token API 測試

無 Token：

```
401 Unauthorized
```

有 Token：

```
200 OK
```

---

## 多使用者隔離

User A：

只能看到自己的：

- Account
- Transaction
- Setting

User B：

不能存取 User A 資料。