using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.GeneralLedger.Migrations
{
    /// <inheritdoc />
    public partial class Phase2GlGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetTransfers",
                schema: "gl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromPeriodNumber = table.Column<int>(type: "int", nullable: false),
                    ToPeriodNumber = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlGainLosses",
                schema: "gl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GainLossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RevaluationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlGainLosses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostingSuspenseItems",
                schema: "gl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceModule = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResolvedBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingSuspenseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YearEndCloseRuns",
                schema: "gl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RetainedEarningsAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExpense = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RetainedEarningsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YearEndCloseRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransfers_BudgetId",
                schema: "gl",
                table: "BudgetTransfers",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransfers_CompanyId",
                schema: "gl",
                table: "BudgetTransfers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GlGainLosses_CompanyId",
                schema: "gl",
                table: "GlGainLosses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GlGainLosses_FiscalPeriodId",
                schema: "gl",
                table: "GlGainLosses",
                column: "FiscalPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingSuspenseItems_CompanyId",
                schema: "gl",
                table: "PostingSuspenseItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingSuspenseItems_Status",
                schema: "gl",
                table: "PostingSuspenseItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_YearEndCloseRuns_CompanyId",
                schema: "gl",
                table: "YearEndCloseRuns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_YearEndCloseRuns_FiscalYearId",
                schema: "gl",
                table: "YearEndCloseRuns",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_YearEndCloseRuns_Status",
                schema: "gl",
                table: "YearEndCloseRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetTransfers",
                schema: "gl");

            migrationBuilder.DropTable(
                name: "GlGainLosses",
                schema: "gl");

            migrationBuilder.DropTable(
                name: "PostingSuspenseItems",
                schema: "gl");

            migrationBuilder.DropTable(
                name: "YearEndCloseRuns",
                schema: "gl");
        }
    }
}
