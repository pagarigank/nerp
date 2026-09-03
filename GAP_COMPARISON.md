# Unified Gap Comparison
## Cross-referencing todo.md, todotax.md, todotax2.md, AUDIT_REPORT.md, spec.md

**Date:** 3 September 2026  
**Method:** Compare every [ ] item across all documents, deduplicate, and classify as BUILT / NOT BUILT / UNCERTAIN

---

## Executive Summary

| Category | Items Still NOT BUILT | Priority |
|----------|:---------------------:|:--------:|
| Tax/e-Invoice (todotax2.md) | 12 | Mix of P1/P2 |
 Integration EDI (todo.md) | 15+ | P1/P2 |
| Core ERP Phases 0-14 (todo.md) | 85+ unit tests, ~25 integration tests | P0 |
| UI/UX (AUDIT_REPORT.md) | 8 items | P1 |
| Cross-cutting (AUDIT_REPORT.md) | 6 items | P0-P2 |

---

## 1. TAX / E-INVOICE MODULE — Still Missing (from todotax2.md)

### Not Built (verified against todotax2.md checkboxes)

| ID | Item | Priority | Status |
|----|------|:--------:|--------|
| EINV-07 | Exclude receipts ≥ RM10,000 from consolidation | P0 | Partial — `RequiresIndividualSubmission` exists but no filter logic during consolidation |
| EINV-12 | Auto-redirect to Credit/Debit/Refund after 72h window | P1 | ❌ Not built |
| TAX-06 | SST-02 / SST-02A computation — actual aggregation of posted transactions | P1 | Entity+service exist but no real transaction aggregation |
| TAX-06 | SST-02 (Service Tax) return report | P1 | ❌ Not built |
| TAX-06 | SST-02A (Sales Tax) return report | P1 | ❌ Not built |
| EINV-07 | Consolidated e-Invoice summary report | P1 | ❌ Not built |
| TAX-01/02 | Sales/Service Tax collected report | P1 | ❌ Not built |
| EINV-15 | Audit trail / change log report | P1 | ❌ Not built |
| EINV-08/09 | Certificate & API credential expiry report | P1 | ❌ Not built |
| TAX-07 | Compliance Dashboard | P2 | ❌ Not built (ComplianceDashboardPage exists but not wired to real data) |
| EINV-02/03 | Buyer/Supplier data-quality report | P2 | ❌ Not built |
| EINV-17 | Local Code Table CRUD/viewer with "last synced" indicator | P1 | Partial — `LocalCodeTablesPage` exists but no CodeTableCache entity or sync job |
| EINV-06 | Self-billed scenario reference data CRUD | P2 | ❌ Not built |
| EINV-18 | Duplicate-document detection background sweep | P2 | ❌ Not built |
| — | Rate-limit-aware batching scheduler | P2 | ❌ Not built |
| — | Turnover threshold monitor | P2 | ❌ Not built |
| — | Notification/webhook listener for MyInvois push updates | P2 | ❌ Not built |
| — | Unit tests for TaxService, UblGenerator, EInvoiceComplianceHelper | P0 | ✅ **BUILT** (59 tests in commit `1210f9c`) |
| — | Integration tests for submit/poll/cancel flow | P0 | ❌ Not built |

---

## 2. INTEGRATION/EDI MODULE — Still Missing (from todo.md)

### Not Built (verified against todo.md Phase 14 checkboxes)

| Item | Priority | Status |
|------|:--------:|--------|
| Bulk export job | P1 | ❌ Not built |
| X12 segment/qualifier validation (element-level) | P1 | ❌ Not built |
| Functional acknowledgment generation (997/999) | P1 | ❌ Not built |
| AS2 communication (MDN, signing, encryption) | P1 | ❌ Not built |
| Outbound EDI from AR invoices (810) and OM shipments (856) — **actual line-level data** | P1 | Partial — handlers exist but use placeholder data, not real invoice/shipment lines |
| Webhook idempotency (delivery ID dedupe) | P1 | ❌ Not built |
| Import error correction workflow (fix row → revalidate) | P1 | ❌ Not built |
| PGP/encryption for sensitive outbound files | P1 | ❌ Not built |
| EDI acknowledgment timeout monitor (alert) | P1 | ❌ Not built |
| Import/Export job scheduler (run on schedule) | P1 | ❌ Not built |
| Webhook dead-letter reprocessing job | P1 | ❌ Not built |
| EDI mapping/translation designer (visual field-map editor) | P1 | ❌ Not built |
| Raw EDI document viewer (segment-level inspection) | P1 | ❌ Not built |
| Vendor/Customer self-service portal | P2 | ❌ Not built |
| API keys / scoped machine identities | P1 | ❌ Not built |
| Inbound 850 → PO wiring (vendor/customer lookup from EDI partner codes) | P1 | Partial — creates PO but vendor/customer are placeholder GUIDs |
| Inbound 810 → AR Invoice wiring (GL account mapping) | P1 | Partial — creates invoice but accountId is placeholder |
| Inbound 856 → Shipment status update | P1 | ❌ Not built (only logs to transaction log) |
| Outbound 810 from AR invoices (real line-level data from InvoiceLines) | P1 | ❌ Not built (uses batch-level totals only) |
| Outbound 856 from OM shipments (real line-level data from ShipmentLines) | P1 | ❌ Not built |

---

## 3. CORE ERP PHASES 0-14 — Still Missing (from todo.md + AUDIT_REPORT.md)

### Tests (Largest Gap)

| Module | Missing Unit Tests | Missing Integration Tests |
|--------|:------------------:|:-------------------------:|
| BOM (Ph.9) | 12 | 7 |
| Project Accounting (Ph.10) | 26 | 8 |
| Payroll (Ph.11) | 12 | 7 |
| Field Service (Ph.12) | N/A | N/A |
| BI/Reporting (Ph.13) | N/A | N/A |
| Integration (Ph.14) | N/A | N/A |
| Tax/e-Invoice (Ph.15) | 0 (built) | 1 (submit/poll/cancel flow) |
| **Total** | **~50** | **~23** |

### Pre-existing Test Compilation Errors

| Test Project | Issue |
|-------------|-------|
| ArServiceTests | Compilation errors (not caused by our changes) |
| InventoryTests | Compilation errors (not caused by our changes) |

### Cross-Cutting Gaps

| Gap | Priority | Phase |
|-----|:--------:|:-----:|
| Async event bus (RabbitMQ/Azure Service Bus) | P2 | Ph.0 |
| PII redaction in audit log | P0 | Ph.1 |
| Shared component library not extracted | P1 | Frontend |
| Performance/load testing not done | P1 | Non-functional |
| Security hardening (pen test, security audit) | P1 | Non-functional |
| Legacy data migration tools | P2 | Infra |
| Disaster recovery not planned | P2 | Infra |
| Email notifications for approvals | P1 | Multiple |

### UI/UX Issues (from AUDIT_REPORT.md)

| Issue | Priority | Status |
|-------|:--------:|--------|
| Density mode toggle | P1 | ✅ **BUILT** (commit `917bc44`) |
| Keyboard-first grid navigation | P1 | ✅ **BUILT** (BatchEntryGrid in commit `917bc44`) |
| Draft autosave | P1 | ✅ **BUILT** (commit `9e04a6d`) |
| Company/Period switcher in header | P1 | ❌ Not built |
| Universal search (Cmd/Ctrl+K) | P1 | ❌ Not built |
| Drill-back on report figures | P1 | ✅ **BUILT** (commit `7d70d70`) |
| Favorites/Recents navigation | P1 | ✅ **BUILT** (commit `bc560c7`) |
| Empty/First-run states | P1 | ❌ Not built |
| Print/PDF styles | P2 | ❌ Not built |
| Status vocabulary inconsistency | P2 | ❌ Not built |
| Dark mode | P2 | ❌ Not built |
| Responsive mobile layout | P2 | ❌ Not built |

---

## 4. FIELD SERVICE MODULE (Phase 12) — Status Clarification

**AUDIT_REPORT.md says:** "0% complete — entire module missing"  
**Reality:** Phase 12 IS built (verified in todo.md and codebase). The AUDIT_REPORT.md is outdated.

| Component | Status |
|-----------|--------|
| Backend entities + DbContext + migration | ✅ Built |
| FieldServiceMastersController | ✅ Built |
| WorkOrderController | ✅ Built |
| FieldServiceReportsController | ✅ Built |
| Background jobs (PM generation, SLA breach) | ✅ Built |
| Cross-module wiring (Inventory, AR, Payroll) | ✅ Built |
| Frontend (13-tab FieldServicePage) | ✅ Built |
| Navigation + routes | ✅ Built |

---

## 5. BI/REPORTING MODULE (Phase 13) — Status Clarification

**AUDIT_REPORT.md says:** "0% complete — entire module missing"  
**Reality:** Phase 13 IS partially built (verified in codebase).

| Component | Status |
|-----------|--------|
| ReportDefinition entity + CRUD | ✅ Built |
| ReportCategory entity + CRUD | ✅ Built |
| ReportParameterSet entity + CRUD | ✅ Built |
| QuickQuery entity + CRUD | ✅ Built |
| SavedQuery entity + CRUD | ✅ Built |
| DashboardWidget entity + CRUD | ✅ Built |
| FinancialStatementLayout entity | ✅ Built |
| SearchIndexEntry + SearchQueryLog | ✅ Built |
| ReportSubscription entity | ✅ Built |
| ReportUsageLog entity | ✅ Built |
| SyncWatermark + SyncRunLog | ✅ Built |
| DeliveryRetryEntry entity | ✅ Built |
| ReportingDbContext + migration | ✅ Built |
| Frontend pages | ✅ Built (11 pages) |
| Navigation + routes | ✅ Built |
| Management-Reporter-style statement designer | ❌ Not built |
| Data mart / CDC pipeline | ❌ Not built |

---

## 6. UNIFIED PRIORITY ACTION LIST

### P0 — Blocks Go-Live (Do First)

1. ~~Write unit tests for TaxService, UblGenerator, EInvoiceComplianceHelper~~ ✅ DONE
2. Fix pre-existing test compilation errors (ArServiceTests, InventoryTests)
3. Implement PII redaction in audit log (before Payroll data enters system)
4. Wire inbound EDI vendor/customer lookup (replace placeholder GUIDs)
5. Wire inbound EDI GL account mapping (replace placeholder Guid.Empty)
6. Wire outbound EDI with real invoice/shipment line data

### P1 — High Priority (Production Quality)

7. SST-02/SST-02A real transaction aggregation (not just entity shell)
8. Consolidated e-Invoice: exclude ≥RM10K receipts from consolidation
9. Company/Period switcher in header (persistent, always visible)
10. Cmd/Ctrl+K universal search (cross-module)
11. Empty/first-run states on all list screens
12. Functional acknowledgment generation (997/999)
13. Webhook idempotency (delivery ID dedupe)
14. Import error correction workflow
15. Local Code Table sync job + "last synced" indicator
16. ~50 missing unit tests (BOM/Project/Payroll)
17. ~23 missing integration tests (BOM/Project/Payroll)
18. Extract shared component library (LookupCombo, StatusBadge, etc.)

### P2 — Medium Priority (Nice-to-Have)

19. Auto-redirect to Credit/Debit/Refund after 72h window
20. Compliance Dashboard with real data
21. Certificate & API credential expiry report
22. Audit trail / change log report
23. Duplicate-document detection background sweep
24. AS2 communication (MDN, signing)
25. PGP/encryption for outbound files
26. Print/PDF styles for financial documents
27. Dark mode
28. Responsive mobile layout
29. Legacy data migration tools
30. Disaster recovery plan
31. Load testing (500 concurrent users)

---

## 7. FILES UPDATED TO REFLECT CURRENT STATE

| File | Last Updated | Needs Update |
|------|-------------|:------------:|
| todotax.md | 3 Sep 2026 | ⚠️ Outdated — many items marked [ ] are actually built |
| todotax2.md | 3 Sep 2026 | ✅ Mostly accurate — minor updates needed for Phase 14 wiring |
| AUDIT_REPORT.md | 3 Sep 2026 | ⚠️ Outdated — Field Service, BI/Reporting, Integration marked 0% but are partially built |
| todo.md | 3 Sep 2026 | ⚠️ Phase 14 updated to ~55%, but Phases 12-13 status still says 0% |
| RBAC.md | 3 Sep 2026 | ✅ Accurate |

---

*End of Gap Comparison*
