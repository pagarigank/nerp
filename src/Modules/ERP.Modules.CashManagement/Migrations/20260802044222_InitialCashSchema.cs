using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialCashSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cash");

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoutingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GlAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankFees",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FeeType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FeeDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GlJournalBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_BankFees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankStatements",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatementDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BeginningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Format = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BankStatements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankTransfers",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransferDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_BankTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deposits",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepositDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_Deposits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NsfRecords",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NsfNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BankReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NsfFeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NsfFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_NsfRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationSessions",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankStatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatementDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BeginningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GlJournalBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LockedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_ReconciliationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankContacts",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankContacts_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalSchema: "cash",
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementLines",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankStatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CheckNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MatchedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchedSource = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementLines_BankStatements_BankStatementId",
                        column: x => x.BankStatementId,
                        principalSchema: "cash",
                        principalTable: "BankStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepositLines",
                schema: "cash",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SourceReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositLines_Deposits_DepositId",
                        column: x => x.DepositId,
                        principalSchema: "cash",
                        principalTable: "Deposits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CompanyId_AccountCode",
                schema: "cash",
                table: "BankAccounts",
                columns: new[] { "CompanyId", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_Status",
                schema: "cash",
                table: "BankAccounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankContacts_BankAccountId",
                schema: "cash",
                table: "BankContacts",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankFees_BankAccountId",
                schema: "cash",
                table: "BankFees",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankFees_CompanyId_FeeNumber",
                schema: "cash",
                table: "BankFees",
                columns: new[] { "CompanyId", "FeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankFees_Status",
                schema: "cash",
                table: "BankFees",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_BankStatementId",
                schema: "cash",
                table: "BankStatementLines",
                column: "BankStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_CheckNumber",
                schema: "cash",
                table: "BankStatementLines",
                column: "CheckNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_MatchedTransactionId",
                schema: "cash",
                table: "BankStatementLines",
                column: "MatchedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_Status",
                schema: "cash",
                table: "BankStatementLines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_CompanyId_BankAccountId_StatementNumber",
                schema: "cash",
                table: "BankStatements",
                columns: new[] { "CompanyId", "BankAccountId", "StatementNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_Status",
                schema: "cash",
                table: "BankStatements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransfers_CompanyId_TransferNumber",
                schema: "cash",
                table: "BankTransfers",
                columns: new[] { "CompanyId", "TransferNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransfers_FromBankAccountId",
                schema: "cash",
                table: "BankTransfers",
                column: "FromBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransfers_Status",
                schema: "cash",
                table: "BankTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransfers_ToBankAccountId",
                schema: "cash",
                table: "BankTransfers",
                column: "ToBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositLines_DepositId",
                schema: "cash",
                table: "DepositLines",
                column: "DepositId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositLines_SourceReferenceId",
                schema: "cash",
                table: "DepositLines",
                column: "SourceReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_BankAccountId",
                schema: "cash",
                table: "Deposits",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_CompanyId_DepositNumber",
                schema: "cash",
                table: "Deposits",
                columns: new[] { "CompanyId", "DepositNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_Status",
                schema: "cash",
                table: "Deposits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NsfRecords_CashReceiptId",
                schema: "cash",
                table: "NsfRecords",
                column: "CashReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_NsfRecords_CompanyId_NsfNumber",
                schema: "cash",
                table: "NsfRecords",
                columns: new[] { "CompanyId", "NsfNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NsfRecords_CustomerId",
                schema: "cash",
                table: "NsfRecords",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_BankAccountId",
                schema: "cash",
                table: "ReconciliationSessions",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_BankStatementId",
                schema: "cash",
                table: "ReconciliationSessions",
                column: "BankStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_CompanyId_SessionNumber",
                schema: "cash",
                table: "ReconciliationSessions",
                columns: new[] { "CompanyId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_Status",
                schema: "cash",
                table: "ReconciliationSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankContacts",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankFees",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankStatementLines",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankTransfers",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "DepositLines",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "NsfRecords",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "ReconciliationSessions",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankAccounts",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "BankStatements",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "Deposits",
                schema: "cash");
        }
    }
}
