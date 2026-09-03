# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module
## Frontend / UI Specification

| | |
|---|---|
| **Document** | frontend.md |
| **Version** | 1.0 |
| **Date** | 3 September 2026 |
| **Companion docs** | `spec.md`, `architecture.md`, `todo.md` |

---

## 1. UX Principles

1. **Compliance should feel invisible until it needs attention.** Most invoices should flow through and validate without the user ever thinking about MyInvois — surface status quietly (a badge), and only interrupt the user when something needs a decision (a rejection, a missing TIN, an expiring certificate).
2. **Never let a user submit a document you already know will fail.** Run local mandatory-field validation before the "Submit" action is even enabled.
3. **Explain LHDN errors in plain language.** Never show a raw `CF403` or `DS326` code without a human-readable explanation and a suggested fix.
4. **Respect the 72-hour clock visually.** Anywhere a document can be cancelled/rejected, show a countdown, not just a date.
5. **Bilingual by design.** English and Bahasa Malaysia are both first-class; no hardcoded English strings in components.

---

## 2. Information Architecture / Module Map

```
Malaysia Compliance (new top-level or Finance sub-menu)
├── Tax (SST)
│   ├── Tax Codes
│   ├── Tax Rates
│   └── SST Return Workspace
├── e-Invoice
│   ├── Company e-Invoice Profile (TIN, cert, environment)
│   ├── Document Queue / Dashboard
│   ├── Document Detail
│   ├── Consolidated e-Invoice Batches
│   ├── Credit / Debit / Refund Notes
│   ├── Self-Billed Documents
│   └── Certificates & API Credentials
├── Master Data extensions (embedded into existing screens)
│   ├── Customer master → Tax & e-Invoice tab
│   ├── Vendor master → Tax & e-Invoice tab
│   └── Item master → Classification tab
└── Reports
    ├── e-Invoice Submission Status
    ├── SST-02 / SST-02A
    ├── Reconciliation
    └── Compliance Dashboard
```

---

## 3. Screens

### 3.1 Tax Codes (list + form)
- **Purpose**: Admin CRUD for tax codes mapped to LHDN tax types.
- **Key elements**: table (Code, Description, LHDN Tax Type, Active), "New Tax Code" form with a dropdown constrained to the valid LHDN tax type set (`01/02/03/04/E/ZRL`) — not free text, to prevent invalid codes reaching a document.
- **States**: empty state prompts seeding from a default Malaysia tax-code template.

### 3.2 Tax Rates (list + form, effective-dated)
- **Purpose**: Manage rate history/future changes without breaking historical documents.
- **Key elements**: table grouped by tax code, showing effective date ranges; "Schedule a Rate Change" form (rate %, effective_from) that visibly warns if it overlaps an existing period.
- **UX note**: display a timeline mini-chart per tax code so a rate change (e.g., 6%→8%) is visually obvious.

### 3.3 SST Return Workspace
- **Purpose**: Prepare the bi-monthly SST-02 (Service) / SST-02A (Sales) figures.
- **Key elements**: period picker, computed totals by tax code, drill-down to underlying transactions, an "export" action producing the data in the format Finance needs for MySST portal filing, and a "mark as filed" status tracker.

### 3.4 Company e-Invoice Profile
- **Purpose**: One-time and periodically-updated setup — TIN, BRN, SST number(s), MSIC code, registered address, environment (Sandbox/Production) selector, and digital certificate upload/status.
- **Key elements**: environment selector is visually distinct (e.g., a persistent colored banner: "SANDBOX MODE" in amber) to prevent test submissions being mistaken for production ones. Certificate card shows issuer, expiry date, and a health indicator (green/amber/red as expiry approaches).

### 3.5 Document Queue / Dashboard
- **Purpose**: The main day-to-day screen — every e-Invoice-eligible transaction and its status.
- **Key elements**:
  - Status filter chips: `Queued`, `Submitted`, `Valid`, `Invalid`, `Cancelled`, `Rejected`
  - Table columns: Document No., Type (Invoice/CN/DN/Refund/Self-billed), Buyer, Amount, Tax Code, Status badge, Submitted At, 72h window countdown (for Valid docs), Actions
  - Bulk actions: "Submit selected," "Retry failed"
  - Summary tiles at top: counts by status, documents nearing 72h expiry, documents blocked on missing data
- **Empty/error states**: if the Integration Service is unreachable, show a clear banner ("e-Invoice service unavailable — documents are queued and will submit automatically") rather than a generic error, since this must never look like data loss.

### 3.6 Document Detail
- **Purpose**: Drill-down into one document's full lifecycle.
- **Key elements**:
  - Status timeline stepper: Created → Signed → Submitted → Validated/Invalid → (Cancelled/Rejected, if applicable)
  - LHDN metadata panel: UUID, submission UID, validation link (clickable), QR code preview
  - Field-level view of the mapped UBL data (collapsible, for troubleshooting)
  - If Invalid: plain-language error explanation + "Fix & Resubmit" action that routes back to the source ERP transaction
  - If Valid and within 72h: "Cancel Document" action with a required reason field and a live countdown
  - If Valid and past 72h: "Cancel" is disabled/hidden; instead show "Issue Credit/Debit/Refund Note" as the primary action

### 3.7 Consolidated e-Invoice Batches
- **Purpose**: Monthly B2C aggregation workflow.
- **Key elements**: period selector, a **preview table** showing what will be aggregated vs. what's being excluded (with the RM10,000 carve-out called out explicitly, e.g., "3 receipts excluded — must be invoiced individually," linking to those), totals, and a "Generate & Submit" confirmation step (irreversible action gets a confirm dialog).

### 3.8 Credit / Debit / Refund Notes
- **Purpose**: Issue adjustment documents against a prior Valid e-Invoice.
- **Key elements**: a searchable picker for the original document (by number or by scanning/pasting the UUID), auto-populated buyer/supplier data (read-only, inherited from the original), an amount/reason entry, and a clear label distinguishing "Credit Note (no cash returned)" from "Refund Note (cash returned)" since users conflate these.

### 3.9 Self-Billed Documents
- **Purpose**: Handle the LHDN-defined self-billed scenarios (payments to agents, foreign suppliers, individual acquisitions, etc.).
- **Key elements**: a scenario picker constrained to the guideline's defined list (not free text), sourced from AP transactions, with the roles of "Supplier" and "Buyer" visually swapped/labeled clearly since this is the most common source of user confusion in this module.

### 3.10 Certificates & API Credentials
- **Purpose**: Admin-only screen to manage digital certificates and OAuth Client ID/Secret pairs per environment.
- **Key elements**: certificate list with expiry health indicators; secrets are **write-once, never displayed again** after entry (standard secret-management UX — show a masked value with a "Rotate" action, not an "Edit" that reveals the old value).

### 3.11 Customer / Vendor Master — Tax & e-Invoice Tab
- **Purpose**: Capture the buyer/supplier-side fields needed for the 55-field structure without cluttering the main master screen.
- **Key elements**: ID Type selector (BRN / NRIC / Army / Passport / Old BRN) that dynamically changes the ID Value field's validation pattern; **inline TIN validation** — a small spinner + checkmark/error icon appears as the user tabs out of the TIN field, calling the cached TIN-validation endpoint; SST Registration Number field with an "N/A" quick-fill for non-SST-registered parties.

### 3.12 Item Master — Classification Tab
- **Purpose**: Default classification code and unit of measure per item, so invoice lines don't require manual classification every time.
- **Key elements**: searchable classification-code picker (typeahead against the local code-table cache from `architecture.md` §3.6), not a raw numeric code field.

### 3.13 Reports
- **e-Invoice Submission Status report**: filterable by date/status/document type; exportable.
- **SST-02 / SST-02A report**: matches the return form layout to minimize manual transcription.
- **Reconciliation report**: ERP-posted transactions vs. MyInvois-confirmed documents, highlighting any gap (posted but never submitted; submitted but not Valid).
- **Compliance Dashboard**: single view of current phase applicability, 12-month rolling turnover vs. thresholds, certificate health, and open (Invalid/blocked) documents — designed for the Tax/Compliance officer, not day-to-day AR staff.

---

## 4. Key User Flows

**Flow A — Issue an invoice that auto-submits cleanly**
1. AR clerk creates a Sales Invoice as normal in the existing ERP screen (unchanged).
2. On posting, a status chip appears: "e-Invoice: Queued."
3. Within seconds/minutes it updates to "Valid" with a QR icon.
4. Printed/emailed invoice automatically includes the QR + validation link — no extra clerk action.

**Flow B — Handle a rejected document**
1. Dashboard summary tile shows "2 Invalid."
2. User opens Document Detail, reads the plain-language error ("Buyer TIN not recognized by LHDN — please verify with the customer").
3. User fixes the customer's TIN in the Customer master (TIN re-validates inline).
4. User clicks "Fix & Resubmit" from the original invoice; a new document is generated and submitted.

**Flow C — Month-end consolidated e-Invoice**
1. Compliance officer opens Consolidated e-Invoice Batches, selects the closed month.
2. Reviews the preview (aggregated total, excluded RM10,000+ receipts listed with links).
3. Confirms "Generate & Submit."
4. Dashboard reflects the new consolidated document's status like any other.

**Flow D — Cancel within the window**
1. User opens a Valid document within 72 hours, sees the countdown ("58h 12m remaining to cancel").
2. Clicks "Cancel Document," enters a reason, confirms.
3. Status updates to "Cancelled"; audit log entry recorded.

---

## 5. Shared Component Library

| Component | Notes |
|---|---|
| **Status Badge** | Consistent color coding: gray=Queued, blue=Submitted, green=Valid, red=Invalid, slate=Cancelled/Rejected. Reused across Dashboard, Detail, Reports. |
| **72-Hour Countdown Chip** | Shows remaining time; turns amber under 12h, red under 2h. |
| **TIN Validator Input** | Debounced validation call, inline icon feedback, tooltip explaining accepted formats/general TINs (e.g., `EI00000000010` for general public). |
| **QR/Validation Link Preview** | Renders the LHDN QR and a copyable validation URL. |
| **Classification Code Picker** | Typeahead over the local code-table cache, shows code + description. |
| **Status Timeline Stepper** | Reused in Document Detail; horizontal on desktop, vertical on mobile. |
| **Tax Breakdown Table** | Line items with tax code, rate, and amount, used on invoice entry and document detail. |
| **Environment Banner** | Persistent, dismissible-but-reappearing banner when in Sandbox mode. |

---

## 6. Empty / Error / Loading States

- **Integration Service down**: banner + queued badge, never a blocking modal.
- **No documents yet**: Dashboard shows a short explainer + link to the Company e-Invoice Profile setup if that hasn't been completed.
- **Certificate missing/expired**: a persistent, dismissible-but-recurring alert on the Dashboard and Company Profile screens; submission actions are disabled with a tooltip explaining why.
- **Rate limit backoff in progress**: Dashboard shows "Submitting — MyInvois is busy, retrying automatically" rather than an error state, since this is expected behavior, not a failure.

---

## 7. Accessibility & Localization

- All status meanings conveyed by icon + text, not color alone (color-blind accessibility).
- Full English / Bahasa Malaysia string coverage; date/currency formatting follows Malaysian conventions (DD/MM/YYYY, RM with thousand separators).
- Keyboard-navigable tables and forms; screen-reader labels on all status badges and countdown chips.

## 8. Real-Time Updates

- Document status changes (Queued→Submitted→Valid/Invalid) should reflect on the Dashboard without a manual refresh — via polling the internal API (`/internal/einvoice/documents/{id}`) at a reasonable interval, or a lightweight push mechanism (WebSocket/SSE) if the ERP's frontend stack already supports one. Do not poll the MyInvois API directly from the browser — always through the Integration Service (see `architecture.md` §8).

## 9. Traceability

Every screen above maps to functional requirements in `spec.md` §7 and components/endpoints in `architecture.md` §3 and §8 — see `todo.md` for the corresponding build tasks.
