using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Integration.Migrations
{
    /// <inheritdoc />
    public partial class InitialIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "int");

            migrationBuilder.CreateTable(
                name: "EdiAcks",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AckType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OriginalIsaControlNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    OriginalGsControlNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    OriginalStControlNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RawContent = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdiAcks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EdiDocumentMappings",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradingPartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MappingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdiDocumentMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EdiTransactionLogs",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradingPartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartnerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsaControlNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GsControlNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    StControlNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RawContent = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdiTransactionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileArchivalPolicies",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    ArchiveLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileArchivalPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EdiDocumentMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ValidatedRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false),
                    CommittedRows = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ValidatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CommittedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingPartners",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsaId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GsSenderId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GsReceiverId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Protocol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CertificateThumbprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EdiVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CertificateExpiresOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveryLogs",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeliveredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PayloadFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    RetryDelaySeconds = table.Column<int>(type: "int", nullable: false),
                    LastDeliveredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastDeliveryStatus = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobLines",
                schema: "int",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    RowData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Errors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Imported = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobLines_ImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalSchema: "int",
                        principalTable: "ImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EdiAcks_ProcessedOn",
                schema: "int",
                table: "EdiAcks",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_EdiAcks_Status",
                schema: "int",
                table: "EdiAcks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EdiDocumentMappings_CompanyId_DocumentType_Direction",
                schema: "int",
                table: "EdiDocumentMappings",
                columns: new[] { "CompanyId", "DocumentType", "Direction" });

            migrationBuilder.CreateIndex(
                name: "IX_EdiTransactionLogs_DocumentType",
                schema: "int",
                table: "EdiTransactionLogs",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_EdiTransactionLogs_ProcessedOn",
                schema: "int",
                table: "EdiTransactionLogs",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_EdiTransactionLogs_Status",
                schema: "int",
                table: "EdiTransactionLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FileArchivalPolicies_CompanyId_IntegrationType",
                schema: "int",
                table: "FileArchivalPolicies",
                columns: new[] { "CompanyId", "IntegrationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobLines_ImportJobId_RowNumber",
                schema: "int",
                table: "ImportJobLines",
                columns: new[] { "ImportJobId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CompanyId_CreatedOn",
                schema: "int",
                table: "ImportJobs",
                columns: new[] { "CompanyId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                schema: "int",
                table: "ImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_CompanyId_PartnerCode",
                schema: "int",
                table: "TradingPartners",
                columns: new[] { "CompanyId", "PartnerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_DeliveredOn",
                schema: "int",
                table: "WebhookDeliveryLogs",
                column: "DeliveredOn");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_EventType",
                schema: "int",
                table: "WebhookDeliveryLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_Status",
                schema: "int",
                table: "WebhookDeliveryLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_CompanyId_EventType",
                schema: "int",
                table: "WebhookSubscriptions",
                columns: new[] { "CompanyId", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EdiAcks",
                schema: "int");

            migrationBuilder.DropTable(
                name: "EdiDocumentMappings",
                schema: "int");

            migrationBuilder.DropTable(
                name: "EdiTransactionLogs",
                schema: "int");

            migrationBuilder.DropTable(
                name: "FileArchivalPolicies",
                schema: "int");

            migrationBuilder.DropTable(
                name: "ImportJobLines",
                schema: "int");

            migrationBuilder.DropTable(
                name: "TradingPartners",
                schema: "int");

            migrationBuilder.DropTable(
                name: "WebhookDeliveryLogs",
                schema: "int");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions",
                schema: "int");

            migrationBuilder.DropTable(
                name: "ImportJobs",
                schema: "int");
        }
    }
}
