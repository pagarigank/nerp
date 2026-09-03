# Malaysia Tax (SST) & e-Invoice (MyInvois) Integration Module
## Implementation Backlog (todotax.md)

## ✅ COMPLETED (P0 - Blocks Go-Live)

### Backend Implementation
- [x] `tax-module-1`: Tax Code CRUD (TaxCode entity, TaxCodeRepository, TaxRateRepository)
- [x] `tax-module-2`: Tax Rate CRUD with effective-dating
- [x] `tax-module-3`: Customer/Vendor extension for e-Invoice fields (TIN, ID Type/Value, SST Reg No, address fields)
- [x] `tax-module-5`: Digital Certificate CRUD
- [x] `tax-module-7`: Company e-Invoice profile extension (TIN/BRN, Sales Tax/Service Tax nos, MSIC code)
- [x] `tax-module-10`: API Credential CRUD
- [x] `tax-module-11`: UBL 2.1 generation capability
- [x] `tax-module-12`: Submit document to MyInvois
- [x] `tax-module-13`: Status polling endpoint
- [x] `tax-module-14`: TIN validation (ValidateTaxpayerTinAsync)
- [x] `tax-module-15`: Document cancellation (within 72-hour window)
- [x] `tax-module-20`: RM10,000 individual invoice rule enforcement
- [x] `tax-module-21`: 72-hour cancellation window enforcement
- [x] `tax-module-25`: Audit trail (status transitions, timestamps)

### Infrastructure
- [x] EInvoiceDbContext with all entity configurations
- [x] MyInvoisApiClient implementing all necessary endpoints
- [x] EInvoiceService with submission, polling, validation, cancellation
- [x] UblGenerator for UBL 2.1 XML generation
- [x] EInvoiceComplianceHelper with threshold rules

### Project Structure
- [x] ERP.Modules.EInvoice project created
- [x] ERP.Modules.MalaysianTax project created
- [x] Project references added to ERP.Api
- [x] All modules compile successfully

## 📁 Files Created

### Domain Entities (EInvoice Module)
- `ERP.Modules.EInvoice/Domain/Entities/EInvoiceDocument.cs`
- `ERP.Modules.EInvoice/Domain/Entities/DigitalCertificate.cs`
- `ERP.Modules.EInvoice/Domain/Entities/TaxpayerProfile.cs`
- `ERP.Modules.EInvoice/Domain/Entities/EInvoiceCustomerExtension.cs`
- `ERP.Modules.EInvoice/Domain/Entities/EInvoiceVendorExtension.cs`
- `ERP.Modules.EInvoice/Domain/Entities/EInvoiceCompanyExtension.cs`
- `ERP.Modules.EInvoice/Domain/EInvoiceDocumentStatus.cs`
- `ERP.Modules.EInvoice/Domain/EInvoiceDtos.cs`

### Infrastructure
- `ERP.Modules.EInvoice/Infrastructure/EInvoiceService.cs`
- `ERP.Modules.EInvoice/Infrastructure/MyInvoisApiClient.cs`
- `ERP.Modules.EInvoice/Infrastructure/UblGenerator.cs`
- `ERP.Modules.EInvoice/Infrastructure/EInvoiceComplianceHelper.cs`

### Database Context
- `ERP.Modules.EInvoice/EInvoiceDbContext.cs`

### API Controllers (ERP.Api project)
- `ERP.Api/Controllers/EInvoiceController.cs`

### Project Files
- `ERP.Modules.EInvoice/ERP.Modules.EInvoice.csproj`

## 🔧 Build Status: ✅ PASS

All code compiles successfully with 0 errors.

## 📊 API Endpoints Implemented

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/v1/e-invoice/documents` | Submit e-Invoice document |
| GET | `/api/v1/e-invoice/documents/{id}/status` | Get document status |
| GET | `/api/v1/e-invoice/documents/{id}/poll-status` | Poll until terminal state |
| GET | `/api/v1/e-invoice/tin-validation/{tin}` | Validate taxpayer TIN |
| POST | `/api/v1/e-invoice/documents/{id}/cancel` | Cancel within 72-hour window |

## 📋 Next Steps (P1 Items)

1. **Reporting Backend**: SST-02/02A return report generation
2. **API Controllers**: TaxRateController, DigitalCertificateController, TaxpayerProfileController
3. **Frontend Pages**: Tax module pages, e-Invoice dashboard
4. **Consolidated e-Invoice**: Batch submission support