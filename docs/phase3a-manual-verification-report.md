# Phase 3A Manual Verification Report

## Summary

| Case | Result |
|---|---|
| V-A Empty salary amount | PASS |
| V-B Amount = 0 | PASS |
| V-C Negative amount | PASS |
| V-D No account selected | PASS |
| V-E Empty transaction date | PASS |
| E-01 API rejection | FAIL — controlled rejection worked, but an unintended transaction was created during the first interception attempt |
| E-02 Double-click during submit | PASS |

## Validation Cases

The baseline after P-01 was one transaction and an account balance of 60,000.
For each validation case, the UI blocked submission, no Transaction POST was
sent, and the read-only database check showed the same transaction count and
balance. The Dashboard did not change.

| Case | Browser action | UI result | Network/API | Database verification | Expected / actual |
|---|---|---|---|---|---|
| V-A Empty salary amount | Cleared the amount and submitted with a valid account and date | Displayed a message requiring a salary amount greater than 0 | No POST | 1 transaction; balance 60,000, unchanged | Blocked as expected — PASS |
| V-B Amount = 0 | Entered 0 and submitted | Displayed a message requiring a salary amount greater than 0 | No POST | 1 transaction; balance 60,000, unchanged | Blocked as expected — PASS |
| V-C Negative amount | Entered -1 and submitted | Displayed a message requiring a salary amount greater than 0 | No POST | 1 transaction; balance 60,000, unchanged | Blocked as expected — PASS |
| V-D No account selected | Entered a valid amount and date, left the account unselected, and submitted | Displayed a message to select a valid account | No POST | 1 transaction; balance 60,000, unchanged | Blocked as expected — PASS |
| V-E Empty transaction date | Entered a valid amount and account, cleared the date, and submitted | Displayed a message to select the transaction date | No POST | 1 transaction; balance 60,000, unchanged | Blocked as expected — PASS |

## E-01 API Rejection

The first E-01 interception attempt did not match the request because of URL
case sensitivity. As a result, a valid 50,000 salary submission reached the
API and succeeded. This created an unintended second 50,000 Income Transaction.

For the subsequent controlled rejection check, the browser test interceptor
changed the outgoing amount to 0. The API returned HTTP 400, the UI displayed
the validation error and retained the form input, and that rejected request
did not change the database.

The controlled 400 behavior passed, but E-01 is recorded as **FAIL** because
the test execution also caused an unintended successful transaction. A
separate mocked 401 response redirected the application to the login page; it
was not counted as an actual backend rejection test. A delayed request attempt
timed out, but the read-only database check showed no write from that attempt.

## E-02 Double-Click During Submit

With an amount of 0.01, the submit button was disabled while the request was
in progress. A rapid double-click produced one POST, which returned HTTP 201,
and one new transaction. The UI showed a success message.

The database count increased from 2 to 3, and the balance increased from
110,000 to 110,000.01. This verifies UI-level duplicate prevention only; it
does not provide backend idempotency.

## Final State and Evidence

The final read-only database check found three Income Transactions for the
test account:

| Transaction | Amount | Date | Account |
|---|---:|---|---|
| `F9BBFB95-64E7-4523-A0B2-840FA01C4F70` — original P-01 | 50,000 | 2026-11-13 | `f4f7f43a-efb5-4aa7-8357-fec66faf149d` |
| `19071466-6A15-49C9-8E56-ABBEC317E8BA` — unintended E-01 test submission | 50,000 | 2026-11-13 | `f4f7f43a-efb5-4aa7-8357-fec66faf149d` |
| `48DE2117-725F-431C-AF48-A5786D5D1590` — E-02 | 0.01 | 2026-11-13 | `f4f7f43a-efb5-4aa7-8357-fec66faf149d` |

The account balance is 110,000.01. The Dashboard displays the balance rounded
to 110,000; Budget Radar displays a living budget of 100,000.01. A browser
screenshot of the transaction list showed the two 50,000 entries and the
0.01 entry.

No source code or database schema was modified. Database checks were
read-only; no transaction was manually removed or reverted.

## New Discoveries and Risks

- The UI blocked blank, zero, negative, missing-account, and missing-date
  submissions without sending Transaction POST requests.
- E-02 confirmed that the submit button is disabled during submission and
  that the rapid double-click produced only one request and one transaction.
  This is frontend duplicate prevention, not backend idempotency.
- The E-01 interception mismatch allowed a valid 50,000 request through. The
  resulting transaction remains in the test database.
- A mocked 401 response triggered the existing redirect to the login page;
  the controlled 400 response was shown as a form error.
- Dashboard balance formatting omits cents, so the displayed 110,000 differs
  in precision from the database balance of 110,000.01.

## Test Environment Cleanup

The application implementation is verified.

However, the test environment is no longer clean because one unintended
Income Transaction was created during the first E-01 interception attempt.

Recommended action:

**Option A:** Delete the unintended transaction and restore account balance.

**Option B (Preferred):** Create a new clean Phase3A test account and rerun
only E-01.

No source code change is required.

## Phase 3A Completion Status

**Functionality verification: PASS.** P-01, validation cases V-A through V-E,
the controlled API 400 rejection behavior, and E-02 UI-level double-click
prevention were verified.

**Test data cleanliness: NOT CLEAN.** The unintended E-01 transaction remains
in the test environment. Cleanup is required before future repeated tests.
The E-01 execution incident remains recorded above; it does not change the
separate result that the application functionality verification passed.

## Next Action / Blocking Status

- **Phase 3A application verification:** PASS
- **Test environment:** NOT CLEAN
- **Blocking reason:** The E-01 interception mismatch created an unintended
  Income Transaction:
  - **TransactionId:** `19071466-6A15-49C9-8E56-ABBEC317E8BA`
  - **Amount:** 50000

Before starting Phase 3B, decide how to proceed:

- **Option A:** Clean up the transaction in the Development DB and restore the
  account balance.
- **Option B:** Create a new Phase3B/Phase3A-clean test account to avoid
  modifying the existing test data.

**No source code change required.**
