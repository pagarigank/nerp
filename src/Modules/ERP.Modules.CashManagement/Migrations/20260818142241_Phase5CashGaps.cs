using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class Phase5CashGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankDuplicateLines",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankDuplicateLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankFeeAnalyses",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    GeneratedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankFeeAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankGlMappings",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GlAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankGlMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LockboxBatches",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImportedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockboxBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PositivePayExceptions",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReceivedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositivePayExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaleCheckEscheatments",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Payee = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EscheatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReissuedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaleCheckEscheatments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankFeeAnalysisLines",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankFeeAnalysisLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankFeeAnalysisLines_BankFeeAnalyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "cash",
                        principalTable: "BankFeeAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LockboxItems",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockboxBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemittanceDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceiptCreated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockboxItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LockboxItems_LockboxBatches_LockboxBatchId",
                        column: x => x.LockboxBatchId,
                        principalSchema: "cash",
                        principalTable: "LockboxBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankDuplicateLines_CompanyId_CheckNumber_Amount",
                schema: "cash",
                table: "BankDuplicateLines",
                columns: new[] { "CompanyId", "CheckNumber", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_BankFeeAnalyses_CompanyId_Year_Month",
                schema: "cash",
                table: "BankFeeAnalyses",
                columns: new[] { "CompanyId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankFeeAnalysisLines_AnalysisId",
                schema: "cash",
                table: "BankFeeAnalysisLines",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_BankGlMappings_CompanyId_BankAccountId",
                schema: "cash",
                table: "BankGlMappings",
                columns: new[] { "CompanyId", "BankAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LockboxBatches_CompanyId_BatchNumber",
                schema: "cash",
                table: "LockboxBatches",
                columns: new[] { "CompanyId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LockboxBatches_Status",
                schema: "cash",
                table: "LockboxBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LockboxItems_LockboxBatchId",
                schema: "cash",
                table: "LockboxItems",
                column: "LockboxBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PositivePayExceptions_CompanyId_CheckNumber",
                schema: "cash",
                table: "PositivePayExceptions",
                columns: new[] { "CompanyId", "CheckNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PositivePayExceptions_Decision",
                schema: "cash",
                table: "PositivePayExceptions",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_StaleCheckEscheatments_CompanyId_CheckNumber",
                schema: "cash",
                table: "StaleCheckEscheatments",
                columns: new[] { "CompanyId", "CheckNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_StaleCheckEscheatments_Status",
                schema: "cash",
                table: "StaleCheckEscheatments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankDuplicateLines",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankFeeAnalysisLines",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankGlMappings",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "LockboxItems",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "PositivePayExceptions",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "StaleCheckEscheatments",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankFeeAnalyses",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "LockboxBatches",
                schema: "cash");
        }
    }
}
