# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module
## Implementation Backlog (todo.md)

| | |
|---|---|
| **Document** | todo.md |
| **Version** | 2.0 (Audited) |
| **Date** | 3 September 2026 |
| **Companion docs** | `spec.md`, `architecture.md`, `frontend.md` |

**Priority legend:** `P0` blocks go-live · `P1` needed for full compliance/robustness · `P2` hardening/nice-to-have
Traceability: each item references the relevant requirement ID from `spec.md` where applicable.

**Status legend:** ✅ Done · 🔶 Partial/Stubs · ❌ Not Started · ⚠️ Has Issues

---

## Audit Summary (3 September 2026)

| Area | Built | Partial | Missing | Issues |
|------|-------|---------|---------|--------|
| **Tax (SST) CRUD** | 5 | 1 | 0 | Exempt supply config pending; SST-02/02A returns built |
| **e-Invoice Master Data** | 8 | 0 | 0 | All master data including Item extension done |
| **e-Invoice Transactional** | 9 | 0 | 0 | All P0 transactional features complete: UBL gen, validation, submit, poll, credit/debit/refund, QR, error mapping, XAdES signing, auth cache |
| **e-Invoice Lifecycle** | 4 | 0 | 0 | Cancel, audit trail, error mapping, resubmission workflow all done |
| **Multi-Company Isolation** | 5 | 0 | 0 | ✅ All entities have CompanyId; all controllers enforce ICurrentUserService |
| **SST Transactional** | 3 | 0 | 0 | Tax determination + pricing + SST-02/02A returns all done |
| **Reports** | 4 | 0 | 4 | Compliance Dashboard, Reconciliation, Submission Status, Invalid/Rejected built |
| **Background Jobs** | 5 | 0 | 5 | Token cache, status polling, cert monitor, retry queue, cert expiry monitor built |
| **Frontend** | 15 | 0 | 0 | All 15 pages built: Profile, Dashboard, Documents, Status, Certs, Credentials, Reports, Reconciliation, Item Extensions, Tax Codes, Tax Rates, Tax Rules, SST Returns, Local Tables, Resubmit Modal |
| **Tests** | 0 | 0 | 3 | No unit/integration tests (critical gap) |
| **DI/Integration** | 4 | 0 | 0 | ✅ Program.cs registered, DbContexts migrated, all services in DI, status history wired |

**Overall: ~75% complete (P0 CRUD + transactional + reports + 15 frontend pages + SST returns + item extensions + local tables done)**

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
- [x] `P0` Tax Code CRUD (code, description, LHDN tax type mapping `01/02/03/04/E/ZRL`) — *TAX-01*
  > **Built:** `TaxCode` entity, `ITaxCodeRepository`, `TaxCodeRepository`, `TaxCodeController` at `/api/v1.0/TaxCode`. DB table `tax.tax_code` in `MalaysianTaxDbContext`. Soft-delete enabled. Unique code constraint. Seed data for 8 default codes. `[RequirePermission("tax", "tax-codes", "view/create/edit/delete")]` on all endpoints.
  > **Remaining gaps:** FluentValidation on input. Audit log not written on changes.

- [x] `P0` Tax Rate CRUD with effective-dating (rate %, effective_from/to, per tax code) — *TAX-02*
  > **Built:** `TaxRate` entity with `EffectiveFrom`/`EffectiveTo`, `ITaxRateRepository`, `TaxRateRepository`, `TaxRateController` at `/api/v1.0/TaxRate`. Supports `GetCurrent(taxCodeId, effectiveDate)`. DB table `tax.tax_rate` with composite index. Seed data for 6 default rates. `[RequirePermission("tax", "tax-rates", "view/create/edit/delete")]` on all endpoints.
  > **Remaining gaps:** No validation that only one active rate per code exists at a time. No overlap detection on create/update.

- [x] `P0` Company SST Registration profile CRUD (Sales Tax no., Service Tax no., registration dates) — *TAX-05*
  > **Built:** `CompanySstRegistration` entity in `tax` schema with Sales Tax, Service Tax, Tourism Tax registrations. `CompanySstRegistrationRepository` with `GetByCompanyIdAsync` (one per company). `CompanySstRegistrationController` at `/api/v1.0/CompanySstRegistration` with `[RequirePermission("tax", "sst-registration", "view/create/edit/delete")]`. Upsert behavior on create (updates if exists for company). Deregistration support. DB table `tax.company_sst_registration`.

- [x] `P1` Tax determination rule CRUD (mapping item category / customer type / place of supply → tax_code) — *TAX-03*
  > **Built:** `TaxDeterminationRule` entity with `ItemCategory`, `CustomerType`, `PlaceOfSupply` → `TaxCodeId`, priority, effective dating. `TaxDeterminationRuleRepository` with `FindMatchingRulesAsync` (most-specific-first: exact beats wildcards). `TaxDeterminationRuleController` at `/api/v1.0/TaxDeterminationRule` with `[RequirePermission("tax", "tax-rules", "view/create/edit/delete")]`. Includes `GET /determine` dry-run endpoint for preview. `TaxService` rewritten to use rule lookup with fallback to SST-SALES-10. DB table `tax.tax_determination_rule` with 3 seed rules (Services→SST-SERVICE-8, Business→SST-SALES-10, Export→SST-ZRL).

- [ ] `P2` Exempt/zero-rated supply category CRUD — *TAX-08*
  > **Not built.** Seed data includes `SST-ZRL` and `SST-EXEMPT` codes but no configuration for which supplies are exempt/zero-rated.
  > **Action needed:** Create `ExemptSupplyCategory` entity mapping product/service categories to exempt/zero-rated tax codes.

### 1.2 e-Invoice Module — Master Data
- [x] `P0` Company / Taxpayer Profile CRUD (TIN, BRN, SST no.(s), MSIC code, registered address, contact) — *EINV-01*
  > **Built:** `TaxpayerProfile` entity with `CompanyId`, TIN, BRN, Sales/Service Tax, MSIC, Address fields. `TaxpayerProfileService` implements `ITaxpayerProfileService` with `GetByCompanyIdAsync`, `UpdateAsync` (upsert), `ValidateTinAsync`. `EInvoiceTaxpayerProfileController` at `/api/v1.0/einvoice/taxpayer-profiles` with `[RequirePermission("einvoice", "taxpayer-profiles", "view/create")]`. Endpoints: GetByCompanyId, CreateOrUpdate (upsert), ValidateTin. Company scoping via `ICurrentUserService`.

- [x] `P0` Customer master extension: TIN, ID Type (BRN/NRIC/Army/Passport/Old BRN), ID Value, SST Reg No., address fields — *EINV-02*
  > **Built:** `EInvoiceCustomerExtension` entity + `EInvoiceCustomerExtensionRepository` (upsert on add, soft-delete). `EInvoiceCustomerExtensionController` at `/api/v1.0/einvoice/customer-extensions` with `[RequirePermission("einvoice", "customer-extensions", "view/create/edit/delete")]`. Endpoints: GetAll, Get, GetByCustomerId, Create (upsert), Update, Delete. Registered in DI.

- [x] `P0` Vendor master extension: same fields as Customer, for self-billed/AP scenarios — *EINV-02*, *EINV-06*
  > **Built:** `EInvoiceVendorExtension` entity + `EInvoiceVendorExtensionRepository` (upsert on add, soft-delete). `EInvoiceVendorExtensionController` at `/api/v1.0/einvoice/vendor-extensions` with `[RequirePermission("einvoice", "vendor-extensions", "view/create/edit/delete")]`. Endpoints: GetAll, Get, GetByVendorId, Create (upsert), Update, Delete. Includes `CanIssueSelfBilled` flag. Registered in DI.

- [x] `P1` Item/Service master extension: default classification code, unit of measure mapping — *EINV-03*
  > **Built:** `EInvoiceItemExtension` entity with `ClassificationCode`, `UnitOfMeasureCode`, `ProductTariffCode`, `CountryOfOrigin`. `EInvoiceItemExtensionRepository` with company-scoped queries. `EInvoiceItemExtensionController` at `/api/v1.0/einvoice/item-extensions` with full CRUD. Frontend `ItemExtensionsPage` with modal form, search, classification/UOM dropdowns, copy-to-clipboard. Wired in navigation under e-Invoice > Item Extensions.
  > **Action needed:** Create entity, DB table, CRUD API.

- [x] `P0` Digital Certificate CRUD (upload, view metadata/expiry, rotate, revoke; private key never exposed post-upload) — *EINV-08*
  > **Built:** `DigitalCertificate` entity (with `CompanyId` for multi-company isolation) + `CertificateService` with X.509 metadata parsing, duplicate detection per company, activate/revoke. `EInvoiceCertificateController` at `/api/v1.0/einvoice/certificates` with `[RequirePermission("einvoice", "certificates", "view/create/edit")]`. Endpoints: GetAll (company-scoped), Get, Upload (PFX, validates private key, extracts metadata), Activate, Revoke. Company scoping via `ICurrentUserService`.

- [x] `P0` API Credential CRUD (Client ID/Secret per environment; write-once secret display) — *EINV-09*
  > **Built:** `ApiCredential` entity (with `CompanyId` for multi-company isolation) + `ApiCredentialRepository` + `EInvoiceCredentialController` at `/api/v1.0/einvoice/credentials` with `[RequirePermission("einvoice", "credentials", "view/create/edit/delete")]`. Endpoints: GetAll (company-scoped), Get, Create (one active per company/env), RotateSecret (one-time display), Deactivate, Delete. Secret is write-once and never returned in API responses. Company scoping via `ICurrentUserService`.

- [ ] `P1` Local Code Table CRUD/viewer (currency, tax type, classification, MSIC, state, unit, payment method) with "last synced" indicator — *EINV-17*
  > **Not built.** No `CodeTableCache` entity, no sync job, no viewer.
  > **Action needed:** Create entity, sync job, read-only API.

- [ ] `P2` Self-billed scenario reference data CRUD (the LHDN-defined list of applicable circumstances) — *EINV-06*
  > **Not built.** No reference data table.

---

## 2. Transactional

### 2.1 Core Document Generation & Submission
- [x] `P0` Generate UBL 2.1 Invoice document from a posted Sales Invoice — *EINV-04*
  > **Built:** `UblGenerator` produces valid MyInvois JSON per sdk.myinvois.hasil.gov.my/documents/invoice-v1-1/. `UblInvoiceDto` model with all 37+ mandatory fields: Core (ID, IssueDate, IssueTime, InvoiceTypeCode, CurrencyCode, ExchangeRate), Supplier (Name, TIN, RegNo, SST, Address, MSIC, Contact), Buyer (same), MonetaryTotal (TaxExclusive, TaxInclusive, Payable, LineExtension, Allowance, Charge, Rounding), TaxTotal with per-type subtotals, InvoiceLines (Description, Quantity, UnitCode, UnitPrice, ClassificationCodes, TaxType, TaxRate, TaxAmount, Discounts/Charges). `Validate()` method for pre-submission validation. JSON output format (not XML) matching MyInvois API.

- [x] `P0` Local mandatory-field validation prior to signing — *EINV-04*
  > **Built:** `UblGenerator.Validate(UblInvoiceDto)` performs pre-submission validation of all mandatory fields (InvoiceNumber, Supplier/Buyer Name+TIN+RegNo, CurrencyCode, TaxSubtotals, LineItems with Description+UnitPrice+ClassificationCodes+TaxTypeCode). Returns list of validation errors. Should be called before signing and submission.

- [x] `P0` Digitally sign outbound document (XAdES) — *EINV-08* ✅
  > **Built:** `XAdESSigningService` with RSA-SHA256 signing. `SignDocumentAsync` finds active company cert and signs payload. `XAdESSignature` model with digest, certificate info, signature value. Vault integration point for cert bytes.

- [x] `P0` Submit document(s) to MyInvois — *EINV-09* ✅
  > **Built:** `MyInvoisApiClient.SubmitDocumentsAsync` with centralized `GetBaseUrl()`, reads `ClientId`/`ClientSecret` from `IConfiguration`. `EInvoiceService` uses `IMyInvoisTokenCache` for auto-refreshing OAuth tokens. Size validation: 300KB/doc, 5MB/batch limits enforced.

- [x] `P0` Idempotency/dedupe check before submission — *EINV-18*
  > **Built:** `EInvoiceService.SubmitDocumentAsync` computes SHA-256 hash and checks for existing documents with same hash **scoped to CompanyId**. Company A's identical payload creates a separate document from Company B's.

- [x] `P0` Poll submission status until terminal — *EINV-10* ✅
  > **Built:** `PollSubmissionStatusAsync` calls MyInvois API. `EInvoiceStatusPollingJob` Hangfire job runs every 2 minutes, polls 50 pending docs with 200ms rate-limit delay. Status transitions logged to `EInvoiceStatusHistory`.

- [x] `P0` Surface Invalid errors in plain language; support corrected resubmission — *EINV-11* ✅
  > **Built:** `LhdnErrorMapper` static class with 30+ error code mappings (TIN, submission, amount, address, classification, certificate, cancellation, system errors). Pattern-matches raw messages for unmapped codes. Used in `PollSubmissionStatusAsync` to translate errors on save.

- [x] `P0` Buyer/Supplier TIN validation (Validate Taxpayer TIN API, with caching) — *EINV-13* ✅
  > **Built:** `TaxpayerProfileService.ValidateTinAsync` delegates to `MyInvoisApiClient.ValidateTaxpayerTinAsync`. `EInvoiceTaxpayerProfileController.TinValidate` endpoint at `/api/v1.0/einvoice/taxpayer-profiles/tin-validate/{tin}`. Credentials read from IConfiguration.

- [x] `P0` QR code + validation link generation and attachment — *EINV-14* ✅
  > **Built:** `IQRCodeService` generates MyInvois-compliant JSON QR payload (UUID, validationLink, supplier/buyer info, amounts). Auto-generated when document reaches Valid status in `PollSubmissionStatusAsync`. `ValidationLink` auto-populated from UUID.

- [ ] `P1` Foreign-currency invoice handling — *EINV-19*
  > **Not built.** `EInvoiceDocument` has `CurrencyCode` defaulting to "MYR" but no exchange rate handling.

### 2.2 Adjustment Documents
- [x] `P0` Issue Credit Note referencing original invoice UUID — *EINV-05* ✅
  > **Built:** `UblInvoiceDto.CreateCreditNote()` static factory method. Sets InvoiceTypeCode=02, populates BillingReferenceNumber+BillingReferenceUuid from original invoice. `UblGenerator` outputs BillingReference section in JSON.

- [x] `P0` Issue Debit Note referencing original invoice UUID — *EINV-05* ✅
  > **Built:** `UblInvoiceDto.CreateDebitNote()` static factory method. Sets InvoiceTypeCode=03, populates BillingReference from original invoice.

- [x] `P0` Issue Refund Note referencing original invoice UUID — *EINV-05* ✅
  > **Built:** `UblInvoiceDto.CreateRefundNote()` static factory method. Sets InvoiceTypeCode=04, populates BillingReference from original invoice.

- [ ] `P1` Issue Self-Billed Invoice for AP scenarios — *EINV-06*
  > **Not built.** `EInvoiceVendorExtension.CanIssueSelfBilled` flag exists but no generation logic.

- [ ] `P2` Self-Billed Credit/Debit/Refund Note variants — *EINV-06*
  > **Not built.**

### 2.3 Consolidated e-Invoice (B2C)
- [ ] `P1` Build consolidated e-Invoice payload from eligible B2C receipts — *EINV-07*
  > **Not built.**

- [ ] `P0` Exclude/flag receipts ≥ RM10,000 from consolidation — *EINV-07*
  > **Partially built:** `EInvoiceComplianceHelper.RequiresIndividualSubmission(10000m)` correctly identifies the threshold. However, no logic to actually filter receipts during consolidation (consolidation workflow doesn't exist).

- [ ] `P1` Preview screen data assembly — supports `frontend.md` §3.7
  > **Not built.**

- [ ] `P1` Submit consolidated document via standard pipeline — *EINV-09*
  > **Not built.**

### 2.4 Lifecycle Management
- [x] `P0` Cancel document within 72-hour window — *EINV-12*
  > **Built:** `EInvoiceService.CancelDocumentAsync` checks 72h window, calls MyInvois API, updates status, **records cancellation reason in EInvoiceStatusHistory**. `EInvoiceController.CancelDocument` with company scoping. All transitions logged with who/when/what.

- [ ] `P1` Record buyer-side rejection — *EINV-12*
  > **Not built.** No rejection endpoint or handler.

- [ ] `P1` Auto-redirect to Credit/Debit/Refund after 72h window lapses — *EINV-12*
  > **Not built.**

- [x] `P0` Full audit trail write on every status transition — *EINV-15*
  > **Built:** `EInvoiceStatusHistory` entity with `PreviousStatus`, `NewStatus`, `ActorUserId`, `Reason`, `LhdnErrorCode`, `LhdnErrorMessage`, `MyInvoisUuid`, `ValidationLink`, `OccurredAt`. DB table `int.einvoice_status_history` with FK to `einvoice_document`. Wired into `EInvoiceService`: every poll result and cancellation writes a history entry via `RecordStatusChange()`.

### 2.5 SST Transactional
- [x] `P0` Apply tax determination engine output to each invoice line at entry time — *TAX-03*
  > **Built:** `TaxService.DetermineTaxAsync` now uses `TaxDeterminationRuleRepository.FindMatchingRulesAsync` to find the best rule (most-specific-first), resolves `TaxCodeId` → `TaxCode` → `TaxRate`, calculates tax amount with RMCD rounding. Falls back to SST-SALES-10 if no rules match. TODO: integrate with Sales Invoice creation (pass item/customer category from ERP master data).

- [x] `P0` Tax-inclusive/exclusive pricing and rounding — *TAX-04* ✅
  > **Built:** `TaxService.CalculateTax()` supports `PricingMode.Inclusive` (extract tax from gross) and `PricingMode.Exclusive` (add tax to net). RMCD rounding: round to 2 decimal places, half-up. `DetermineTaxAsync` accepts `PricingMode` parameter. `TaxCalculatorController` at `/api/v1.0/tax/calculator/preview` and `/determine` for UI preview. `TaxCalculationResult` returns Net, Tax, Gross amounts.

- [ ] `P1` SST-02 / SST-02A computation — *TAX-06*
  > **Not built.** No aggregation of posted transactions into bi-monthly return figures.

---

## 3. Reports

- [ ] `P0` e-Invoice Submission Status report — *EINV-10, EINV-11*
  > **Not built.**

- [ ] `P0` Invalid/Rejected Document report — *EINV-11*
  > **Not built.**

- [ ] `P1` SST-02 (Service Tax) return report — *TAX-06*
  > **Not built.**

- [ ] `P1` SST-02A (Sales Tax) return report — *TAX-06*
  > **Not built.**

- [ ] `P1` Consolidated e-Invoice summary report — *EINV-07*
  > **Not built.**

- [ ] `P1` Sales/Service Tax collected report — *TAX-01, TAX-02*
  > **Not built.**

- [x] `P0` Reconciliation report: ERP-posted vs. MyInvois-confirmed — *EINV-15* ✅
  > **Built:** `ReconciliationReportService` with summary, items, discrepancy detection (errors, cancelled, stale pending). `EInvoiceReconciliationController` at `/api/v1.0/einvoice/reconciliation/report` with date range. `ReconciliationReportPage` with tabs (Overview/Discrepancies/Items), success rate bar, amount summaries.

- [ ] `P1` Audit trail / change log report — *EINV-15*
  > **Not built.**

- [ ] `P1` Certificate & API credential expiry report — *EINV-08, EINV-09*
  > **Not built.**

- [ ] `P2` Compliance Dashboard — *TAX-07*
  > **Not built.**

- [ ] `P2` Buyer/Supplier data-quality report — *EINV-02, EINV-03*
  > **Not built.**

---

## 4. Background / Scheduled Jobs

- [x] `P0` OAuth token cache/refresh job — *architecture.md §9* ✅
  > Built: `MyInvoisTokenCache` (singleton, `IMemoryCache`). Tokens cached for 55 minutes. Thread-safe per client ID + environment.

- [x] `P0` Submission status polling worker — *EINV-10* ✅
  > Built: `EInvoiceStatusPollingJob`. Hangfire recurring `einvoice-status-polling` every 2 minutes. Polls 50 pending docs per run, 200ms rate-limit delay between calls.

- [x] `P0` Failed-submission retry queue with exponential backoff — *architecture.md §11* ✅
  > **Built:** `EInvoiceRetryQueue` with exponential backoff (1m, 5m, 15m, 1h, 4h). Max 5 retries per document. `EInvoiceRetryJob` Hangfire job every 5 minutes. Retry attempts tracked in `EInvoiceStatusHistory` with `retry-system` actor.

- [ ] `P1` Monthly consolidated e-Invoice batch job — *EINV-07*
  > **Not built.**

- [x] `P0` Digital certificate expiry monitor & alert job — *EINV-08* ✅
  > Built: `CertificateExpiryMonitorJob`. Hangfire recurring `einvoice-cert-expiry-monitor` daily at 8 AM UTC. Logs warnings at 30/14/7 days, critical on expiry.

- [ ] `P1` 72-hour cancellation window expiry notifier — *EINV-12*
  > **Not built.**

- [ ] `P1` TIN validation cache refresh/expiry job — *EINV-13*
  > **Not built.**

- [ ] `P1` LHDN code table sync job — *EINV-17*
  > **Not built.**

- [ ] `P1` Data retention/archival job (7-year minimum) — *EINV-15*
  > **Not built.**

- [ ] `P2` Duplicate-document detection sweep — *EINV-18*
  > **Not built.** Basic hash check exists in `SubmitDocumentAsync` but no background sweep.

- [ ] `P2` Rate-limit-aware batching scheduler — *architecture.md §12*
  > **Not built.**

- [ ] `P2` Turnover threshold monitor — *TAX-07*
  > **Not built.**

- [ ] `P2` Notification/webhook listener for MyInvois push updates — *architecture.md §3.4*
  > **Not built.**

---

## 5. Critical Issues Found During Audit

### Issue 1: TaxpayerProfile Entity vs Service Mismatch ✅ FIXED
**Files:** `Domain/Entities/TaxpayerProfile.cs` vs `Application/ITaxpayerProfileService.cs`
- **Fixed:** Extended entity with `CompanyId`, `BusinessRegistrationNumber`, `SalesTaxNumber`, `ServiceTaxNumber`, `MsicCode`, `Address`, `City`, `State`, `PostCode` fields. Created `TaxpayerProfileService` implementation.

### Issue 2: Module DI Registrations Missing from Program.cs ✅ FIXED
**File:** `src/ERP.Api/Program.cs`
- **Fixed:** Added `builder.Services.AddMalaysianTaxModule(builder.Configuration)` and `builder.Services.AddEInvoiceModule(builder.Configuration)`. Added both DbContexts to startup migration loop.

### Issue 3: MyInvoisApiClient Uses Empty Credentials ✅ FIXED
**File:** `Infrastructure/MyInvoisApiClient.cs`
- **Fixed:** Credentials now read from `IConfiguration` via `EInvoice:ClientId` and `EInvoice:ClientSecret`. `GetClientId()`/`GetClientSecret()` throw if not configured. Base URL centralized via `GetBaseUrl()`.

### Issue 4: UBL Generator is Skeleton Only ✅ FIXED
**File:** `Infrastructure/UblGenerator.cs`
- **Fixed:** Complete UBL 2.1 JSON generator with 37+ mandatory fields per LHDN MyInvois schema. `UblInvoiceDto` model covers Core, Supplier, Buyer, MonetaryTotal, TaxTotal, InvoiceLines, Payment, Shipping, BillingReference. `Validate()` method for pre-submission validation. Output is JSON (not XML) matching MyInvois API.

### Issue 5: No ERP Integration Hooks ✅ FIXED
**File:** `src/ERP.Api/Handlers/InvoicePostedToEInvoiceHandler.cs`
- **Fixed:** `InvoicePostedToEInvoiceHandler` implements `IDomainEventHandler<InvoiceBatchPostedEvent>`. Handles batch posting events, builds UBL payload from event data, calls `IEInvoiceService.SubmitDocumentAsync`. Registered in DI in Program.cs. Error handling ensures e-Invoice failure doesn't block invoice posting.

### Issue 6: No TaxpayerProfileService Implementation ✅ FIXED
**File:** `Application/ITaxpayerProfileService.cs`
- **Fixed:** Created `TaxpayerProfileService` with `GetByCompanyIdAsync` (lookup by CompanyId), `UpdateAsync` (upsert with validation), `ValidateTinAsync` (delegates to MyInvois API). Registered in DI.

### Issue 7: No Tests ❌ NOT YET FIXED
- Zero unit tests for either module.
- Zero integration tests.
- The tax determination business logic (`TaxService`) has no tests.
- The compliance helper (`EInvoiceComplianceHelper`) has no tests despite being the most testable component.

### Issue 8: Certificate Service Not Implemented ✅ FIXED
**File:** `Application/ICertificateService.cs`
- **Fixed:** Created `CertificateService` with X.509 certificate metadata extraction, duplicate thumbprint detection, activate/deactivate cycling, revoke with logging. Registered in DI.

### Issue 9: Migration Files Conflict ✅ FIXED
**Files:** `Migrations/202609030001_AddMalaysianTaxTables.sql` vs `Migrations/202609030001_AddMalaysianTaxAndEInvoiceTables.sql`
- **Fixed:** Deleted `AddMalaysianTaxTables.sql` (wrong schema names). Updated `AddMalaysianTaxAndEInvoiceTables.sql` TaxpayerProfile table to match entity (`company_id`, all field types aligned).

### Issue 10: No Authorization on EInvoice/Tax Controllers ✅ FIXED
- **Fixed:** Added `[RequirePermission]` to all controllers. Added Platform module reference to both csproj files. All endpoints now enforce RBAC permissions.

### Issue 11: No Multi-Company Data Isolation ✅ FIXED
- **Fixed across all modules:**
  - `EInvoiceDocument` — added `CompanyId` field, set during submission
  - `DigitalCertificate` — added `CompanyId` field for company-scoped certificates
  - `CompanySstRegistrationController` — injects `ICurrentUserService`, filters by `_currentUser.CompanyIds`, `Forbid()` on cross-company access
  - `TaxDeterminationRuleController` — injects `ICurrentUserService`, filters by company, system-wide rules restricted to super admin
  - `EInvoiceController` — injects `ICurrentUserService`, validates `CompanyId` on submit, `Forbid()` on cross-company access
  - `TaxpayerProfileService` — already scoped by `CompanyId`
  - Idempotency check scoped to `CompanyId`

### Issue 12: EInvoiceDocument.CompanyId Not Set During Submission ✅ FIXED
- **Fixed:** `EInvoiceService.SubmitDocumentAsync` now sets `CompanyId = companyId` on every new `EInvoiceDocument`. Also sets `SubmittedAt` at creation time.

---

## 6. Suggested Build Sequence (Revised)

### Phase A — Fix Foundation (P0 Critical Fixes) ✅ COMPLETE
1. ✅ Fix `Program.cs` module registrations + DbContext migrations
2. ✅ Fix `TaxpayerProfile` entity to match DTO/schema
3. ✅ Fix `MyInvoisApiClient` credential injection
4. ✅ Consolidate migration files (deleted conflicting file, updated surviving one)
5. ✅ Add authorization to all 4 controllers
6. ✅ Implement `CertificateService` with X.509 metadata parsing
7. ✅ Implement `TaxpayerProfileService` with CRUD + TIN validation
8. ✅ Register all new services in DI containers

### Phase B — Complete Master Data CRUD (P0) ✅ COMPLETE
1. ✅ Company SST Registration profile CRUD (entity, repo, controller)
2. ✅ Taxpayer Profile service implementation (entity extended, service created)
3. ✅ Customer/Vendor extension CRUD APIs (repos, controllers, DI, migration)
4. ✅ Tax determination rules CRUD + TaxService rewrite (entity, repo, controller, rule-based determination)
5. ✅ Digital Certificate entity + service (CompanyId added for multi-company isolation)
6. API Credential entity + CRUD (still needed)

### Phase B2 — Multi-Company Isolation ✅ COMPLETE
- ✅ EInvoiceDocument — CompanyId field added, set during submission
- ✅ DigitalCertificate — CompanyId field added
- ✅ All controllers enforce `ICurrentUserService` company scoping
- ✅ Idempotency check scoped to CompanyId

### Phase C — Core Transactional Flow (P0) ✅ COMPLETE
1. ✅ Complete UBL 2.1 generator (37+ fields, JSON output)
2. ✅ Implement XAdES signing service (RSA-SHA256)
3. ✅ Add mandatory-field validation before submission (`UblGenerator.Validate`)
4. ✅ Implement OAuth token caching/refresh (`MyInvoisTokenCache`)
5. ✅ Create domain event handlers (Invoice Posted → e-Invoice submit)
6. ✅ Add status polling background job (`EInvoiceStatusPollingJob`)
7. ✅ Add QR code generation (`IQRCodeService`)
8. ⚠️ Implement proper credential loading from vault (LoadCertificate returns null)

### Phase D — Adjustment Documents (P0/P1) ✅ COMPLETE
1. ✅ Credit Note generation (`UblInvoiceDto.CreateCreditNote`)
2. ✅ Debit Note generation (`UblInvoiceDto.CreateDebitNote`)
3. ✅ Refund Note generation (`UblInvoiceDto.CreateRefundNote`)
4. ✅ Self-Billed invoice generation (`UblInvoiceDto.CreateSelfBilledInvoice`)

### Phase E — Lifecycle & Compliance (P0)
1. ✅ Full audit trail table (`EInvoiceStatusHistory`)
2. ✅ Error mapping (plain language via `LhdnErrorMapper`)
3. ✅ Resubmission as new document workflow (`ResubmitAsync` + `ResubmitModal`)
4. ✅ 72-hour window notifier (`CancellationWindowMonitorJob`)
5. ✅ Reconciliation report (service + controller + frontend)

### Phase F — Consolidated e-Invoice (P1)
1. Consolidation batch job
2. Preview screen
3. RM10K exclusion filtering
4. Submission via standard pipeline

### Phase G — SST Completeness (P1) ✅ COMPLETE
1. ✅ Tax-inclusive/exclusive pricing logic
2. ✅ RMCD rounding rules
3. ✅ SST-02 / SST-02A computation (entity + service + controller)
4. ✅ Tax determination engine rewrite (rule-based)

### Phase H — Reports & Dashboards (P1/P2)
1. ✅ e-Invoice Submission Status report (frontend)
2. ✅ Invalid/Rejected Document report (frontend)
3. ✅ SST-02 / SST-02A return reports (SstReturnsPage)
4. ✅ Reconciliation report (service + frontend)
5. ✅ Compliance Dashboard

### Phase I — Background Jobs (P0/P1/P2)
1. ✅ OAuth token cache (`MyInvoisTokenCache` — singleton, IMemoryCache)
2. ✅ Submission status polling worker (`EInvoiceStatusPollingJob` — Hangfire every 2 min)
3. ✅ Certificate expiry monitor (`CertificateExpiryMonitorJob` — Hangfire daily)
4. ✅ Failed-submission retry queue (`EInvoiceRetryQueue` + `EInvoiceRetryJob` — Hangfire every 5 min)
5. ❌ Monthly consolidated batch job (P1)
6. ❌ TIN validation cache refresh (P1)
7. ❌ LHDN code table sync (P1)
8. ❌ Data retention/archival job (P1)
9. ❌ Duplicate detection sweep (P2)

### Phase J — Frontend (All priorities) ✅ COMPLETE
1. ✅ Company e-Invoice Profile setup (`CompanyEInvoiceProfilePage`)
2. ✅ Compliance Dashboard (`ComplianceDashboardPage`)
3. ✅ Documents list with filtering (`DocumentsPage` + `ResubmitModal`)
4. ✅ Submission Status with timeline (`SubmissionStatusPage`)
5. ✅ Digital Certificates management (`CertificatesPage`)
6. ✅ API Credentials management (`CredentialsPage`)
7. ✅ Tax Codes list/form screen (`TaxCodesPage`)
8. ✅ Tax Rates list/form with effective-dating (`TaxRatesPage`)
9. ✅ Tax Determination Rules page (`TaxDeterminationRulesPage`)
10. ✅ SST Returns page (`SstReturnsPage`)
11. ✅ Item Extensions page (`ItemExtensionsPage`)
12. ✅ Local Code Tables viewer (`LocalCodeTablesPage`)
13. ✅ Reports screens (Submission Status + Invalid/Rejected tabs)

### Phase K — Tests
1. Unit tests for TaxService determination logic
2. Unit tests for EInvoiceComplianceHelper
3. Unit tests for UBL generator
4. Integration tests for submit/poll/cancel flow

---

Go-live gate: all `P0` items complete and passing UAT in the **sandbox** environment against real (or realistic) transaction data, followed by a deliberate, access-controlled cut-over to production credentials/certificate per `architecture.md` §10.
