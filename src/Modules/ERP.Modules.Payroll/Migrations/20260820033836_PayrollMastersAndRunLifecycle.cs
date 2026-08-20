using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class PayrollMastersAndRunLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeductionBenefits",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsPreTax = table.Column<bool>(type: "bit", nullable: false),
                    DefaultRate = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    GlAccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_DeductionBenefits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDeductionBenefits",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeductionBenefitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDeductionBenefits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManualChecks",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CheckNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
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
                    table.PrimaryKey("PK_ManualChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollChecks",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDirectDeposit = table.Column<bool>(type: "bit", nullable: false),
                    AchTraceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PtoLedgers",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccrualRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    MaxAccrual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CarryoverLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Accrued = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Used = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PtoLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "W4Records",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilingStatus = table.Column<int>(type: "int", nullable: false),
                    Allowances = table.Column<int>(type: "int", nullable: false),
                    IsLegacyPre2020 = table.Column<bool>(type: "bit", nullable: false),
                    AdditionalWithholding = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MultipleJobs = table.Column<bool>(type: "bit", nullable: false),
                    DependentsCredit = table.Column<int>(type: "int", nullable: false),
                    OtherIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Deductions = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_W4Records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WageBaseLimits",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LimitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SurtaxThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WageBaseLimits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkersCompClassCodes",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RatePer100 = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ExperienceModification = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkersCompClassCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PtoTransaction",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PtoLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AsOf = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PtoTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PtoTransaction_PtoLedgers_PtoLedgerId",
                        column: x => x.PtoLedgerId,
                        principalSchema: "pay",
                        principalTable: "PtoLedgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeductionBenefits_CompanyId",
                schema: "pay",
                table: "DeductionBenefits",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductionBenefits_DeductionBenefitId",
                schema: "pay",
                table: "EmployeeDeductionBenefits",
                column: "DeductionBenefitId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductionBenefits_EmployeeId",
                schema: "pay",
                table: "EmployeeDeductionBenefits",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualChecks_CompanyId",
                schema: "pay",
                table: "ManualChecks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualChecks_EmployeeId",
                schema: "pay",
                table: "ManualChecks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollChecks_EmployeeId",
                schema: "pay",
                table: "PayrollChecks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollChecks_PayrollRunId",
                schema: "pay",
                table: "PayrollChecks",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PtoLedgers_EmployeeId",
                schema: "pay",
                table: "PtoLedgers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PtoTransaction_PtoLedgerId",
                schema: "pay",
                table: "PtoTransaction",
                column: "PtoLedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_W4Records_EmployeeId",
                schema: "pay",
                table: "W4Records",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageBaseLimits_CompanyId_Year_Type",
                schema: "pay",
                table: "WageBaseLimits",
                columns: new[] { "CompanyId", "Year", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkersCompClassCodes_CompanyId",
                schema: "pay",
                table: "WorkersCompClassCodes",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeductionBenefits",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "EmployeeDeductionBenefits",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "ManualChecks",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "PayrollChecks",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "PtoTransaction",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "W4Records",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "WageBaseLimits",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "WorkersCompClassCodes",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "PtoLedgers",
                schema: "pay");
        }
    }
}
