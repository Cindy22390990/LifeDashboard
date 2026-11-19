Architecture Decision Record: Multi-Language and Multi-Currency Design

1. Architecture Decision

Localization (i18n) Boundary

Decision: Localization is strictly a Frontend responsibility (Presentation Layer).

Rationale: The Clean Architecture Backend should remain completely language-agnostic. Business rules, Domain Models, and Use Cases do not change based on whether the user speaks English or Japanese.

Backend Contract: The backend will only return system-standard identifiers (e.g., Category: "FOOD_AND_DRINK") and standardized Error Codes (e.g., ERR_INSUFFICIENT_FUNDS). The frontend will map these to the selected language. User-generated content (e.g., Transaction Notes, Goal Names) remains as originally inputted.

Multi-Currency Boundary

Decision: Currency is a Domain responsibility.

Rationale: A number without a currency has no financial meaning. Currency dictates arithmetic rules (e.g., you cannot directly add USD 100 and JPY 100).

Core Concept: Introduce a Money Value Object (VO) containing Amount and CurrencyCode (ISO 4217). Every financial entity must use this VO or contain both fields.

2. Database Design

To support Multi-Currency, we need schema changes.

Modified Entities:

User / SystemSettings: Add BaseCurrencyCode (e.g., "TWD"). This is the anchor currency used for the Dashboard's "Total Assets" calculation.

Account: Add CurrencyCode. An account is strictly single-currency (e.g., a USD checking account, a TWD savings account).

Transaction: Add CurrencyCode. By default, it inherits the CurrencyCode of the Account it belongs to.

Goal: Add CurrencyCode. The target amount and current funding must be measured in a specific currency.

FixedCommitment: Add CurrencyCode.

3. Frontend Design

i18n Architecture

Framework: Use react-i18next (or similar standard library).

Dictionaries: Maintain static JSON files for en, zh-TW, pt-BR, ko, ja.

Language Detection:

User's explicit setting in App (Highest priority).

Browser/OS locale (Fallback).

Currency Display & Formatting

Separation of Language and Currency: UI Language and Currency Formatting must be decoupled. A user might use the App in Traditional Chinese (zh-TW) but view an account in Japanese Yen (JPY).

Native Formatting: Rely on Intl.NumberFormat in the browser, passing the exact CurrencyCode from the backend API. Do not hardcode currency symbols (like $, ¥) in the UI or translation files.

4. Backend Design

Currency Domain Model

Value Object: Create a Money Value Object in the Domain layer. All calculations must validate that CurrencyCodes match before adding or subtracting.

Exchange Rate Interface: To calculate "Total Assets" on the Dashboard, the DashboardService needs to aggregate multiple accounts in different currencies. Introduce an IExchangeRateService interface in the Application Layer.

DTO Updates

Every API response containing an amount must now return a composite object or two fields.

Old: totalAssets: 125000

New: totalAssets: 125000, currency: "TWD"

5. Migration Plan

Schema Update: Add CurrencyCode columns to existing tables (Account, Transaction, Goal, FixedCommitment) and BaseCurrency to User Settings.

Data Backfill (Defaulting): We cannot have null currencies. During migration, all existing records must be updated to a default currency.

Strategy: Derive the default currency from the user's previously known locale, or force a global default (e.g., "TWD" or "USD") for the V1 migration, prompting the user to update it upon their next login.

Zero Downtime: Ensure the backend API sets the default currency for any incoming legacy requests during the transition period.

6. Risk Analysis

Risk

Impact

Mitigation

Total Asset Fluctuation

Users might panic if their Dashboard "Total Assets" drops suddenly due to exchange rate volatility, not actual spending.

Add a tooltip or subtle UI indicator explaining that Total Assets are estimated based on current exchange rates.

Cross-Currency Transfers

Transferring 100 USD to a TWD account involves exchange rates and fees. If not handled, accounts will fall out of sync.

For Phase 1, treat cross-currency transfers as two separate transactions (Expense in USD, Income in TWD) where the user manually inputs the exact amounts for both sides.

Floating Point Errors

Using standard floats for multiple currencies (especially cryptos or high-inflation currencies).

Strictly enforce decimal types in C# and avoid floating-point math in the frontend.

7. What NOT To Do Now (Avoid Over-engineering)

To ensure smooth delivery, do not implement the following in this phase:

Real-time FX API Integration: Do not integrate with external APIs (like Fixer.io or OANDA) yet. Use a static, hardcoded exchange rate dictionary in the backend, or allow the user to manually set a fixed custom rate for their own dashboard.

Multi-Currency Wallets/Ledgers: Do not allow a single Account entity to hold multiple currencies simultaneously. If a user has a multi-currency bank account, represent it as multiple separate Account entities in our system.

Historical Exchange Rates: Do not attempt to calculate past net worth based on historical exchange rates on the exact day of a transaction. Use the current exchange rate for all Dashboard aggregations.

8. Copilot Implementation Steps

Phase 1: Domain & Database

Create Money Value Object.

Update EF Core Entities to include CurrencyCode.

Generate and run EF Core Migration (with default currency backfill script).

Phase 2: API Contract Refactoring

Update all DTOs (Dashboard, Transaction, Account) to include CurrencyCode.

Implement a mock IExchangeRateService with static rates for Dashboard aggregation.

Phase 3: Frontend i18n Scaffold

Install i18n libraries.

Extract hardcoded strings in the Navigation Drawer and Hero Stats into translation JSONs for en and zh-TW first.

Phase 4: Frontend Currency Formatting

Create a global currency formatter utility using Intl.NumberFormat.

Apply the formatter to all amount displays across the React components.