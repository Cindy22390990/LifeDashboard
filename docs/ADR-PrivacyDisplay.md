# Architecture Decision Record: Privacy Display Mode

## Status
**Approved**

## 1. Context & Decision

In a personal finance application, users frequently open the dashboard in public spaces (e.g., commuting, office). Displaying large, highly visible net worth and budget numbers poses a privacy risk.

**Decision:** 
Financial sensitive values must support "Privacy Masking" (displaying dots or asterisks instead of numbers). 
Crucially, **the frontend presentation layer exclusively controls this visibility**. The backend API will remain completely unaware of this UI state and will **always return actual financial values**.

---

## 2. Scope of Masking

Not all data needs to be hidden. Masking non-sensitive data or masking data in deep-dive audit views reduces the usability of the app.

**Sensitive Fields (Subject to masking):**
* Total Assets (Net Worth)
* Account Balances
* Goal Saved Amounts & Targets
* Dashboard Financial Summary (Spent Today, Remaining Budget)

**Non-Sensitive Fields (Remains visible):**
* Transaction Category (Icon / Name)
* Date and Time
* Account Names (e.g., "Hua Nan Bank", unless the name contains an amount)
* UI Labels and Navigation elements

---

## 2.1 Visibility Policy

Privacy Display is designed for dashboard privacy, not full application obfuscation. 

Default masking scope:

| Area | Mask |
|---|---|
| Dashboard Hero Stats | Yes |
| Account Summary | Yes |
| Goal Progress Amount | Yes |
| Calendar Summary Amount | Optional |
| Recent Transaction Amount | Optional |
| Full Ledger | No (User intent indicates audit mode) |

The goal is preventing accidental exposure during casual viewing, while preserving financial management usability.

---

## 3. Interaction Design

The masking should be seamless and instantly recognizable without breaking the layout.

**Dashboard Hero Stats Example:**

*Normal State:*
> Total Assets
> **NT$ 1,250,000**
> Spent Today: NT$ 350 | Remaining: NT$ 12,400

*Privacy Mode State:*
> Total Assets
> **••••••**
> Spent Today: •••• | Remaining: ••••

---

## 4. Trigger Mechanism

**Decision:**
Use a **Global State** controlled by a quick toggle (👁 / 👁‍🗨 icon) strategically placed in the Hero section or the top navigation bar, mirrored by a toggle in the Drawer menu for discoverability. Toggling it anywhere affects the targeted areas instantly.

---

## 5. State Management

This is purely a client-side display concern.

*   **Responsibility:** Frontend only.
*   **Technology:** Zustand (React State Management).
*   **Store Structure Blueprint:**
    
```javascript
privacyStore: {
  isAmountHidden: boolean,
  toggleVisibility: () => void
}
Persistence: Use localStorage (via Zustand persist middleware) so the user's preference is remembered across sessions.

Security Disclaimer for Persistence: The stored value ONLY represents UI preference ({"isAmountHidden": true}) and contains NO financial data, tokens, or authentication information.

6. Security Boundary
This feature does NOT encrypt data.
This is a "Shoulder Surfing" prevention mechanism, not a cryptographic security feature.

The backend still transmits the raw, unencrypted numeric values in the JSON API payload.

Anyone inspecting network traffic (DevTools) can see the actual amounts.

The application assumes the device network and browser environment are already secure.

7. Future Extension (Strictly Out of Scope for Phase 1)
This architecture leaves room for advanced privacy features in later phases, but must not be implemented in Phase 1:

Biometric Unlock (FaceID / TouchID): Require biometrics to toggle isAmountHidden. This is heavily deferred because it requires PWA capability, WebAuthn, Native wrappers, and OS permissions, extending far beyond simple React UI development.

Timeout Auto-hide: Automatically revert isAmountHidden to true after 3 minutes of inactivity or when the app goes to the background.

Blur Mode: Using CSS filter: blur(5px) instead of explicit dots (••••), providing a softer visual aesthetic.