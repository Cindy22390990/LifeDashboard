# Existing Category Sources

| Layer | Location | Purpose |
|---|---|---|
| Domain | `LifeDashboard.Domain.Entities.Category` | Defines user-owned category identity, name, icon, bucket type and creation time. |
| Database | `Categories` table from InitialCreate migration; `LifeDashboardDbContext.Categories` | Persists category fields and indexes categories by user and name. |
| API | `GET /api/categories` via `CategoryController` | Existing read endpoint for the legacy Category management flow. |
| API | `POST/PUT/DELETE /api/categories` | Existing Category CRUD; outside Phase 4.3. |
| Application | `ICategoryRepository`, `CategoryRepository`, `ICategoryService`, `CategoryService` | Existing category persistence, mapping and CRUD application flow. |
| DTO | `CategoryDto`, `CreateCategoryDto`, `UpdateCategoryDto`, `CategoryBreakdownDto` | Legacy category payloads; breakdown DTO is reporting/budget-adjacent and is not reused for the read model. |
| Frontend | `CategoryService`, `categoryStore`, `CategoryManagementModal`, `TransactionPage` | Existing legacy category read/write and selector usage. |
| Frontend | `dashboard-v4/categories/CategoriesPage.tsx` | V4 prototype page; currently placeholder only. |

## Current Domain Meaning

Current `Category` represents a user-defined transaction classification record with:

- Id
- Name
- Icon
- BucketType
- Creation time

`BucketType` is the existing enum:

- Living
- Entertainment
- Savings
- Investment
- Responsibility

The implementation does not confirm that `BucketType` means expense/income category type. It should not be relabeled or interpreted as budget allocation rules in this read slice.

Existing transaction data can reference `CategoryId` and expose `categoryName`; this confirms Category is related to Transaction classification. The domain does not currently define:

- a separate expense category entity
- an income category entity
- a category status/active flag
- display order
- default-category ownership on Category
- category monthly usage count

## Open Questions

- Are categories expense-only, income-only, shared, or type-independent?
- Are system categories supported?
- Is a category status or inactive flag required?
- Is display order required?
- Is icon a fixed catalog identifier or free text?
- Is one default category allowed per user or transaction type?
- How is Uncategorized represented?
- Should category name uniqueness be enforced within a user?
- Is category deletion/retirement compatible with existing transactions?
- Is `BucketType` a transaction classification or only a budget/bucket concept? The documents currently show overlap, but do not define the exact boundary.
