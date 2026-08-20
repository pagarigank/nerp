using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class PayrollFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pay");

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SsnEncrypted = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsBillable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayCodes",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    GlAccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsOvertime = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsHoursWorked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekEnding = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SupervisorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnionCertifiedProfiles",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradeClassification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Jurisdiction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrevailingWageRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FringeBenefitRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnionLocal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnionCertifiedProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCompensations",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OvertimeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DoubleTimeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsSalary = table.Column<bool>(type: "bit", nullable: false),
                    SalaryAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeCompensations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCompensations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "pay",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePayCodes",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverrideRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsBillable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePayCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePayCodes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "pay",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetLines",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TradeClassification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsBillable = table.Column<bool>(type: "bit", nullable: false),
                    IsOvertime = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetLines_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalSchema: "pay",
                        principalTable: "Timesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCompensations_EmployeeId",
                schema: "pay",
                table: "EmployeeCompensations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayCodes_EmployeeId",
                schema: "pay",
                table: "EmployeePayCodes",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayCodes_PayCodeId",
                schema: "pay",
                table: "EmployeePayCodes",
                column: "PayCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_EmployeeCode",
                schema: "pay",
                table: "Employees",
                columns: new[] { "CompanyId", "EmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayCodes_CompanyId_Code",
                schema: "pay",
                table: "PayCodes",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetLines_ProjectId",
                schema: "pay",
                table: "TimesheetLines",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetLines_TimesheetId",
                schema: "pay",
                table: "TimesheetLines",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_CompanyId",
                schema: "pay",
                table: "Timesheets",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_EmployeeId",
                schema: "pay",
                table: "Timesheets",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_Status",
                schema: "pay",
                table: "Timesheets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UnionCertifiedProfiles_CompanyId_TradeClassification_Jurisdiction",
                schema: "pay",
                table: "UnionCertifiedProfiles",
                columns: new[] { "CompanyId", "TradeClassification", "Jurisdiction" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeCompensations",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "EmployeePayCodes",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "PayCodes",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "TimesheetLines",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "UnionCertifiedProfiles",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "Timesheets",
                schema: "pay");
        }
    }
}
