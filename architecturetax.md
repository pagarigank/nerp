# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module
## Technical Architecture

| | |
|---|---|
| **Document** | architecture.md |
| **Version** | 1.0 |
| **Date** | 3 September 2026 |
| **Companion docs** | `spec.md`, `frontend.md`, `todo.md` |

---

## 1. Architecture Goals & Principles

1. **Non-invasive to the existing ERP core.** The ERP's Sales/AR/AP/Inventory modules keep their current data model and workflow. Malaysia compliance is added as a **bolt-on integration service** plus targeted master-data extensions, not a rewrite.
2. **Asynchronous by default.** MyInvois is an external, rate-limited, occasionally-unavailable dependency. Posting an invoice in the ERP must never block on a synchronous call to a government API.
3. **Idempotent and replay-safe.** Every outbound call carries enough context (internal document ID, content hash) to detect and avoid duplicate submissions, matching LHDN's own duplicate-detection window.
4. **Config over code.** Tax rates, code tables, document type versions, and the in-scope threshold change by government notice, not sprint planning — they must be data, not constants.
5. **Environment isolation.** Sandbox and production MyInvois environments (different base URLs, credentials, certificates) are structurally separate; a build/environment flag decides which is active, never a runtime toggle mixed with live data.
6. **Auditable end-to-end.** Every state transition of every document is logged with who/when/what for the 7-year retention requirement.

---

## 2. High-Level Architecture

```
                         ┌───────────────────────────────────────────┐
                         │              Existing ERP Core              │
                         │  Sales / AR / AP / Inventory / Finance      │
                         │  (unchanged transactional workflow)         │
                         └───────────────┬─────────────────────────────┘
                                         │ domain events
                                         │ (InvoicePosted, CreditNoteIssued,
                                         │  APBillPosted, PaymentReceived...)
                                         ▼
                  ┌───────────────────────────────────────────────────────┐
                  │            MY Tax & e-Invoice Integration Service       │
                  │                                                         │
                  │  ┌───────────────┐   ┌─────────────────────────────┐   │
                  │  │  Tax Engine    │   │   e-Invoice Orchestrator    │   │
                  │  │  (SST calc,    │   │                             │   │
                  │  │  rate lookup)  │   │  ┌───────────────────────┐  │   │
                  │  └───────┬───────┘   │  │  Document Builder      │  │   │
                  │          │           │  │  (UBL 2.1 mapper)      │  │   │
                  │          ▼           │  └──────────┬─────────────┘  │   │
                  │  ┌───────────────┐   │             ▼                │   │
                  │  │ Master Data   │   │  ┌───────────────────────┐  │   │
                  │  │ Enrichment    │──▶│  │  Signature Service     │  │   │
                  │  │ (TIN/BRN/SST/ │   │  │  (XAdES, cert vault)   │  │   │
                  │  │  MSIC/classif)│   │  └──────────┬─────────────┘  │   │
                  │  └───────────────┘   │             ▼                │   │
                  │                      │  ┌───────────────────────┐  │   │
                  │                      │  │  Submission Outbox     │  │   │
                  │                      │  │  (queue, dedupe key)   │  │   │
                  │                      │  └──────────┬─────────────┘  │   │
                  │                      │             ▼                │   │
                  │                      │  ┌───────────────────────┐  │   │
                  │                      │  │  MyInvois API Client   │  │   │
                  │                      │  │  (auth/submit/poll/    │  │   │
                  │                      │  │   cancel/reject/TIN)   │  │   │
                  │                      │  └──────────┬─────────────┘  │   │
                  │                      └─────────────┼────────────────┘   │
                  │  ┌───────────────────────────────────────────────────┐ │
                  │  │   Status Poller / Notification Listener (worker)   │ │
                  │  └─────────────────────────────────────────────────┘ │
                  └───────────────┬───────────────────────────────────────┘
                                  │ status updates, UUID, QR, errors
                                  ▼
                    ┌───────────────────────────────┐
                    │   ERP data (updated status,    │
                    │   UUID, QR, validation link)    │
                    └───────────────────────────────┘

                                  │ HTTPS (mTLS/OAuth2)
                                  ▼
                    ┌───────────────────────────────┐
                    │      MyInvois System (LHDN)     │
                    │  sandbox: preprod-api.myinvois.  │
                    │  hasil.gov.my                    │
                    │  prod: api.myinvois.hasil.gov.my │
                    └───────────────────────────────┘
```

---

## 3. Component Breakdown

### 3.1 Tax Engine
- Owns Tax Code / Tax Rate masters (effective-dated).
- Exposes a `determineTax(lineItem, customer, placeOfSupply)` function called synchronously during invoice entry in the ERP — this is the one piece that *is* synchronous, because it affects the price shown to the customer, but it's a local computation against local master data, not a network call.
- Feeds tax type codes (`01`/`02`/`03`/`04`/`E`/`ZRL`) into the Document Builder.

### 3.2 Master Data Enrichment
- Extends Customer, Vendor, Item, and Company Profile records with the e-Invoice-specific fields (TIN, ID type/value, SST no., MSIC code, classification code, address structure LHDN expects).
- Validates TIN format and, on demand, calls the **Validate Taxpayer TIN** API; caches results with a short TTL to avoid re-validating on every keystroke.

### 3.3 e-Invoice Orchestrator
- **Document Builder**: maps an ERP transaction (Sales Invoice, Credit Note, Debit Note, Refund, AP Bill for self-billed cases, or a batch of B2C receipts for consolidation) into a UBL 2.1 JSON or XML payload matching the current document type version. Validates the 37 mandatory fields locally *before* signing — fail fast, don't waste a submission.
- **Signature Service**: applies XAdES digital signature using the company's private key, held in a certificate vault (see §7). Abstracted behind an interface so the signing library/cert vendor can be swapped without touching the Document Builder.
- **Submission Outbox**: a durable queue/table holding documents ready to submit, each keyed by an idempotency hash (source document ID + content hash) so a crash-and-retry never double-submits.
- **MyInvois API Client**: thin wrapper over the platform's REST endpoints — Login (Taxpayer/Intermediary), Submit Documents, Get Submission, Get Document, Cancel Document, Reject Document, Validate Taxpayer TIN, Search Documents, Get Document Types. Handles token caching, retries with backoff, and rate-limit headers.

### 3.4 Status Poller / Notification Listener (background worker)
- Polls `Get Submission` for outstanding submissions until every document reaches `Valid` or `Invalid` (submission is asynchronous — HTTP 202 on submit).
- On `Valid`: writes back UUID, long ID (validation link), QR payload to the ERP transaction; marks it compliant.
- On `Invalid`: writes back the error detail; raises a task for Finance to correct and resubmit as a new document.
- Watches the 72-hour cancel/reject window and raises reminders as it approaches expiry.

### 3.5 Certificate & Secret Vault
- Stores the organizational digital certificate (private key) and OAuth Client ID/Secret pairs for both sandbox and production, encrypted at rest, accessible only to the Signature Service and API Client — never exposed to the ERP UI layer or logs.
- Emits expiry warnings on a schedule (see `todo.md` background jobs).

### 3.6 Data Store Additions (owned by the integration service, referencing ERP records by ID — not duplicating ERP data)

| Entity | Purpose |
|---|---|
| `tax_code` | Code, description, LHDN tax type mapping |
| `tax_rate` | Tax code, rate %, effective_from, effective_to |
| `taxpayer_profile` | Company TIN, BRN, SST no.(s), MSIC code, address |
| `customer_tax_profile` / `vendor_tax_profile` | TIN, ID type/value, SST no., default classification |
| `einvoice_document` | Internal doc ID → ERP source ref, document type, version, payload, format, hash, status |
| `einvoice_submission` | Submission UID, batch of document refs, submitted_at, environment |
| `einvoice_status_history` | Document ref, status, timestamp, error detail, actor |
| `digital_certificate` | Cert metadata, environment, expiry, vault reference (never the raw key in this table) |
| `api_credential` | Client ID reference (secret in vault), environment |
| `code_table_cache` | Local mirror of LHDN currency/tax/classification/MSIC/state/unit/payment code tables, with `synced_at` |
| `sst_return_period` | Bi-monthly period, tax type, computed totals, filing status |

### 3.7 Existing ERP touch points (minimal, additive)
- New fields on Customer/Vendor/Item/Company screens (additive columns/tables, not schema breaks).
- A domain event or hook fired on Invoice/Credit Note/Debit Note/Refund posting, consumed by the integration service — implemented as either an in-process event, an outbox table the ERP already writes to, or a message queue publish, depending on the ERP's existing extensibility model.
- A status/UUID/QR field surfaced back onto the ERP's invoice record and print template.

---

## 4. Sequence: Individual e-Invoice Submission (happy path)

```
ERP: Invoice posted
  → Integration Service: build UBL document (Document Builder)
  → validate mandatory fields locally
  → Signature Service: sign (XAdES)
  → Submission Outbox: enqueue with idempotency key
  → API Client: Login as Taxpayer (or reuse cached token, ~60 min TTL)
  → API Client: POST /api/v1.0/documentsubmissions/  → 202 + submissionUID
  → Status Poller: GET submission status (poll, backoff) until terminal
  → on Valid: store UUID, validation link, QR; update ERP invoice status
  → ERP: print/email invoice with QR + validation link
```

## 5. Sequence: Rejected Document → Resubmission

```
Status Poller: document status = Invalid, error = <LHDN error code/message>
  → raise task to Finance/Compliance with plain-language explanation
  → user corrects source data in ERP (e.g., missing buyer TIN)
  → Document Builder regenerates a NEW document (new internal ID)
  → resubmit through the same pipeline (never blind-retry the identical rejected payload)
```

## 6. Sequence: Cancellation within 72 Hours

```
User requests cancellation (Finance UI)
  → check: now - validated_at < 72h ?
      yes → API Client: Cancel Document (reason required)
            → update status = Cancelled; audit log entry
      no  → block; instruct user to issue Credit/Debit/Refund Note instead
```

## 7. Sequence: Monthly Consolidated e-Invoice (B2C)

```
Scheduled job (month-end + up to 7 days grace):
  → query all B2C receipts for the period NOT already individually invoiced
  → split out any receipt ≥ RM10,000 → route to individual invoice flow instead
  → aggregate remainder by rules (e.g., by day or by SKU per guideline)
  → build ONE consolidated Invoice document, buyer = "General Public" / TIN EI00000000010
  → sign, submit, poll — same pipeline as §4
```

---

## 8. API Design (internal — ERP ⇄ Integration Service)

Expose a small internal REST/RPC surface so the ERP UI can be built against stable internal contracts rather than the LHDN schema directly:

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/internal/einvoice/documents` | Submit an ERP transaction for e-Invoice processing |
| `GET` | `/internal/einvoice/documents/{id}` | Get current status, UUID, QR, errors |
| `POST` | `/internal/einvoice/documents/{id}/cancel` | Cancel within window |
| `POST` | `/internal/einvoice/documents/{id}/reject` | Record buyer rejection (if applicable) |
| `POST` | `/internal/einvoice/consolidated/preview` | Preview an upcoming consolidated batch before submission |
| `POST` | `/internal/einvoice/consolidated/submit` | Trigger consolidated submission |
| `GET` | `/internal/einvoice/tin/validate` | Validate a TIN (proxied + cached) |
| `GET` | `/internal/tax/determine` | Return tax code/rate for a line item context |
| `GET` | `/internal/tax/codes`, `/internal/tax/rates` | Admin CRUD |
| `GET` | `/internal/einvoice/certificates`, `/internal/einvoice/credentials` | Admin CRUD (secrets never returned in plaintext after creation) |
| `GET` | `/internal/reports/sst`, `/internal/reports/einvoice-status` | Reporting endpoints |

---

## 9. Authentication & Security

- **MyInvois auth**: OAuth 2.0 client-credentials flow (`POST /connect/token`) using per-environment Client ID/Secret; **Login as Taxpayer** for direct filers, **Login as Intermediary** if this ERP acts as a middleware on behalf of multiple taxpayer entities. Cache the access token for its `expires_in` (~60 minutes); never log in per API call.
- **Secrets**: Client secrets and certificate private keys live in a secrets manager/HSM (e.g., cloud KMS, vault service), encrypted at rest, with access scoped to the Integration Service's runtime identity only.
- **Transport**: TLS to MyInvois; internal service-to-service calls behind the ERP's existing auth boundary.
- **Least privilege**: the integration service account in the ERP only needs read access to source transactions and write access to the status/UUID/QR fields it owns — not general ERP data access.
- **PII handling**: buyer NRIC/passport numbers are personal data; apply the ERP's existing data-protection controls (masking in UI logs, access control on customer master).

## 10. Environments

| | Portal | API base |
|---|---|---|
| Production | `myinvois.hasil.gov.my` | `api.myinvois.hasil.gov.my` |
| Sandbox | `preprod.myinvois.hasil.gov.my` | `preprod-api.myinvois.hasil.gov.my` |

Sandbox has lower rate limits and limited data retention (~3 months) — do not treat it as a long-term UAT data store; plan UAT cycles accordingly. Environment selection is a deployment-time configuration, backed by entirely separate credential and certificate records — never a per-request parameter a user could accidentally flip.

## 11. Error Handling & Retry Strategy

| Failure | Handling |
|---|---|
| HTTP 4xx (bad structure, incorrect submitter) | Do not retry automatically; surface to a human — the payload is wrong, not the network. |
| HTTP 422 Duplicate Submission | Respect `Retry-After` header; do not resubmit blindly (LHDN dedupes on payload hash within a 10-minute window). |
| HTTP 429 / rate limit | Exponential backoff with jitter; respect standard headers; batch future submissions more aggressively. |
| Network/5xx | Retry with backoff, capped attempts, then move to a dead-letter state for manual review. |
| Signature errors (`DS3xx` range) | Treat as configuration/certificate issues, not transient — alert IT/Integration immediately, do not retry until resolved. |
| Validation errors on submitted document | Terminal for that document version; requires a corrected resubmission, not a retry. |

All outbox items carry an attempt counter and last-error field; a dead-letter queue with alerting prevents silent pile-ups.

## 12. Rate Limiting & Batching

- **Submit Documents**: recommended ≤100 requests/minute per Client ID; batch up to 100 documents per submission (≤5MB total, ≤300KB/document) — the Status Poller and Outbox should prefer batching during high-volume periods (e.g., month-end).
- **Get Submission (poll)**: ~300 RPM; poll with backoff rather than tight-looping, and prefer polling `Get Submission` over any "recent documents" style endpoint.
- **Login**: ~12 RPM — irrelevant in practice once token caching is implemented correctly.
- Minify JSON/XML payloads before submission to reduce size and avoid line-count validation issues.

## 13. Observability

- **Structured logs** per document: internal ID, submission UID, MyInvois UUID, status transitions, timestamps, error codes — no raw certificate/secret material ever logged.
- **Metrics**: submission success rate, average time-to-Valid, rejection rate by error code, queue depth, token refresh failures, certificate days-to-expiry.
- **Alerts**: dead-letter queue depth > threshold; certificate expiry < 30 days; consolidated batch job failure; sustained 429/5xx rate.
- **Runbooks**: what to do on a DS3xx signature error, on a sandbox 403, on a stuck submission — captured for the operations team (see `todo.md` for the job that produces these).

## 14. Deployment Topology

- Integration Service deployable as a separate application/service (container) alongside the ERP, communicating over an internal network — this keeps the compliance-critical, frequently-changing MyInvois integration decoupled from the ERP's release cycle.
- Background workers (Status Poller, consolidated batch job, certificate monitor) run as scheduled jobs/queue consumers, horizontally scalable independent of the API-facing part.
- Blue/green or canary deployment recommended for the Integration Service given the compliance blast radius of a bad release.

## 15. Technology Stack (recommendation, adapt to existing ERP stack)

- **Language/runtime**: match the existing ERP's stack where possible to ease maintenance (e.g., if ERP is .NET/Java/Node/PHP, keep the integration service in the same ecosystem); several community SDKs exist per language (PHP: `klsheng/myinvois-php-sdk`; Node.js: `syukranDev/e-invoice-sdk-nodejs`; Python/Frappe: `ERPGulf/myinvois`; C#/.NET: `mokth/einvoice`) as reference implementations for UBL mapping and signing — evaluate as accelerators, not black boxes, since compliance correctness is the team's responsibility either way.
- **Queue**: any durable queue/outbox pattern already available in the stack (DB-backed outbox is sufficient at typical SME/mid-market volumes).
- **Secrets**: cloud KMS / HashiCorp Vault / equivalent already used by the ERP's infra.
- **Scheduler**: existing ERP's job scheduler or a lightweight cron/worker framework.

## 16. Build vs. Buy Note

Given the compliance stakes and the pace of LHDN schema/guideline changes, evaluate a **licensed middleware/gateway product** (several MSC Trustgate/PosDigicert-adjacent vendors, and open community SDKs) for the low-level UBL/XAdES/API plumbing, and keep the **Tax Engine, Document Builder business logic, and ERP integration** in-house — this balances compliance risk against long-term maintenance cost. This decision should be made explicitly before Phase 2 of `todo.md`.

## 17. References

Same as `spec.md` §2.3 and §12, plus: `sdk.myinvois.hasil.gov.my/integration-practices` (official integration recommended practices) and `sdk.myinvois.hasil.gov.my/signature` (XAdES signature spec).
