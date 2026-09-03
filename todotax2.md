# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module
## Implementation Backlog (todo.md)

| | |
|---|---|
| **Document** | todo.md |
| **Version** | 1.0 |
| **Date** | 3 September 2026 |
| **Companion docs** | `spec.md`, `architecture.md`, `frontend.md` |

**Priority legend:** `P0` blocks go-live · `P1` needed for full compliance/robustness · `P2` hardening/nice-to-have
Traceability: each item references the relevant requirement ID from `spec.md` where applicable.

---

## 0. Pre-Build Prerequisites (not engineering tasks, but blocking)

- [ ] `P0` Confirm current e-Invoice mandate threshold and phase applicability for this business against `hasil.gov.my/en/e-invoice` (verify the RM3 million figure at build time — it has changed twice in nine months).
- [ ] `P0` Confirm SST registration status/rates applicable to the business's product/service mix against `mysst.customs.gov.my`.
- [ ] `P0` Register on MyInvois Portal (sandbox and production); obtain Client ID/Secret for both environments.
- [ ] `P0` Procure a digital certificate from an MCMC-licensed CA (e.g., Pos Digicert, MSC Trustgate, Rafftech).
- [ ] `P1` Decide build-vs-buy for the UBL/XAdES/API plumbing layer (see `architecture.md` §16).

---

## 1. CRUD

### 1.1 Tax (SST) Module
- [ ] `P0` Tax Code CRUD (code, description, LHDN tax type mapping `01/02/03/04/E/ZRL`) — *TAX-01*
- [ ] `P0` Tax Rate CRUD with effective-dating (rate %, effective_from/to, per tax code) — *TAX-02*
- [ ] `P0` Company SST Registration profile CRUD (Sales Tax no., Service Tax no., registration dates) — *TAX-05*
- [ ] `P1` Tax determination rule CRUD (mapping item category / customer type / place of supply → tax code) — *TAX-03*
- [ ] `P2` Exempt/zero-rated supply category CRUD — *TAX-08*

### 1.2 e-Invoice Module — Master Data
- [ ] `P0` Company / Taxpayer Profile CRUD (TIN, BRN, SST no.(s), MSIC code, registered address, contact) — *EINV-01*
- [ ] `P0` Customer master extension: TIN, ID Type (BRN/NRIC/Army/Passport/Old BRN), ID Value, SST Reg No., address fields — *EINV-02*
- [ ] `P0` Vendor master extension: same fields as Customer, for self-billed/AP scenarios — *EINV-02*, *EINV-06*
- [ ] `P1` Item/Service master extension: default classification code, unit of measure mapping — *EINV-03*
- [ ] `P0` Digital Certificate CRUD (upload, view metadata/expiry, rotate, revoke; private key never exposed post-upload) — *EINV-08*
- [ ] `P0` API Credential CRUD (Client ID/Secret per environment; write-once secret display) — *EINV-09*
- [ ] `P1` Local Code Table CRUD/viewer (currency, tax type, classification, MSIC, state, unit, payment method) with "last synced" indicator — *EINV-17*
- [ ] `P2` Self-billed scenario reference data CRUD (the LHDN-defined list of applicable circumstances) — *EINV-06*

---

## 2. Transactional

### 2.1 Core Document Generation & Submission
- [ ] `P0` Generate UBL 2.1 Invoice document from a posted Sales Invoice (map all mandatory fields; block on missing data with a clear message) — *EINV-04*
- [ ] `P0` Local mandatory-field validation prior to signing (fail fast before consuming a submission) — *EINV-04*
- [ ] `P0` Digitally sign outbound document (XAdES) using vaulted certificate — *EINV-08*
- [ ] `P0` Submit document(s) to MyInvois — Submit Documents API, single and batched (≤100 docs/≤5MB/≤300KB per doc) — *EINV-09*
- [ ] `P0` Idempotency/dedupe check before submission (internal hash-based guard) — *EINV-18*
- [ ] `P0` Poll submission status until terminal (Valid/Invalid); write back UUID, validation link, QR — *EINV-10*
- [ ] `P0` Surface Invalid errors in plain language; support corrected resubmission as a new document — *EINV-11*
- [ ] `P0` Buyer/Supplier TIN validation (Validate Taxpayer TIN API, with caching) — *EINV-13*
- [ ] `P0` QR code + validation link generation and attachment to printed/emailed invoice — *EINV-14*
- [ ] `P1` Foreign-currency invoice handling (exchange rate field, currency validation) — *EINV-19*

### 2.2 Adjustment Documents
- [ ] `P0` Issue Credit Note referencing original invoice UUID (value reduction, no cash refund) — *EINV-05*
- [ ] `P0` Issue Debit Note referencing original invoice UUID (additional charge) — *EINV-05*
- [ ] `P0` Issue Refund Note referencing original invoice UUID (cash refund confirmation) — *EINV-05*
- [ ] `P1` Issue Self-Billed Invoice for applicable AP scenarios (agents, foreign suppliers, individual acquisitions, etc.) — *EINV-06*
- [ ] `P2` Issue Self-Billed Credit/Debit/Refund Note variants — *EINV-06*

### 2.3 Consolidated e-Invoice (B2C)
- [ ] `P1` Build consolidated e-Invoice payload from eligible B2C receipts for a period — *EINV-07*
- [ ] `P0` Exclude and flag any single receipt ≥ RM10,000 from consolidation (must be invoiced individually) — *EINV-07*, compliance rule §9 of `spec.md`
- [ ] `P1` Preview screen data assembly (aggregate totals + excluded-items list) prior to submission — supports `frontend.md` §3.7
- [ ] `P1` Submit consolidated document via the standard submission pipeline — *EINV-09*

### 2.4 Lifecycle Management
- [ ] `P0` Cancel document within 72-hour window (reason required; block after window closes) — *EINV-12*
- [ ] `P1` Record buyer-side rejection within 72-hour window (if ERP also receives documents) — *EINV-12*
- [ ] `P1` Redirect user to Credit/Debit/Refund Note flow automatically once the 72h window has lapsed on a document the user is trying to cancel — *EINV-12*
- [ ] `P0` Full audit trail write on every status transition (who/when/what) for 7-year retention — *EINV-15*

### 2.5 SST Transactional
- [ ] `P0` Apply tax determination engine output to each invoice line at entry time — *TAX-03*
- [ ] `P0` Tax-inclusive/exclusive pricing and rounding per RMCD convention — *TAX-04*
- [ ] `P1` Roll up posted transactions into the bi-monthly SST-02 / SST-02A computation — *TAX-06*

---

## 3. Reports

- [ ] `P0` e-Invoice Submission Status report (by status/date/document type, drill-down to document) — *EINV-10, EINV-11*
- [ ] `P0` Invalid/Rejected Document report with LHDN error reasons and resolution status — *EINV-11*
- [ ] `P1` SST-02 (Service Tax) return report, matching the RMCD form layout — *TAX-06*
- [ ] `P1` SST-02A (Sales Tax) return report, matching the RMCD form layout — *TAX-06*
- [ ] `P1` Consolidated e-Invoice summary report (aggregated vs. excluded items by period) — *EINV-07*
- [ ] `P1` Sales Tax / Service Tax collected report by tax code and period — *TAX-01, TAX-02*
- [ ] `P0` Reconciliation report: ERP-posted transactions vs. MyInvois-confirmed documents (gap detection) — *EINV-15*
- [ ] `P1` Audit trail / change log report (document lifecycle, master data changes affecting tax fields) — *EINV-15*
- [ ] `P1` Certificate & API credential expiry report — *EINV-08, EINV-09*
- [ ] `P2` Compliance Dashboard: rolling 12-month turnover vs. e-Invoice/SST thresholds, phase applicability status — *TAX-07*
- [ ] `P2` Buyer/Supplier data-quality report (missing TIN, unvalidated TIN, incomplete classification codes) — *EINV-02, EINV-03*

---

## 4. Background / Scheduled Jobs

- [ ] `P0` OAuth token cache/refresh job respecting ~60-minute token lifetime (avoid re-login per call) — *architecture.md §9*
- [ ] `P0` Submission status polling worker (poll Get Submission with backoff until terminal state, ~300 RPM budget) — *EINV-10*
- [ ] `P0` Failed-submission retry queue with exponential backoff and a dead-letter state after max attempts — *architecture.md §11*
- [ ] `P1` Monthly consolidated e-Invoice batch job (auto-run at month-end + within the 7-day submission grace window) — *EINV-07*
- [ ] `P0` Digital certificate expiry monitor & alert job (e.g., 30/14/7-day warnings) — *EINV-08*
- [ ] `P1` 72-hour cancellation/rejection window expiry notifier (reminds users before the window closes) — *EINV-12*
- [ ] `P1` TIN validation cache refresh/expiry job — *EINV-13*
- [ ] `P1` LHDN code table sync job (currency, tax type, classification, MSIC, state, unit, payment method) — *EINV-17*
- [ ] `P1` Data retention/archival job enforcing the 7-year minimum retention for documents and logs — *EINV-15*
- [ ] `P2` Duplicate-document detection sweep (mirrors LHDN's own dedupe window as a pre-emptive local check) — *EINV-18*
- [ ] `P2` Rate-limit-aware batching scheduler that groups queued documents into optimal submission batches during high-volume periods (e.g., month-end close) — *architecture.md §12*
- [ ] `P2` Turnover threshold monitor (alerts Compliance officer as rolling 12-month revenue approaches SST or e-Invoice mandate thresholds) — *TAX-07*
- [ ] `P2` Notification/webhook listener for any push-based status updates MyInvois may offer, with polling as the guaranteed fallback — *architecture.md §3.4*

---

## 5. Suggested Build Sequence

1. **Foundation (P0 CRUD)**: Tax Code/Rate, Company Profile, Customer/Vendor extensions, Certificate/Credential management.
2. **Core transactional flow (P0)**: Invoice generation → signing → submission → polling → status write-back → QR on print, plus TIN validation and the token-refresh/polling/retry background jobs.
3. **Compliance-critical guardrails (P0)**: RM10,000 individual-invoice rule, 72-hour cancellation window, audit trail, reconciliation report.
4. **Adjustment documents (P0/P1)**: Credit/Debit/Refund Notes.
5. **Consolidated e-Invoice (P1)**: batch job, preview, submission.
6. **Self-billed scenarios (P1/P2)**: lower transaction volume, can follow initial go-live.
7. **Reporting & dashboards (P1/P2)**: SST returns, compliance dashboard, data-quality reports.
8. **Hardening (P2)**: batching optimization, duplicate-detection sweep, threshold monitors.

Go-live gate: all `P0` items complete and passing UAT in the **sandbox** environment against real (or realistic) transaction data, followed by a deliberate, access-controlled cut-over to production credentials/certificate per `architecture.md` §10.
