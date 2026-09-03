# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module
## Implementation Backlog

**Document:** todotax.md | **Version:** 1.0 | **Date:** 3 September 2026 | **Companion docs:** spec.md, architecture.md, todo.md, spec_malaysian_tax.md

---

## P0 - Blocks Go-Live

### Tax Module
- [x] TAX-01: Tax Code CRUD (code, description, LHDN tax type, active status)
- [x] TAX-02: Tax Rate CRUD with effective-dating (rate %, effective dates)
- [ ] TAX-03: Tax determination engine (assign tax code to line items)
- [ ] TAX-04: Support tax-inclusive/exclusive pricing with RMCD rounding rules
- [ ] TAX-05: Track company SST registration status and numbers

### e-Invoice Module
- [x] EINV-01: Company/Taxpayer Profile (TIN, BRN, SST number, MSIC, address)
- [ ] EINV-02: Extend Customer/Vendor master with TIN, ID type/value, SST reg no
- [ ] EINV-03: Extend Item master with classification code and unit of measure
- [ ] EINV-04: Generate UBL 2.1 Invoice from Sales Invoice (map 37 mandatory fields)
- [ ] EINV-05: Generate Credit/Debit/Refund Notes
- [ ] EINV-06: Generate Self-Billed documents
- [ ] EINV-07: Generate Consolidated e-Invoice (exclude RM10,000+ transactions)
- [ ] EINV-08: Digitally sign outbound documents (XAdES)
- [ ] EIN-09: Submit to MyInvois API
- [ ] EINV-10: Poll submission status until Valid/Invalid
- [ ] EINV-11: Surface Invalid errors with plain-language explanation
- [ ] EINV-12: Support cancellation within 72-hour window
- [ ] EINV-13: Buyer/Supplier TIN validation
- [ ] EINV-14: Attach QR code and validation link to PDF/emailed invoice
- [ ] EINV-15: Maintain audit history (7-year retention)
- [ ] EINV-16: Support sandbox/production environments
- [ ] EINV-17: Keep local copies of LHDN code tables
- [ ] EINV-18: Detect and block duplicate submissions
- [ ] EINV-19: Support foreign-currency invoices

---

## P1 - High Priority

### e-Invoice Module (cont'd)
- [ ] EINV-07b: SST Return workspace (SST-02/SST-02A reporting)
- [ ] EINV-15a: Reconciliation report (ERP vs MyInvois)
- [ ] EINV-15b: Compliance dashboard

### Background Jobs
- [ ] Certificate expiry monitoring job
- [ ] Status poller job (poll submissions until terminal state)
- [ ] Consolidated e-Invoice batch job (monthly)
- [ ] TIN validation cache refresh job
- [ ] LHDN code table sync job

### Reports
- [ ] e-Invoice Submission Status report
- [ ] SST-02 / SST-02A report
- [ ] Reconciliation report
- [ ] Compliance Dashboard

---

## P2 - Medium Priority

### Advanced Features
- [ ] Adjustment documents UI (Credit/Debit/Refund Notes)
- [ ] Self-billed document creation
- [ ] QR code generation and attachment
- [ ] Foreign currency exchange rate handling

### Data Management
- [ ] LHDN code tables import job
- [ ] Document backup/archive strategy
- [ ] Data retention compliance

---

## Frontend Pages Required (from frontendtax.md)

### Tax Module Pages
- [ ] Tax Codes list + form
- [ ] Tax Rates list + form (effective-dated)
- [ ] SST Return Workspace

### e-Invoice Module Pages
- [ ] Company e-Invoice Profile (TIN, cert, environment)
- [ ] Document Queue / Dashboard
- [ ] Document Detail
- [ ] Consolidated e-Invoice Batches
- [ ] Credit / Debit / Refund Notes
- [ ] Self-Billed Documents
- [ ] Certificates & API Credentials

### Master Data Extensions
- [ ] Customer master → Tax & e-Invoice tab
- [ ] Vendor master → Tax & e-Invoice tab
- [ ] Item master → Classification tab

### Reports Pages
- [ ] e-Invoice Submission Status report
- [ ] SST-02 / SST-02A report
- [ ] Reconciliation report
- [ ] Compliance Dashboard