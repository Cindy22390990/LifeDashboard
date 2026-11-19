# Architecture Review: Localization & Multi-Currency

## 1. Current Architecture Impact

**Frontend i18n (Low Impact to Backend, Medium Impact to Frontend)**
*   **Conflict:** If the frontend implements i18n by fetching translations from the backend, it violates the separation of concerns and creates unnecessary API latency.
*   **Resolution:** i18n must be purely a frontend concern (using libraries like `i18next`). The backend API contracts (e.g., `Category.Name`, `Goal.Name`) should remain in the user's input language. UI elements (menus, labels) are translated client-side.

**Backend Currency (High Impact to Core Domain)**
*   **Conflict:** Changing the `Transaction` entity from a simple `decimal Amount` to a `Money` value object (Amount + Currency) fundamentally alters the core domain model (Phase 1). It breaks existing balance calculations, reporting aggregations, and Goal Funding mappings.
*   **Resolution:** The core ledger (Transactions, Accounts, Goals) MUST remain in a single base currency (e.g., TWD) for MVP. Multi-currency should be treated as an edge-case feature for later phases.

**Database Migration (High Risk if rushed)**
*   **Conflict:** Adding a `Currency` column to `Transaction` and `Account` requires migrating all existing records to a default currency. Furthermore, historical transactions lack exchange rate data, making historical net worth calculations impossible if multiple currencies are retroactively applied.
*   **Resolution:** Delay database schema changes for multi-currency.

**Domain Model (Medium Risk)**
*   **Conflict:** The `GoalFundingAllocation` relies on exact amounts. Introducing exchange rates introduces rounding errors and discrepancies in "Goal Progress." If I transfer 100 USD (Exchange Rate 31.5) to a TWD goal, is it exactly 3150 TWD? Who owns the rounding error?
*   **Resolution:** Goal Funding MUST operate in the base currency only.

---

## 2. Risk Analysis

**High Risk Modifications (DO NOT DO IN PHASE 1)**
*   Modifying the `Transaction` entity to include `CurrencyCode` and `ExchangeRate`.
*   Modifying the `Account` entity to support multiple currencies per account.
*   Implementing live exchange rate fetching APIs.
*   Modifying `GoalFundingAllocation` to handle cross-currency mappings.

**Medium Risk Modifications (Defer to Phase 2/3)**
*   Allowing users to select a "Display Currency" for the Dashboard (requires real-time conversion of the base currency).
*   Adding timezone support for users traveling across borders (impacts `TransactionDate` and Calendar UI).

**Low Risk Modifications (Safe for Phase 1)**
*   Frontend UI translation (i18n) for static text, menus, and labels (English, Traditional Chinese, Portuguese, Korean, Japanese).
*   Adding a `PreferredLanguage` column to the `User` or `Settings` entity (if user profiles exist).
*   Frontend formatting of currency symbols based on locale (e.g., displaying `NT$` vs `$`).

---

## 3. Recommended Phasing

**Phase 1: Pure Frontend UI Localization (Safe & Immediate)**
*   **Scope:** Client-side only. Implement `i18next` or similar in React.
*   **Features:** Translate all static strings in `DashboardV3UXDesignSpec.md` into the 5 requested languages. Implement a language switcher in the Drawer menu.
*   **Backend Impact:** NONE.
*   **Currency Impact:** Formatting only. Ensure the frontend uses `Intl.NumberFormat` to display the base currency correctly according to the user's locale (e.g., thousands separators).

**Phase 2: Base Currency Standardization (Backend Prep)**
*   **Scope:** Backend configuration, no schema changes.
*   **Features:** Define a system-wide `BaseCurrency` setting (e.g., TWD). Ensure all API responses explicitly assume this currency. This prepares the system for future conversions without breaking existing logic.
*   **Currency Impact:** Formalize the assumption that `Amount = Base Currency`.

**Phase 3: True Multi-Currency (Future Architecture Expansion)**
*   **Scope:** Deep domain changes.
*   **Features:** Introduce `Money` Value Object. Add `ForeignCurrencyTransaction` subclass or related entities. Implement exchange rate caching and historical rate tracking.
*   **Currency Impact:** High. Requires significant ADRs and regression testing.

---

# LocalizationCurrencyImplementationPlan.md

**Goal:** Implement Localization (5 languages) in the Frontend without impacting the Backend Domain, APIs, or database schema. Lay the groundwork for future Multi-Currency without breaking the current single-currency assumption.

## 1. Frontend Implementation Specification (Phase 1)

### A. i18n Architecture
*   **Technology:** Use a robust localization library (e.g., `react-i18next`).
*   **Resource Management:** Store translations in local JSON files (e.g., `en.json`, `zh-TW.json`, `pt-BR.json`, `ko.json`, `ja.json`). Do not fetch these from the backend to avoid latency.
*   **State:** Store the selected language in `localStorage` and (if applicable) sync with backend user settings eventually.

### B. UI Translation Scope
*   Translate all Drawer navigation items (Dashboard, Ledger, Accounts, etc.).
*   Translate static headers (Hero Stats, Recent Transactions, Budget Radar).
*   Translate empty states (e.g., "Your ledger is clean").
*   **EXCLUSION:** Do NOT attempt to auto-translate user-generated content (e.g., Transaction Titles, Goal Names).

### C. Currency Formatting (Not Conversion)
*   **Rule:** The backend sends numbers (e.g., `125000`). The frontend formats them.
*   **Implementation:** Use standard JavaScript `Intl.NumberFormat`.
*   *Crucial Distinction:* This is formatting, not multi-currency. It ensures `$125,000` displays correctly based on locale rules, but it does NOT convert 125,000 TWD to USD.

## 2. Backend Constraints (Phase 1 & 2)

### A. API Contract Preservation
*   The `Phase5DashboardApiContract` remains unchanged.
*   `Amount` fields remain plain decimals. Do not introduce `Currency` fields yet.

### B. Domain Protection
*   The `Transaction`, `Account`, and `GoalFundingAllocation` entities remain untouched.
*   No database migrations are permitted for currency in Phase 1.

## 3. Future-Proofing for Phase 3 (Multi-Currency)

When LifeDashboard eventually requires true multi-currency (e.g., logging a transaction in JPY while the base account is TWD), the architecture will evolve as follows (DO NOT IMPLEMENT NOW):

1.  **Value Object:** Replace `decimal Amount` with a `Money` Value Object:
    ```csharp
    public record Money(decimal Amount, string CurrencyCode);
    ```
2.  **Exchange Rate Entity:** Introduce an entity to store historical exchange rates to ensure past transactions don't fluctuate when viewed today.
3.  **Cross-Currency Funding Risk:** We will need an ADR to decide if a Goal in TWD can be funded by a Transaction in USD, and how to handle the rounding/exchange spread.

## 4. Copilot Prompting Guide

When instructing Copilot, use these strict constraints:
*   "Implement React i18next for the Dashboard UI using local JSON files."
*   "Create translation keys for the Navigation Drawer and Hero Stats headers."
*   "Use Intl.NumberFormat to format numeric amounts from the API. Do NOT write currency conversion logic."
*   "Do not modify any C# backend code, DTOs, or database schemas."