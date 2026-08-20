using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class GarnishmentAndAccrual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Garnishments",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    DisposableIncomePercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ArrearsWeeks = table.Column<int>(type: "int", nullable: true),
                    CaseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TerminatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Garnishments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Garnishments_CompanyId",
                schema: "pay",
                table: "Garnishments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Garnishments_EmployeeId",
                schema: "pay",
                table: "Garnishments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Garnishments_EmployeeId_IsActive",
                schema: "pay",
                table: "Garnishments",
                columns: new[] { "EmployeeId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Garnishments",
                schema: "pay");
        }
    }
}
