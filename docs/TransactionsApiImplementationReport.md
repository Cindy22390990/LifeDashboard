# Transactions API Report

## Existing Architecture

沿用既有 V4 Transactions Read API 分層，未建立新的 Backend 架構：

- `V4TransactionsController`：JWT 保護的 `GET /api/transactions`。
- `IV4TransactionService` / `V4TransactionService`：Application 查詢服務與 response mapping。
- `V4TransactionQuery` / `IV4TransactionReader`：既有查詢物件與 Reader 介面。
- `V4TransactionReader`：EF Core 資料來源，沿用使用者隔離與 soft-delete query filter。
- `TransactionListResponseDto`：既有 response DTO，調整為 Backend Contract 所列欄位。
- `V4TransactionApiTests`：既有 API integration test fixture。

Quick Create 的 POST 行為未修改。

## Modified Files

### Backend

- `LifeDashboard.Api/Controllers/V4TransactionsController.cs`
- `LifeDashboard.Application/Features/V4/Interfaces/IV4Readers.cs`
- `LifeDashboard.Application/Features/V4/Models/V4TransactionProjection.cs`
- `LifeDashboard.Application/Features/V4/Dtos/V4TransactionDto.cs`
- `LifeDashboard.Application/Features/V4/Services/V4TransactionService.cs`
- `LifeDashboard.Infrastructure/Repositories/V4TransactionReader.cs`
- `LifeDashboard.Tests/V4/V4TransactionApiTests.cs`

### Frontend

- `LifeDashboard.Web/src/presentation/pages/dashboard-v4/transactions/TransactionsPage.tsx`

### Added Tests

- 擴充既有 GET 測試，驗證分頁、soft-delete 過濾及 response 不含 `totalCount`、`page`、`pageSize`。
- `Get_transactions_applies_all_requested_filters_and_sorting`：整合驗證帳戶、分類、關鍵字、交易類型、日期區間、排序及 totals。
- `Get_transactions_returns_problem_details_for_unsupported_query_values`：驗證未知交易類型與排序欄位回傳 RFC 7807 `400`。

## API Endpoint

### Request

`GET /api/transactions`，需要 JWT Bearer 驗證。

| Parameter | Behavior |
|---|---|
| `page` | 頁碼，預設 `1`，需大於等於 `1`。 |
| `pageSize` | 每頁筆數，預設 `20`，範圍 `1`–`100`。 |
| `keyword` | 模糊比對交易 `Note`。 |
| `accountId` | 依交易帳戶 ID 精確篩選。 |
| `categoryId` | 依交易分類 ID 精確篩選。 |
| `transactionType` | `Expense`、`Income` 或 `Transfer`，不分大小寫。 |
| `startDate` / `endDate` | ISO DateOnly 範圍，含起訖日；兩者同時提供時起日不可晚於迄日。 |
| `sortBy` | 支援 `transactionDate`、`amount`、`note`。未提供時使用交易日期排序。 |
| `sortDirection` | `asc` 或 `desc`，預設 `desc`。 |

為相容既有呼叫，保留 `year`、`month`、`date`、`type`。`date` 優先於月份篩選；`year` 與 `month` 須成對提供。`type` 可作為 `transactionType` 的舊名稱；若兩者不同則回傳 `400` ProblemDetails。

### Response

符合 `DashboardV4BackendContract.md` 的 response 欄位：

```json
{
  "totalIncome": 0,
  "totalExpense": 0,
  "items": []
}
```

`items` 含 `id`、`note`、`amount`、`transactionType`、`transactionDate`，以及既有實作提供的 `accountId`、`accountName`、`categoryId`、`categoryName`。

### Pagination

`page` 與 `pageSize` 在 Backend 執行資料分頁；收支 totals 依所有篩選條件計算，並不受當前頁影響。依 Backend Contract，response 不包含總筆數或頁碼欄位。

## Frontend Integration

TransactionsPage 使用既有 `dashboardV4Client`、`dashboardV4Keys.transactions.list()`、TanStack Query Infinite Query 與 `mapV4Transaction`。清單依交易日期分組，提供載入中、空清單、ProblemDetails error/retry 與載入更多狀態；未加入 API filter UI，原 prototype 的篩選與摘要區塊保持不變。

載入更多以當頁回傳筆數是否等於 `pageSize` 判斷。因 response 契約沒有總筆數，資料筆數剛好是 `pageSize` 整數倍時，最後可能多發一次空頁請求。

## Contract Differences

### Backend Contract

- Backend Contract 未定義 `page`、`pageSize`、`accountId`、`categoryId`、`transactionType`、`startDate`、`endDate`、`sortBy`、`sortDirection`；本階段依任務要求新增支援，並保留 Contract 已列出的舊篩選參數。
- Backend Contract 的 `keyword` 僅比對 Note；Product Specification 對搜尋欄位的描述較廣。本次遵循 Backend Contract，未將分類名稱納入 `keyword` 搜尋。
- `sortBy` 的支援值與日期範圍邊界在 Backend Contract 中未定義；目前實作採上述 allowlist，日期起訖皆包含。
- Transaction item 的 account/category 欄位在 Contract 標示為選填/TBD；目前 API 會輸出既有實作提供的欄位。

### Readiness Review / Frontend DTO

- Readiness Review 與現有 Frontend `TransactionListResponseDto` 型別包含 `totalCount`、`page`、`pageSize`；這與 Backend Contract 的 response 不一致。本次遵循最高優先級的 Backend Contract，移除 Backend response 的這三個欄位。
- 依「Frontend 只修改 TransactionsPage」限制，未修改共用 Frontend DTO/mapper；其 response 型別仍宣告上述三欄位，但此頁不讀取它們。後續應在允許修改 Frontend DTO 時同步更新型別與 mapper。

### Readiness

`DashboardV4BackendContract.md` 未修改。Backend response 與其列出的 response 欄位一致；查詢參數擴充及 Readiness/Frontend DTO 的分頁 metadata 差異如上。

## Validation

- `.NET build`：通過，0 warnings、0 errors。由於既有 `LifeDashboard.Api` 程序鎖定預設輸出 DLL，使用獨立暫存輸出目錄重跑。
- Backend tests：通過，149 passed、0 failed、2 skipped（共 151）。
- `npm run build`：通過；Vite 顯示既有 bundle 超過 500 kB 的提示。
- `npx tsc -b --pretty false`：通過。
- `npx eslint src/presentation/pages/dashboard-v4/transactions/TransactionsPage.tsx`：通過。
