using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class InitialArSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ar");

            migrationBuilder.CreateTable(
                name: "CashReceipts",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceiptDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_CashReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditDebitMemos",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MemoDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AppliedToInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MemoType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditDebitMemos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditHoldDays = table.Column<int>(type: "int", nullable: false),
                    DefaultPaymentTermId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxExempt = table.Column<bool>(type: "bit", nullable: false),
                    TaxExemptCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
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
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceCharges",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChargeDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChargeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_FinanceCharges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceBatches",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostingDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_InvoiceBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statements",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsOfDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatementNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_Statements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashReceiptApplications",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashReceiptApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashReceiptApplications_CashReceipts_CashReceiptId",
                        column: x => x.CashReceiptId,
                        principalSchema: "ar",
                        principalTable: "CashReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymentTermId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceBatches_InvoiceBatchId",
                        column: x => x.InvoiceBatchId,
                        principalSchema: "ar",
                        principalTable: "InvoiceBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditDebitMemoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_CreditDebitMemos_CreditDebitMemoId",
                        column: x => x.CreditDebitMemoId,
                        principalSchema: "ar",
                        principalTable: "CreditDebitMemos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "ar",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashReceiptApplications_CashReceiptId",
                schema: "ar",
                table: "CashReceiptApplications",
                column: "CashReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceiptApplications_InvoiceId",
                schema: "ar",
                table: "CashReceiptApplications",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_CompanyId_ReceiptReference",
                schema: "ar",
                table: "CashReceipts",
                columns: new[] { "CompanyId", "ReceiptReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_CustomerId",
                schema: "ar",
                table: "CashReceipts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_Status",
                schema: "ar",
                table: "CashReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CreditDebitMemos_CustomerId",
                schema: "ar",
                table: "CreditDebitMemos",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditDebitMemos_InvoiceBatchId",
                schema: "ar",
                table: "CreditDebitMemos",
                column: "InvoiceBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerId",
                schema: "ar",
                table: "Customers",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                schema: "ar",
                table: "Customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCharges_ChargeNumber",
                schema: "ar",
                table: "FinanceCharges",
                column: "ChargeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCharges_CustomerId",
                schema: "ar",
                table: "FinanceCharges",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBatches_CompanyId_BatchNumber",
                schema: "ar",
                table: "InvoiceBatches",
                columns: new[] { "CompanyId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBatches_Status",
                schema: "ar",
                table: "InvoiceBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_AccountId",
                schema: "ar",
                table: "InvoiceLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_CreditDebitMemoId",
                schema: "ar",
                table: "InvoiceLines",
                column: "CreditDebitMemoId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                schema: "ar",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                schema: "ar",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId_InvoiceNumber",
                schema: "ar",
                table: "Invoices",
                columns: new[] { "CustomerId", "InvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceBatchId",
                schema: "ar",
                table: "Invoices",
                column: "InvoiceBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Statements_CustomerId",
                schema: "ar",
                table: "Statements",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Statements_StatementNumber",
                schema: "ar",
                table: "Statements",
                column: "StatementNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashReceiptApplications",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "FinanceCharges",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "Statements",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "CashReceipts",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "CreditDebitMemos",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "InvoiceBatches",
                schema: "ar");
        }
    }
}
