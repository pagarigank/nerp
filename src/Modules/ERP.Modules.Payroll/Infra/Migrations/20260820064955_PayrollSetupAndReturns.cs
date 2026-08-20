using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PayrollSetupAndReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchReturns",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TraceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReturnCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnAction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyPayrollSetups",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ein = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FederalTaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StateTaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SutaState = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EftpsPin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepositSchedule = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SocialSecurityRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    MedicareRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    FutaRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    SutaRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    WageExpenseAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollTaxExpenseAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollLiabilityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClearingAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyPayrollSetups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewHireReportingConfigs",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AgencyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DueWindowDays = table.Column<int>(type: "int", nullable: false),
                    TransmissionMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SftpEndpoint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AgencyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewHireReportingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PtoPolicies",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccrualRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    AccrualBasis = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaxAccrual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CarryoverLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashOutRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CashOutAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PtoPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchReturns_CompanyId",
                schema: "pay",
                table: "AchReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AchReturns_PayrollRunId",
                schema: "pay",
                table: "AchReturns",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPayrollSetups_CompanyId",
                schema: "pay",
                table: "CompanyPayrollSetups",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewHireReportingConfigs_CompanyId_StateCode",
                schema: "pay",
                table: "NewHireReportingConfigs",
                columns: new[] { "CompanyId", "StateCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PtoPolicies_CompanyId_Name",
                schema: "pay",
                table: "PtoPolicies",
                columns: new[] { "CompanyId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchReturns",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "CompanyPayrollSetups",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "NewHireReportingConfigs",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "PtoPolicies",
                schema: "pay");
        }
    }
}
