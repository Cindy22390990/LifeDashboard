# Dashboard V3 UX Design Specification

## UX Risk Review

Before proceeding with the formal specification, let's analyze the potential risks in the initial V3 Blueprint draft:

| 問題 (Problem) | 風險 (Risk) | 建議修改 (Recommendation) |
|---|---|---|
| Full Monthly Calendar | Takes up too much valuable above-the-fold space on mobile. Most users don't need a 30-day view daily. | Default to a **Weekly View** (1 row) on mobile, expandable to Monthly. Desktop can retain Monthly view alongside Day Details. |
| Day Detail Panel taking full width | Disrupts context. If a user clicks a day to see details, full-screen takes them out of the calendar context. | Mobile: Use a **Bottom Sheet** (peek height). Desktop: Use a **Side Panel** next to the calendar. |
| Over-reliance on color for status | Accessibility issue. Red/Green alone might be difficult for colorblind users to parse quickly. | Reinforce color with clear **Icons (↑, ↓) and Typographic weight** (Income bold/green, Expense medium/black). |
| Reports relegated to Drawer | Analytics are crucial for financial health. Hiding them entirely behind a menu reduces engagement. | Introduce a lightweight **"Mini-Report" or "Insights summary"** on the Dashboard (e.g., spending trend sparkline) that links to full Reports. |
| Goal Card empty space for "Future Allocation" | "Future Allocation Placeholder" looks broken or unfinished. | Do not leave an explicit "placeholder" void. Design the card to look complete *now* (progress, remaining, deadline), and allow the future "Allocate" action to be integrated as a distinct, additive UI element (like a FAB or bottom action bar) when ready. |

---

## Section 1: Product UX Principle

**User Mental Model:**
Users approach a financial app seeking clarity and control, often accompanied by underlying anxiety. The app must act as a calm, authoritative source of truth. It should not feel like an accounting ledger (input-heavy) but rather a financial navigator (insight-heavy).

**First-Glance Priority (The 1-Second Rule):**
1. **Security/Grounding:** Net Worth (Total Assets) - *Do I have money?*
2. **Current Velocity:** Spent Today - *What did I just do?*
3. **Pacing:** Remaining Budget - *Can I buy this coffee right now?*
4. **Trajectory:** Goal Progress - *Am I moving forward?*

**What the Dashboard Must Answer:**
The Dashboard is a high-level summary, not an exhaustive list. It answers "How am I doing?" not "Let me audit my entire financial history."

---

## Section 2: Information Architecture

**Navigation Model:** Hamburger Drawer Menu.
*Why?* Bottom navigation consumes vertical screen real estate, which is critical for scrolling lists like transactions. A drawer categorizes complex features efficiently, keeping the primary view uncluttered.

**Drawer Sizing:**
*   **Desktop:** Fixed sidebar or 320px drawer.
*   **Mobile:** 70% viewport width, click off-canvas overlay to dismiss.

### Drawer Structure

**Primary Navigation (Daily/Frequent Use)**
*   **Dashboard** (Home, quick pulse check)
*   **Ledger** (Detailed transaction history, auditing)
*   **Accounts** (Checking balances, reconciling)
*   **Goals** (Checking progress, motivation)
*   **Reports** (Analytics, trends)

**Financial Management (Monthly/Occasional Setup)**
*   **Budgets** (Setting monthly limits)
*   **Fixed Commitments** (Recurring bills, subscriptions)
*   **Categories** (Organizing spending buckets)

**System (Rarely Used)**
*   **Settings**
*   **Dark Mode Toggle**
*   **Data Export**
*   **Security (FaceID/Passcode)**
*   **Logout**

---

## Section 3: Dashboard Layout

The layout prioritizes the "First-Glance" metrics, followed by actionable insights (Calendar), and then detailed summaries.

### Desktop Layout

```text
┌───────────────────────────────────────────────────────────────┐
│ ≡ Menu    │  Hello, Hsin.                     Nov 18, 2026    │
├───────────┴───────────────────────────────────────────────────┤
│                                                               │
│  [Hero Stats]                                                 │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│  │ Total Assets    │ │ Spent Today     │ │ Left to Spend   │  │
│  │ $125,000        │ │ $350            │ │ $12,400         │  │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│                                                               │
├───────────────────────────────┬───────────────────────────────┤
│ [Calendar Section]            │ [Day Detail Panel (Nov 15)]   │
│  Mo Tu We Th Fr Sa Su         │  Total: -$450                 │
│      ...                      │  ──────────────────────────   │
│  10 11 12 13 14[15]16         │  [Icon] Starbucks      -$150  │
│                 ↓450          │  [Icon] Groceries      -$300  │
│      ...                      │                               │
│                               │  [ + Add Transaction ]        │
├───────────────────────────────┼───────────────────────────────┤
│ [Budget Radar]                │ [Goal Progress]               │
│  ● Safe (65% used)            │  Japan Trip (65%)             │
│  [███████░░░░]                │  [███████░░░░]                │
├───────────────────────────────┼───────────────────────────────┤
│ [Recent Transactions]         │ [Account Summary]             │
│  [Icon] Salary       +$45,000 │  Hua Nan Bank        $80,000  │
│  [Icon] Lunch           -$150 │  Cash                 $5,000  │
│                  [View All]   │                   [View All]  │
└───────────────────────────────┴───────────────────────────────┘
```

### Mobile Layout

```text
┌───────────────────────────────┐
│ ≡   Dashboard        Nov 18   │
├───────────────────────────────┤
│ [Hero Stats]                  │
│ Total Assets       $125,000   │
│ Spent Today            $350   │
│ Left to Spend       $12,400   │
├───────────────────────────────┤
│ [Weekly Calendar (Default)]   │
│ Mo  Tu  We  Th  Fr [Sa] Su    │
│ 13  14  15  16  17  18  19    │
│     ↓   ↑           •         │
│          [Expand to Month v]  │
├───────────────────────────────┤
│ [Budget Radar]                │
│ ● Safe (65% used)             │
│ [███████░░░░]                 │
├───────────────────────────────┤
│ [Goal Progress]               │
│ Japan Trip (65%)              │
│ [███████░░░░]                 │
├───────────────────────────────┤
│ [Recent Transactions]         │
│ [Icon] Salary       +$45,000  │
│ [Icon] Lunch           -$150  │
│                  [View All]   │
├───────────────────────────────┤
│ [Account Summary]             │
│ Hua Nan Bank        $80,000   │
│                  [View All]   │
└───────────────────────────────┘

=================================
(When Calendar day is tapped, Bottom Sheet slides up)
┌───────────────────────────────┐
│ ─ (Handle)                    │
│ Nov 15 Details          [✕]   │
│ ───────────────────────────── │
│ Total: -$450                  │
│                               │
│ [Icon] Starbucks        -$150 │
│ [Icon] Groceries        -$300 │
│                               │
│ [ + Add Transaction ]         │
└───────────────────────────────┘
```

---

## Section 4: Calendar UX Decision

1. **Should Calendar always be expanded?** No. On mobile, default to a **Weekly View**. This saves vertical space while showing immediate past/future context. Desktop can default to Monthly due to ample screen real estate.
2. **Should Monthly View be collapsible?** Yes, on mobile. A chevron expands Weekly to Monthly if the user needs macro-level review.
3. **How are day clicks handled?**
    *   **Desktop:** Opens/updates a fixed Side Panel next to the calendar.
    *   **Mobile:** Slides up a Bottom Sheet (peek height, draggable to full height).
4. **Adding Transactions:** The "Add Transaction" button inside the Day Detail Panel pre-fills the selected date. It must open a modal supporting Income, Expense, and Transfer types.

---

## Section 5: Component Design

**React Component Tree:**

```text
App
 ├ NavigationDrawer
 │   └ DrawerMenu (Handles routing)
 │
 ├ DashboardPage
 │   │
 │   ├ HeroStats (Purpose: Immediate financial health. Data: Net Worth, Spent Today, Remaining Budget.)
 │   │
 │   ├ CalendarSection
 │   │   ├ CalendarWidget (State: View mode (Week/Month), Selected Date)
 │   │   └ DayDetailPanel (Desktop) / DayDetailBottomSheet (Mobile)
 │   │       └ TransactionList (Filtered by Selected Date)
 │   │
 │   ├ BudgetRadarWidget (Purpose: Pacing visualization. Data: Current vs Total Budget, Status color.)
 │   │
 │   ├ GoalProgressWidget (Purpose: Motivation. Data: Top 2-3 active goals, Progress %.)
 │   │
 │   ├ RecentTransactionsWidget (Purpose: Auditing recent activity. Data: Last 5 transactions globally.)
 │   │   └ TransactionItem
 │   │
 │   └ AccountSummaryWidget (Purpose: Liquidity check. Data: Active account balances.)
```

---

## Section 6: Transaction UX

**Goal:** Provide clear, scannable context without cognitive overload.

**Transaction Item Structure:**

```text
[ Category Icon (e.g., 🍔) ]

[ Title (e.g., "McDonald's") ]                 [ Amount (e.g., -$150) ]
[ Subtitle (e.g., "Lunch with team") ]         (Green for Income, Black for Expense)
[ Category Name • Account Name ]               [ Date (e.g., Nov 05) ]
```

*Example:*
```text
[ 💰 ] Salary                                +$45,000
       November Salary
       Income • Hua Nan Bank                 Nov 05
```

---

## Section 7: Goal UX

**Goal:** Present goals purely as progress trackers for now, avoiding "Deposit/Transfer" confusion, aligning with the upcoming Allocation Model.

**Goal Card Layout:**

```text
┌───────────────────────────────────────┐
│ ✈️ Japan Trip                        │
│ ───────────────────────────────────── │
│ Progress: 65%            Left: $35K   │
│ [█████████████░░░░░░░]                │
│ Saved: $65,000 / $100,000             │
│ Deadline: Dec 2027                    │
│                                       │
│ (Clean whitespace - NO deposit buttons)│
└───────────────────────────────────────┘
```

---

## Section 8: Reports UX

Reports are reinstated as a dedicated page for deep analytical dives.

**Reports Page Wireframe:**

```text
┌────────────────────────────────────────────────────────┐
│ 📈 Reports                   [ 2026 ▼ ] [ November ▼ ] │
├────────────────────────────────────────────────────────┤
│ [ Spending Trend (Line Chart) ]                        │
│ Shows daily cumulative spend against a budget baseline │
├────────────────────────────┬───────────────────────────┤
│ [ Income vs Expense (Bar)] │ [ Category Spend (Pie) ]  │
│ In: $50K | Out: $20K       │ 🍔 Food: 40%              │
│                            │ 🚗 Transport: 20%         │
├────────────────────────────┴───────────────────────────┤
│ [ Net Worth Trend (Area Chart) ]                       │
│ 6-month historical view of total assets                │
├────────────────────────────────────────────────────────┤
│ [ Cash Flow (Waterfall Chart) ]                        │
│ Income -> Fixed Commits -> Living -> Remaining         │
├────────────────────────────────────────────────────────┤
│ [ Goal Progress Trend (Multi-line Chart) ]             │
│ Tracking goal growth over time                         │
└────────────────────────────────────────────────────────┘
```

---

## Section 9: Design System

**Colors (Tailwind-inspired conventions):**
*   `Background`: `bg-slate-50` (Very light, cool gray for depth)
*   `Surface`: `bg-white` (Pure white for cards)
*   `Primary Text`: `text-slate-900`
*   `Secondary Text`: `text-slate-500`
*   `Income (Positive)`: `text-emerald-600`
*   `Expense (Negative)`: `text-slate-900` (Avoid red for standard expenses to reduce stress)
*   `Warning/Exceeded`: `text-rose-500` (Only use red when budget is breached or for destructive actions)

**Typography (Inter or San Francisco):**
*   `Hero Number`: 32px-40px, Semibold (Assets, Totals)
*   `Heading`: 18px-20px, Semibold (Card Titles)
*   `Body`: 14px-16px, Regular (Transaction titles, list items)
*   `Caption`: 12px, Regular, Secondary color (Dates, metadata)

**Spacing & Layout:**
*   Strict 8px grid (`p-4`, `p-6`, `gap-4`).
*   Card padding: minimum 16px (`p-4`), ideally 24px (`p-6`) for breathing room.

**Radius & Shadows:**
*   `Card Radius`: `rounded-2xl` (Soft, friendly edges)
*   `Button Radius`: `rounded-xl`
*   `Card Shadow`: `shadow-sm` (Very subtle elevation, `rgba(0,0,0,0.05)`)

---

## Section 10: Animation

Keep animations functional and subtle. Over-animation feels slow.
*   **Drawer**: Slide in from left (`translate-x`), 200ms ease-out. Overlay fades in (`opacity`), 200ms linear.
*   **Bottom Sheet**: Slide up from bottom, 250ms spring or ease-out.
*   **Calendar Expansion (Week -> Month)**: Height transition with content fade-in, 300ms ease-in-out.
*   **Card Loading**: Use skeleton screens (shimmer effect) matching the exact dimensions of the loaded content. No spinners.

---

## Section 11: Empty State

First-time user experience is critical. Don't show blank cards.

*   **No Accounts:** "Welcome! Let's start by adding your primary checking account." [Button: Add Account]
*   **No Transactions (Calendar/Recent):** "Your ledger is clean. Record your first income or expense today." [Button: Add Transaction]
*   **No Goals:** "What are you saving for? Set a goal to track your progress." [Button: Create Goal]

---

## Section 12: Loading / Error State

*   **Dashboard API Loading:** Display skeleton loaders for Hero Stats, Radar, and Goal widgets simultaneously. Ensure layout doesn't jump when data arrives.
*   **Calendar Loading:** Show a skeleton grid for the calendar month while fetching month-boundary transaction data.
*   **API Error:** Show a non-intrusive toast or inline error state within the specific card (e.g., Goal card fails to load, show a subtle "Failed to load goals [Retry]" within the card, don't crash the whole dashboard).

---

## Section 13: Copilot Implementation Checklist

**Phase 1: Pure UI Scaffold (Layout & Components)**
*   Setup Tailwind config with Design System tokens (Colors, Typography, Radius).
*   Build `NavigationDrawer` (UI only, no routing).
*   Build `DashboardPage` grid layout (Responsive: CSS Grid/Flexbox).
*   Build static components with Mock Data: `HeroStats`, `CalendarSection` (Weekly default), `BudgetRadarWidget`, `GoalProgressWidget`, `RecentTransactionsWidget`, `AccountSummaryWidget`, `TransactionItem`.
*   *Constraint: NO API calls, NO backend changes, pure UI slice.*

**Phase 2: API Integration (Existing APIs)**
*   Integrate Zustand for state management.
*   Connect `DashboardPage` to `GET /api/dashboard/summary` and `GET /api/budget/radar`.
*   Replace Mock Data with fetched data for Hero Stats, Budget Radar, Accounts, and Recent Transactions.
*   Implement Skeleton Loaders and Error handling.

**Phase 3: Goal Funding & Interactions (Future)**
*   Wait for backend GoalFunding APIs.
*   Connect `GoalProgressWidget` to real Read Model data.
*   Implement "Add Transaction" modal triggered from Calendar `DayDetailPanel`.