using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PayrollTaxDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxDepositSchedules",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Agency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepositDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepositedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DepositedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FormType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Deposited = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDepositSchedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDepositSchedules_CompanyId",
                schema: "pay",
                table: "TaxDepositSchedules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDepositSchedules_DepositDate",
                schema: "pay",
                table: "TaxDepositSchedules",
                column: "DepositDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxDepositSchedules",
                schema: "pay");
        }
    }
}
