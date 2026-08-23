using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.ProjectAccounting.Migrations
{
    /// <inheritdoc />
    public partial class Phase10Asc606DocsEacSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractPerformanceObligations",
                schema: "proj",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TransactionPriceAllocated = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StandaloneSellingPriceBasis = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecognizedRevenueToDate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SatisfiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPerformanceObligations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDocuments",
                schema: "proj",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UploadedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEacSnapshots",
                schema: "proj",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BudgetAtCompletion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimateAtCompletion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedMarginPct = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    PendingChangeOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEacSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractPerformanceObligations_CompanyId",
                schema: "proj",
                table: "ContractPerformanceObligations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPerformanceObligations_ProjectId",
                schema: "proj",
                table: "ContractPerformanceObligations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPerformanceObligations_Status",
                schema: "proj",
                table: "ContractPerformanceObligations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_CompanyId",
                schema: "proj",
                table: "ProjectDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_DocumentType",
                schema: "proj",
                table: "ProjectDocuments",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId",
                schema: "proj",
                table: "ProjectDocuments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEacSnapshots_CompanyId",
                schema: "proj",
                table: "ProjectEacSnapshots",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEacSnapshots_ProjectId_CapturedOn",
                schema: "proj",
                table: "ProjectEacSnapshots",
                columns: new[] { "ProjectId", "CapturedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractPerformanceObligations",
                schema: "proj");

            migrationBuilder.DropTable(
                name: "ProjectDocuments",
                schema: "proj");

            migrationBuilder.DropTable(
                name: "ProjectEacSnapshots",
                schema: "proj");
        }
    }
}
