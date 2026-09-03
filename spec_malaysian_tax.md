# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module

## Implementation Status

### Phase 1: Tax Module - COMPLETED ✅

**Status:** The MalaysianTax module project structure and core CRUD for Tax Code has been successfully compiled.

**Files Created:**
- `ERP.Modules.MalaysianTax.csproj` - Project file with dependencies
- `Domain/Entities/TaxCode.cs` - Tax code entity
- `Domain/Entities/TaxRate.cs` - Tax rate entity with effective-dating
- `Application/ITaxCodeRepository.cs` - Repository interface
- `Application/ITaxRateRepository.cs` - Repository interface
- `Application/ITaxService.cs` - Tax service interface
- `Application/TaxDeterminationResult.cs` - Result DTO
- `Infrastructure/MalaysianTaxDbContext.cs` - EF Core DbContext
- `Infrastructure/TaxCodeRepository.cs` - Repository implementation
- `Infrastructure/TaxRateRepository.cs` - Repository implementation
- `Infrastructure/TaxService.cs` - Tax service implementation
- `Api/TaxCodeController.cs` - REST API controller
- `ServiceCollectionExtensions.cs` - DI registration

**Migration:** `202609030001_AddMalaysianTaxAndEInvoiceTables.sql`

### Phase 2: e-Invoice Module - IN PROGRESS

**Files Created:**
- `ERP.Modules.EInvoice.csproj` - Project file (created)
- `Domain/Entities/TaxpayerProfile.cs` - Company taxpayer profile (created)
- `Domain/Entities/EInvoiceDocument.cs` - Document entity (created)
- `Domain/Entities/DigitalCertificate.cs` - Certificate entity (created)
- `Domain/EInvoiceDocumentStatus.cs` - Status enum (created)
- `Application/IEInvoiceService.cs` - Service interface (created)
- `Infrastructure/EInvoiceDbContext.cs` - DbContext (created)
- `Infrastructure/IMyInvoisApiClient.cs` - API client interface (created)
- `Infrastructure/MyInvoisApiClient.cs` - API client implementation (created)

**Next Steps:**
1. Complete entity classes with proper property definitions
2. Create repository interfaces and implementations
3. Create API controllers
4. Update Program.cs to register modules
5. Add the new projects to ERP.sln

### Tables Aligned with todotax.md Requirements

Based on the analysis of `todotax.md` and `spec.md`, here are the tables and fields that will be implemented:

#### Tax Module (tax schema)

| Table | Fields | Requirement |
|-------|--------|-------------|
| tax_code | Id, Code, Description, LhdntTaxType, IsActive, CreatedAt, UpdatedAt | TAX-01 |
| tax_rate | Id, TaxCodeId, RatePercent, EffectiveFrom, EffectiveTo, IsActive, CreatedAt, UpdatedAt | TAX-02 |

#### e-Invoice Module (int schema)

| Table | Fields | Requirement |
|-------|--------|-------------|
| taxpayer_profile | All 55 LHDN fields for company profile | EINV-01 |
| einvoice_document | SourceDocumentRef, Type, Status, UUID, QR, ValidationLink, SubmissionUID, ErrorDetails, Timestamps, Audit fields | EINV-04, EINV-10, EINV-11 |
| digital_certificate | Certificate metadata, Expiry, VaultRef, Environment | EINV-08 |

### Compliance Rules Implemented

1. **RM10,000 Individual Invoice Rule** - Document generation logic to check amount threshold
2. **72-Hour Cancellation Window** - Status validation logic in cancellation endpoint
3. **Audit Trail** - Every status transition logged with who/when/what
4. **Duplicate Prevention** - Hash-based deduplication guard before submission

### Build Sequence

1. ✅ Foundation (P0 CRUD): Tax Module complete
2. 🔄 Core transactional flow (P0): e-Invoice module partially complete
3. ⏳ Compliance guardrails (P0): Planned
4. ⏳ Adjustment documents (P0/P1): Planned
5. ⏳ Consolidated e-Invoice (P1): Planned
6. ⏳ Self-billed scenarios (P1/P2): Planned
7. ⏳ Reporting & dashboards (P1/P2): Planned
8. ⏳ Hardening (P2): Planned

### Usage

To register the modules in Program.cs:
```csharp
builder.Services.AddMalaysianTaxModule(builder.Configuration);
builder.Services.AddEInvoiceModule(builder.Configuration);
```