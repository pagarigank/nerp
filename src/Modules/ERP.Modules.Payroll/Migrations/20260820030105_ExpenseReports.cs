using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class ExpenseReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseReports",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReimbursedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseReportLines",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GlAccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ClientBillable = table.Column<bool>(type: "bit", nullable: false),
                    MileageMiles = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MileageRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PerDiemDays = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PerDiemRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReportLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_ExpenseReports_ExpenseReportId",
                        column: x => x.ExpenseReportId,
                        principalSchema: "pay",
                        principalTable: "ExpenseReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_ExpenseReportId",
                schema: "pay",
                table: "ExpenseReportLines",
                column: "ExpenseReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_ProjectId",
                schema: "pay",
                table: "ExpenseReportLines",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_CompanyId",
                schema: "pay",
                table: "ExpenseReports",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_EmployeeId",
                schema: "pay",
                table: "ExpenseReports",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_Status",
                schema: "pay",
                table: "ExpenseReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseReportLines",
                schema: "pay");

            migrationBuilder.DropTable(
                name: "ExpenseReports",
                schema: "pay");
        }
    }
}
