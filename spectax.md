# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module
## Functional & Business Specification

| | |
|---|---|
| **Document** | spec.md |
| **Version** | 1.0 |
| **Date** | 3 September 2026 |
| **Status** | Draft for engineering review |
| **Applies to** | Existing ERP — Malaysia localization module |

---

## 1. Purpose & Background

The company operates an existing ERP system (Sales, AR/AP, Inventory, Finance). Malaysian law now requires that:

1. **Sales & Service Tax (SST)** be correctly calculated, charged, and reported to the Royal Malaysian Customs Department (RMCD/JKDM).
2. **Every qualifying commercial transaction** be issued as a structured, government-validated **e-Invoice** through LHDN's **MyInvois** platform before it is legally recognized as an invoice.

This specification defines the requirements to add a **Tax (SST) module** and an **e-Invoice module** to the existing ERP, integrating with MyInvois without a ground-up rewrite of the ERP's transactional core.

This is a compliance-driven build. Functional correctness must match the **Inland Revenue Board of Malaysia (LHDN/IRBM)** e-Invoice Guidelines and the **Royal Malaysian Customs Department (RMCD)** SST rules current at build time, and must be **re-validated against the official sources before go-live**, since both frameworks change by government notice (most recently: the e-Invoice exemption threshold was revised twice within the last nine months).

---

## 2. Regulatory Context

### 2.1 e-Invoice / MyInvois Mandate

MyInvois is LHDN's platform that validates invoices in near real time and returns a unique identifier (UUID), a long validation link, and a QR code before a document is legally an "e-Invoice." Submission is via the MyInvois Portal (manual) or the **e-Invoice API / SDK** (system-to-system) — this project targets the API route.

**Implementation phases (by annual revenue/turnover):**

| Phase | Annual Revenue | Mandatory From | Relaxation Period Ends | Full Enforcement |
|---|---|---|---|---|
| Phase 1 | > RM100 million | 1 Aug 2024 | 31 Jan 2025 | In force |
| Phase 2 | RM25 million – RM100 million | 1 Jan 2025 | 30 Jun 2025 | In force |
| Phase 3 | RM5 million – RM25 million | 1 Jul 2025 | 31 Dec 2025 | In force |
| Phase 4 | RM3 million – RM5 million | 1 Jan 2026 | 31 Dec 2027 | 1 Jan 2028 |
| New businesses (commenced 2023–2025) with revenue ≥ RM3 million | — | 1 Jul 2026 | 31 Dec 2027 | 1 Jan 2028 |
| **Exempt** | **< RM3 million** | Not required (may opt in voluntarily) | — | — |

> ⚠️ **The exemption threshold changed twice in the last nine months**: RM500,000 → RM1,000,000 (Cabinet decision, 6 Dec 2025) → **RM3,000,000** (PM's National Day announcement, effective **1 September 2026**, formalized in e-Invoice General Guideline v4.8). During the "relaxation period" for each phase, consolidated e-invoicing and reduced penalties apply, but the RM10,000 rule below is **not** relaxed. Confirm the current threshold against `hasil.gov.my/en/e-invoice` before locking scope, since the ERP must determine whether the client company is in scope at all.

**Key transactional rule:** any **single transaction of RM10,000 or more** must be issued as its own individual, LHDN-validated e-Invoice — it can never be folded into a monthly consolidated e-Invoice, regardless of phase or relaxation period (effective 1 Jan 2026, applies to B2B, B2C, and B2G).

**Document lifecycle:** once validated, Valid documents can be **cancelled by the Supplier** or **rejected by the Buyer** within a **72-hour window**. After 72 hours the document is final; any correction after that must be made via a **Credit Note, Debit Note, or Refund Note** referencing the original document — not a resubmission.

**Document types supported by MyInvois:**
- Invoice
- Credit Note (reduces value of a prior invoice, no cash refund)
- Debit Note (additional charge on a prior invoice)
- Refund Note (confirms a cash refund to the buyer)
- Self-Billed Invoice, Self-Billed Credit Note, Self-Billed Debit Note, Self-Billed Refund Note (buyer issues on behalf of the supplier — e.g., payments to agents, foreign suppliers, e-commerce, interest, individual acquisitions; see Specific Guideline §8)

**Consolidated e-Invoice (B2C):** where an individual consumer does not request a tax invoice at point of sale, receipts may be aggregated into a single monthly consolidated e-Invoice submitted within 7 days after month-end, using the LHDN general TIN `EI00000000010` and buyer name "General Public" — **except** any single receipt ≥ RM10,000, which must always be issued individually.

**Format & transport:** documents are UBL 2.1 structured **JSON or XML**, digitally signed (XAdES) with a certificate from an MCMC-licensed Certificate Authority, and submitted via REST API. Submission limits: **max 100 documents/submission batch, 5 MB per submission, 300 KB per document**, recommended **100 requests/minute** per Client ID for submission.

**Core data requirement:** every e-Invoice needs up to **55 data fields** across 8 categories (Address, Business Details, Contact Number, Invoice Details, Parties, Party Details, Payment Info, Products/Services); 37 are mandatory, 18 optional, plus annex fields for specific scenarios (shipping to a third party, etc.).

### 2.2 Sales & Service Tax (SST)

SST is a **single-stage** tax (not VAT/GST) — no input tax credit; it is a cost embedded in price. Administered by RMCD via the MySST portal.

| | Sales Tax | Service Tax |
|---|---|---|
| Applies to | Manufacture/import of taxable goods | Provision of taxable services |
| Standard rate | 5% or 10% (by HS code) | 8% (raised from 6% on 1 Mar 2024) |
| Reduced rate | — | 6% retained for F&B, telecommunications, parking, logistics, and select July-2025 categories |
| Registration threshold | RM500,000 taxable turnover / 12 months | RM500,000 (RM1,000,000 for leasing/rental & financial services; RM1,500,000 for construction & private healthcare; RM60,000/student/year for private education) |
| Return | SST-02A, bi-monthly | SST-02, bi-monthly |
| Filing/payment | Due within 30 days after taxable period end | Same |
| Input tax credit | None | None |

Businesses below the SST threshold **still fall under the e-Invoice mandate** once their revenue crosses the e-Invoice threshold — they issue e-Invoices using an **Exempt (E)** or **Zero-Rated (ZRL)** tax code rather than 01 (Sales Tax)/02 (Service Tax).

**e-Invoice tax type codes (relevant subset):** `01` Sales Tax, `02` Service Tax, `03` Tourism Tax, `04` High-Value Goods Tax, `E` Exempt, `ZRL` Zero-rated. These codes are validated against LHDN's published code table at submission time.

### 2.3 Sources (verify against these before build sign-off)

- IRBM e-Invoice microsite: `hasil.gov.my/en/e-invoice`
- e-Invoice General & Specific Guidelines (latest PDFs): `hasil.gov.my/en/e-invoice/reference-for-the-implementation-of-e-invoice/guidelines`
- MyInvois SDK portal (API reference, schemas, code tables, validation rules): `sdk.myinvois.hasil.gov.my`
- MySST portal (SST registration/filing): `mysst.customs.gov.my`
- Royal Malaysian Customs Department: `customs.gov.my`

---

## 3. Objectives

1. Every invoice, credit note, debit note, and refund note issued by the ERP that falls in scope is transmitted to MyInvois, validated, and stored with its LHDN UUID and QR code — with **zero manual re-keying** of data already in the ERP.
2. SST is calculated correctly at the line-item level, using the right rate and tax type code, and can be reported on the bi-monthly SST-02/02A returns.
3. Finance/compliance staff have full visibility into submission status, rejections, and the 72-hour cancellation/rejection window, without needing to log into the MyInvois portal directly.
4. The integration degrades gracefully — MyInvois downtime, rate limiting, or rejected documents never block the ERP's core sales/invoicing workflow; they queue and retry.
5. The design absorbs future LHDN schema/rate changes (new document type versions, new code tables, threshold changes) with configuration rather than a rebuild.

---

## 4. Scope

### In scope
- Tax (SST) configuration: tax codes, rates (with effective-dating for rate changes), tax determination rules, SST-02/02A reporting.
- e-Invoice document generation, digital signature, submission, status polling, cancellation, rejection handling, for all document types listed in §2.1.
- Consolidated e-Invoice generation for B2C.
- Self-billed invoice generation for applicable scenarios.
- Buyer/Supplier TIN validation.
- Master data extensions (customer, vendor, item, company profile) needed to populate the 55 fields.
- Digital certificate lifecycle management (procurement is external; storage/rotation/expiry alerting is in scope).
- Sandbox and production environment support with a safe cut-over process.
- Compliance and reconciliation reporting.

### Out of scope (for this phase)
- Corporate income tax (Form C / e-Filing) and other non-SST/e-Invoice tax types (e.g., withholding tax, real property gains tax).
- Payroll-related statutory contributions (EPF/SOCSO/EIS/PCB).
- Procurement of the digital certificate itself (business/legal process with a licensed CA).
- Peppol-based e-Invoicing (a separate transmission model LHDN also recognizes; only the direct API model is covered here unless later added).
- Industry-specific catalogue extensions (banking, insurance, construction) beyond the general guideline — flag as a fast-follow if the business operates in those sectors.

---

## 5. Stakeholders & Roles

| Role | Responsibility |
|---|---|
| Finance/AR team | Issues invoices, credit/debit notes; resolves rejected documents |
| Tax/Compliance officer | Owns tax code/rate configuration, SST filings, threshold monitoring, LHDN correspondence |
| IT/Integration team | Owns the integration service, certificates, credentials, monitoring |
| Sales/Order Management | Supplies transaction data (customer, items) that feeds the e-Invoice |
| External auditor | Consumes audit trail and reconciliation reports |
| LHDN / RMCD (external) | Regulator; system of record for validated e-Invoices and SST returns |

---

## 6. Glossary

| Term | Meaning |
|---|---|
| **LHDN / IRBM** | Lembaga Hasil Dalam Negeri Malaysia — Inland Revenue Board of Malaysia |
| **RMCD / JKDM** | Royal Malaysian Customs Department — administers SST |
| **MyInvois** | LHDN's e-Invoice validation platform |
| **TIN** | Tax Identification Number, issued by LHDN to every taxpayer |
| **BRN** | Business Registration Number (SSM) |
| **UBL 2.1** | Universal Business Language — the XML/JSON document standard MyInvois requires |
| **XAdES** | XML Advanced Electronic Signature — the digital signature standard used to sign documents |
| **UUID** | Unique document ID assigned by MyInvois once a document is accepted |
| **Submission UID** | Unique ID for a batch of documents submitted together |
| **MSIC code** | Malaysia Standard Industrial Classification — describes the taxpayer's business activity |
| **Classification code** | LHDN code describing the nature of goods/services on a line item |
| **Consolidated e-Invoice** | Monthly aggregated e-Invoice for B2C transactions where the buyer didn't request one at point of sale |
| **Self-billed e-Invoice** | e-Invoice issued by the buyer on the supplier's behalf in specific LHDN-defined scenarios |
| **SST-02 / SST-02A** | Bi-monthly Service Tax / Sales Tax return forms |
| **Relaxation period** | Transitional window per phase where consolidated invoicing and reduced penalties apply |

---

## 7. Functional Requirements

### 7.1 Tax (SST) Module

| ID | Requirement |
|---|---|
| TAX-01 | Maintain a **Tax Code master** (e.g., SST-SALES-10, SST-SERVICE-8, SST-SERVICE-6, EXEMPT, ZERO-RATED) mapped to LHDN's e-Invoice tax type codes (`01`, `02`, `03`, `04`, `E`, `ZRL`). |
| TAX-02 | Maintain a **Tax Rate master** with **effective-dating**, so a historical rate change (e.g., service tax 6% → 8% on 1 Mar 2024) does not corrupt past documents and future changes can be scheduled in advance. |
| TAX-03 | Support a **tax determination engine** that assigns the correct tax code to a line item based on item/service classification, customer registration status, and place of supply, with manual override at line level. |
| TAX-04 | Support both tax-inclusive and tax-exclusive pricing, with rounding rules matching RMCD guidance. |
| TAX-05 | Track the company's own SST registration status and number(s) per tax type (Sales Tax and/or Service Tax may be registered independently). |
| TAX-06 | Generate the data needed for **SST-02 (Service Tax)** and **SST-02A (Sales Tax)** bi-monthly returns from posted transactions. |
| TAX-07 | Alert the compliance officer when cumulative 12-month taxable turnover approaches an SST registration threshold (RM500k/RM1m/RM1.5m tiers) or the e-Invoice mandate threshold. |
| TAX-08 | Support exempt and zero-rated supply handling with the correct LHDN tax type code (`E`, `ZRL`) flowing through to the e-Invoice module. |

### 7.2 e-Invoice Module

| ID | Requirement |
|---|---|
| EINV-01 | Maintain a **Taxpayer/Company Profile** (TIN, BRN, SST number(s), MSIC code, registered address, contact) used as the Supplier party on every document. |
| EINV-02 | Extend **Customer and Vendor master data** with TIN, ID type (BRN/NRIC/Army/Passport/Old BRN), ID value, SST registration number (or "NA"), and address/contact fields required for the Buyer party. |
| EINV-03 | Extend the **Item/Service master** with a default classification code and unit of measure mapped to LHDN's code tables. |
| EINV-04 | Generate a UBL 2.1 **Invoice** document from a posted Sales Invoice, mapping all mandatory fields (of the 55) automatically from ERP data; flag and block submission if a mandatory field is missing, with a clear in-app remediation message. |
| EINV-05 | Generate **Credit Note, Debit Note, and Refund Note** documents referencing the original invoice's UUID, following the correct scenario (value reduction without refund vs. with refund vs. additional charge). |
| EINV-06 | Generate **Self-Billed Invoice/Credit/Debit/Refund Notes** for the LHDN-defined self-billed scenarios (e.g., payments to agents, foreign suppliers, individual acquisitions), sourced from AP transactions. |
| EINV-07 | Generate a **Consolidated e-Invoice** for eligible B2C receipts on a monthly cycle, using buyer "General Public" / TIN `EI00000000010`, **excluding** any receipt ≥ RM10,000 (which must be issued individually per EINV-04). |
| EINV-08 | **Digitally sign** every outbound document (XAdES) using the company's certificate from a licensed CA before submission. |
| EINV-09 | **Submit** documents to MyInvois via the Submit Documents API, individually or batched (≤100 docs / ≤5MB / ≤300KB per doc per the platform's limits), respecting the 100 RPM guidance. |
| EINV-10 | **Poll submission status** until each document reaches a terminal validation state (Valid/Invalid) and update the source ERP transaction accordingly; surface the LHDN UUID, long validation link, and QR code on Valid documents. |
| EINV-11 | On **Invalid**, surface the LHDN error/reason in-app, allow correction, and resubmit as a new document (never silently retry an identical payload). |
| EINV-12 | Support **Cancellation** (supplier-initiated) and record **Rejection** (buyer-initiated, if the ERP is also a receiving party) within the **72-hour window**; block cancellation attempts after the window closes and direct the user to issue a Credit/Debit/Refund Note instead. |
| EINV-13 | Provide **Buyer/Supplier TIN validation** against LHDN's Validate Taxpayer TIN API at data-entry time, so bad TINs are caught before submission rather than after rejection. |
| EINV-14 | Attach the **QR code and validation link** to the printed/PDF/emailed customer-facing invoice once validated. |
| EINV-15 | Maintain full **audit history** of every document version, submission attempt, and status transition for the statutory retention period (7 years). |
| EINV-16 | Support **sandbox and production** environments with clearly separated credentials/certificates and an explicit, permission-gated cut-over control. |
| EINV-17 | Keep local copies of LHDN **code tables** (currency, tax type, classification, MSIC, state, payment method, unit type) and refresh them periodically rather than hardcoding. |
| EINV-18 | Detect and block **duplicate submissions** of the same logical document before they reach MyInvois (LHDN also runs its own duplicate detection, but pre-empting it avoids wasted rate-limit budget). |
| EINV-19 | Support **foreign-currency invoices** with the correct exchange rate field and validation. |

---

## 8. Non-Functional Requirements

| Category | Requirement |
|---|---|
| **Compliance** | System must never allow an in-scope transaction to be finalized without either a successful MyInvois submission or an explicit, logged compliance override. |
| **Availability/Resilience** | MyInvois unavailability or throttling must not block core ERP invoicing; documents queue for submission/retry. |
| **Performance** | Respect published rate limits (100 RPM submission, 300 RPM polling per Client ID); batch where possible. |
| **Security** | Client secrets and digital certificate private keys stored in a secrets manager/HSM, never in application config or source control; access-token caching (~60 min) rather than re-authenticating per call. |
| **Data retention** | Documents, signatures, and submission logs retained a minimum of 7 years per Malaysian tax record-keeping law. |
| **Auditability** | Every status change and manual override is attributable to a user and timestamped. |
| **Localization** | UI available in English and Bahasa Malaysia; currency formatting in MYR by default. |
| **Extensibility** | New document type versions, code table updates, and rate/threshold changes must be configurable without a code deployment where feasible. |
| **Separation of environments** | Sandbox and production credentials, certificates, and data must never mix. |

---

## 9. Compliance Rule Summary (quick reference for QA/UAT)

| Rule | Detail |
|---|---|
| RM10,000 individual-invoice rule | Any single transaction ≥ RM10,000 must be an individual e-Invoice, never consolidated — no exceptions, no phase relaxation. |
| 72-hour window | Supplier may cancel, buyer may reject a Valid document within 72 hours of validation; after that, use Credit/Debit/Refund Note. |
| Consolidated e-Invoice submission | Submitted within 7 days after the end of the month being consolidated. |
| Submission limits | ≤100 documents per submission batch, ≤5 MB per submission, ≤300 KB per document. |
| Rate limits | ~100 RPM for Submit Documents; ~300 RPM for Get Submission (poll); ~12 RPM for Login. |
| Token lifetime | Access tokens expire in ~60 minutes; cache and reuse, do not re-login per request. |
| Retention | 7 years minimum for tax records including e-Invoices. |
| In-scope threshold | Currently RM3 million annual revenue/turnover, effective 1 Sept 2026 — **re-verify at build time**, this has changed twice recently. |

---

## 10. Assumptions & Constraints

- The business will procure a valid digital certificate from an MCMC-licensed CA before production go-live; this is a prerequisite, not a deliverable of this build.
- The business will complete MyInvois Portal registration and obtain Client ID/Secret for both sandbox and production before integration testing begins.
- The existing ERP's Sales Invoice, Credit Note, and AP modules expose the transactional data needed to populate the 55 fields; any gaps become master-data enrichment tasks (see `todo.md`).
- LHDN's guidelines and code tables may change; this spec reflects the framework as of September 2026 and must be diffed against the official SDK portal before each release.

## 11. Risks

| Risk | Mitigation |
|---|---|
| Threshold/phase rules change again before go-live | Build the in-scope determination as configuration, not a hardcoded date/amount. |
| Missing/incorrect buyer TIN blocks invoicing | TIN validation at data entry (EINV-13); fallback to general/foreign TINs per guideline. |
| MyInvois outage during month-end close | Queue-and-retry design (EINV-09/10); do not couple invoice posting to synchronous submission success. |
| Certificate expiry causes signature failures in production | Automated expiry monitoring job (see `todo.md` background jobs). |
| SST rate change mid-period | Effective-dated rate master (TAX-02). |

## 12. References

See §2.3. Additionally: MyInvois SDK error cookbook and validation rules at `sdk.myinvois.hasil.gov.my/document-validation-rules` for the authoritative, current list of validators (Structure, Core Fields, Signature, Taxpayer, Referenced Documents, Code, Duplicate Document, Currency).
