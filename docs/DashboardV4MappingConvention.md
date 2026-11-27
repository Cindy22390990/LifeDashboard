# Dashboard V4 DTO Mapping Convention

> **Document Path**: `docs/DashboardV4MappingConvention.md`
>
> **Status**: Accepted

---

## 1. Core Data Flow Rule
Dashboard V4 嚴格執行單向資料轉換邊界，**禁止 UI Component 直接消費原始 API DTO**：

```text
API DTO (Backend Contract)
  ↓
Mapper (Pure Function)
  ↓
View Model (UI Ready Model)
  ↓
Component (Only Display)
```

### 範例對照
* ❌ **禁止**：`<Home>{data.dailyRemaining.toLocaleString()}</Home>`（UI 直接理解 API 欄位與格式化邏輯）。
* ✅ **標準**：`DashboardV4HomeDto` → `mapHomeViewModel()` → `DailyDecisionHeroModel` → `<DailyDecisionHero model={vm.hero} />`。

---

## 2. Directory Placement
V4 的 DTO、ViewModel 與 Mapper 各自獨立，**嚴禁共用或引用 `dashboard-v3/` 的 Mapper**：

```text
src/presentation/components/dashboard-v4/
├── api/          # V4 API Client 與原始 Response DTO 型別
├── mappers/      # 純函數 Mapper（例：mapHomeViewModel.ts, mapMeAssetsViewModel.ts）
└── models/       # 前端 ViewModel 型別定義（例：DailyDecisionHeroModel）
```

---

## 3. Mapper Responsibilities
1. **Enum 防禦轉換**：將 `accountType` / `transactionType` 安全對應至前端型別，未知字串一律降級為 `'Unknown'`。
2. **Nullable 與空字串防呆**：處理 `note?.trim()` 空值退回預設翻譯鍵值；集合欄位強制 `?? []`。
3. **日期安全處理**：`businessDate` 與 `transactionDate` 僅透過安全 `DateOnly` 工具格式化，嚴禁 `new Date("YYYY-MM-DD")`。
4. **純函數保證**：Mapper 不得包含副作用（不呼叫 API、不讀寫 Store），方便 100% 單元測試覆蓋。