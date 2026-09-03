using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.EInvoice.Migrations
{
    /// <inheritdoc />
    public partial class InitialEInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "int");

            migrationBuilder.EnsureSchema(
                name: "ar");

            migrationBuilder.EnsureSchema(
                name: "inv");

            migrationBuilder.EnsureSchema(
                name: "ap");

            migrationBuilder.CreateTable(
                name: "api_credential",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EncryptedClientSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRotatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_credential", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "digital_certificate",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NotBefore = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotAfter = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Thumbprint = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VaultObjectId = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CertificateBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CertificatePassword = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_certificate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "einvoice_customer_extension",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxIdentificationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SstRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SstBillingAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SstBillingCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SstBillingState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SstBillingPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SstBillingCountry = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    IsSubjectToEInvoice = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_customer_extension", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "einvoice_document",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", maxLength: 18, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCodeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SupplierTIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerTIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MyInvoisUuid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValidationLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QrCodePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LhdnsErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LhdnsErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmissionUid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SelfBilledInvoiceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BuyerRejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsolidatedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_document", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "einvoice_document_archive",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCodeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SupplierTIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerTIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MyInvoisUuid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValidationLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LhdnsErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LhdnsErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmissionUid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_document_archive", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "einvoice_item_extension",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassificationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UnitOfMeasureCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ProductTariffCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    IsSubjectToEInvoice = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_item_extension", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "einvoice_status_history",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EInvoiceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LhdnErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LhdnErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MyInvoisUuid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValidationLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_status_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "einvoice_vendor_extension",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxIdentificationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SstRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SstBillingAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SstBillingCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SstBillingState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SstBillingPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SstBillingCountry = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    IsSubjectToEInvoice = table.Column<bool>(type: "bit", nullable: false),
                    CanIssueSelfBilled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_vendor_extension", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "taxpayer_profile",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxIdentificationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BusinessRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SalesTaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServiceTaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MsicCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsSubjectToEInvoice = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_taxpayer_profile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_credential_CompanyId_Environment",
                schema: "int",
                table: "api_credential",
                columns: new[] { "CompanyId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_api_credential_IsActive",
                schema: "int",
                table: "api_credential",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_digital_certificate_NotAfter_IsActive",
                schema: "int",
                table: "digital_certificate",
                columns: new[] { "NotAfter", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_digital_certificate_Thumbprint",
                schema: "int",
                table: "digital_certificate",
                column: "Thumbprint");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_customer_extension_CustomerId",
                schema: "ar",
                table: "einvoice_customer_extension",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_customer_extension_TaxIdentificationNumber",
                schema: "ar",
                table: "einvoice_customer_extension",
                column: "TaxIdentificationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_CreatedAt",
                schema: "int",
                table: "einvoice_document",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_IsSandbox_Status",
                schema: "int",
                table: "einvoice_document",
                columns: new[] { "IsSandbox", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_MyInvoisUuid",
                schema: "int",
                table: "einvoice_document",
                column: "MyInvoisUuid",
                unique: true,
                filter: "[MyInvoisUuid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_SourceDocumentId_SourceDocumentType",
                schema: "int",
                table: "einvoice_document",
                columns: new[] { "SourceDocumentId", "SourceDocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_Status",
                schema: "int",
                table: "einvoice_document",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_archive_ArchivedAt",
                schema: "int",
                table: "einvoice_document_archive",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_archive_CompanyId",
                schema: "int",
                table: "einvoice_document_archive",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_archive_CompanyId_ArchivedAt",
                schema: "int",
                table: "einvoice_document_archive",
                columns: new[] { "CompanyId", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_document_archive_MyInvoisUuid",
                schema: "int",
                table: "einvoice_document_archive",
                column: "MyInvoisUuid");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_status_history_EInvoiceDocumentId",
                schema: "int",
                table: "einvoice_status_history",
                column: "EInvoiceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_status_history_EInvoiceDocumentId_OccurredAt",
                schema: "int",
                table: "einvoice_status_history",
                columns: new[] { "EInvoiceDocumentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_status_history_OccurredAt",
                schema: "int",
                table: "einvoice_status_history",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_vendor_extension_TaxIdentificationNumber",
                schema: "ap",
                table: "einvoice_vendor_extension",
                column: "TaxIdentificationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_vendor_extension_VendorId",
                schema: "ap",
                table: "einvoice_vendor_extension",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_taxpayer_profile_TaxIdentificationNumber",
                schema: "int",
                table: "taxpayer_profile",
                column: "TaxIdentificationNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_credential",
                schema: "int");

            migrationBuilder.DropTable(
                name: "digital_certificate",
                schema: "int");

            migrationBuilder.DropTable(
                name: "einvoice_customer_extension",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "einvoice_document",
                schema: "int");

            migrationBuilder.DropTable(
                name: "einvoice_document_archive",
                schema: "int");

            migrationBuilder.DropTable(
                name: "einvoice_item_extension",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "einvoice_status_history",
                schema: "int");

            migrationBuilder.DropTable(
                name: "einvoice_vendor_extension",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "taxpayer_profile",
                schema: "int");
        }
    }
}
