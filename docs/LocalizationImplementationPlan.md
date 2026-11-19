# LifeDashboard Localization Phase 1 Implementation

請先閱讀：

- `docs/LocalizationImplementationPlan.md`
- `docs/DashboardV3UXDesignSpec.md`
- `docs/DashboardV3UXBlueprint.md`

本次只實作 Frontend Localization，不修改 Backend Domain。

## 目標

為 LifeDashboard V3 加入多語系支援：

支援語言：

- English (`en`)
- 繁體中文 (`zh-TW`)
- Português Brasileiro (`pt-BR`)
- 한국어 (`ko`)
- 日本語 (`ja`)

---

# Scope

## 1. Frontend i18n Setup

使用：

- `react-i18next`
- JSON translation resources

建立：

```
src/i18n/
 ├── index.ts
 └── locales/
      ├── en.json
      ├── zh-TW.json
      ├── pt-BR.json
      ├── ko.json
      └── ja.json
```

---

## 2. Translation Boundary

只翻譯系統 UI：

包含：

- Navigation Drawer
- Dashboard 標題
- Hero Stats Labels
- Budget Radar
- Goal Progress
- Reports
- Empty States
- Settings

不要翻譯：

- Transaction Title
- Transaction Note
- Goal Name
- User輸入內容

使用者建立的資料必須保持原始文字。

---

## 3. Language Setting

新增 Language Selector。

位置：

Navigation Drawer → System section

例如：

```
🌐 Language

English
繁體中文
Português
한국어
日本語
```

需求：

- 使用者選擇後立即切換 UI
- 儲存在 localStorage
- 未設定時使用 browser locale
- 不修改 Backend User Entity

---

## 4. Currency Formatting

本階段不是 Multi-Currency。

禁止：

- 修改 Transaction Entity
- 修改 Account Entity
- 新增 CurrencyCode 欄位
- 新增 Exchange Rate
- 修改 Database Migration

只建立前端 formatter：

```
formatCurrency(amount, currencyCode)
```

使用：

JavaScript Intl.NumberFormat

例如：

```
125000 TWD

zh-TW:
NT$125,000

en:
NT$125,000

ja:
￥125,000
```

注意：

這只是格式化，不是換匯。

---

# Architecture Constraints

禁止：

❌ 修改 C# Backend  
❌ 修改 DTO Contract  
❌ 修改 Database Schema  
❌ 修改 Money Value Object  
❌ 實作 Exchange Rate Service  
❌ 修改 Goal Funding Logic  


原因：

Multi-Currency 屬於未來 Domain Expansion，目前只保留架構方向。

---

# Implementation Order

請依序完成：

## Phase 1

建立：

- i18n configuration
- translation JSON
- language switch component

先讓 Drawer 可以切換語言。


## Phase 2

逐步替換：

硬編碼文字：

例如：

```
Dashboard
Accounts
Goals
Reports
Settings
```

改成：

```
t("navigation.dashboard")
```


## Phase 3

建立：

```
currencyFormatter.ts
```

並替換目前金額顯示。


## Phase 4

測試：

確認：

- 五種語言可以切換
- Refresh 後仍保留設定
- Dashboard V3 不受影響
- Build 通過


完成後請回報：

1. 修改檔案列表
2. Architecture 是否有被破壞
3. Build 結果
4. 下一步建議