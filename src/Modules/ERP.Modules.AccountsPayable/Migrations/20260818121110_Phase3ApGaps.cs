using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsPayable.Migrations
{
    /// <inheritdoc />
    public partial class Phase3ApGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ap1099Classifications",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormType = table.Column<int>(type: "int", nullable: false),
                    TaxYear = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ap1099Classifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashDiscountCaptures",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAvailable = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountTaken = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountLost = table.Column<bool>(type: "bit", nullable: false),
                    CapturedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDiscountCaptures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DuplicateInvoiceChecks",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConflictingVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    CheckedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateInvoiceChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrirAccruals",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccrualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversedByAccrualId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrirAccruals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaleCheckEscheatments",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssuedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatutoryDays = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReportedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaleCheckEscheatments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorBankVerifications",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBankVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorStatements",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatementDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatementTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorStatements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorW9Records",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TinVerified = table.Column<bool>(type: "bit", nullable: false),
                    TinMatchStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CapturedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorW9Records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorStatementLines",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorStatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatementAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BookAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDisputed = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorStatementLines_VendorStatements_VendorStatementId",
                        column: x => x.VendorStatementId,
                        principalSchema: "ap",
                        principalTable: "VendorStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ap1099Classifications_TaxYear",
                schema: "ap",
                table: "Ap1099Classifications",
                column: "TaxYear");

            migrationBuilder.CreateIndex(
                name: "IX_Ap1099Classifications_VendorId",
                schema: "ap",
                table: "Ap1099Classifications",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDiscountCaptures_VendorId",
                schema: "ap",
                table: "CashDiscountCaptures",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDiscountCaptures_VoucherId",
                schema: "ap",
                table: "CashDiscountCaptures",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateInvoiceChecks_CompanyId",
                schema: "ap",
                table: "DuplicateInvoiceChecks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateInvoiceChecks_InvoiceNumber",
                schema: "ap",
                table: "DuplicateInvoiceChecks",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateInvoiceChecks_VendorId",
                schema: "ap",
                table: "DuplicateInvoiceChecks",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_GrirAccruals_CompanyId",
                schema: "ap",
                table: "GrirAccruals",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GrirAccruals_FiscalPeriodId",
                schema: "ap",
                table: "GrirAccruals",
                column: "FiscalPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_GrirAccruals_VendorId",
                schema: "ap",
                table: "GrirAccruals",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_StaleCheckEscheatments_CompanyId",
                schema: "ap",
                table: "StaleCheckEscheatments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StaleCheckEscheatments_PaymentId",
                schema: "ap",
                table: "StaleCheckEscheatments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_StaleCheckEscheatments_VendorId",
                schema: "ap",
                table: "StaleCheckEscheatments",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBankVerifications_VendorBankAccountId",
                schema: "ap",
                table: "VendorBankVerifications",
                column: "VendorBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorStatementLines_VendorStatementId",
                schema: "ap",
                table: "VendorStatementLines",
                column: "VendorStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorStatements_VendorId",
                schema: "ap",
                table: "VendorStatements",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorW9Records_VendorId",
                schema: "ap",
                table: "VendorW9Records",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ap1099Classifications",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "CashDiscountCaptures",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "DuplicateInvoiceChecks",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "GrirAccruals",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "StaleCheckEscheatments",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "VendorBankVerifications",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "VendorStatementLines",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "VendorW9Records",
                schema: "ap");

            migrationBuilder.DropTable(
                name: "VendorStatements",
                schema: "ap");
        }
    }
}
