using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PayrollDirectDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DirectDeposits",
                schema: "pay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoutingNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountNumberEncrypted = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsRemainder = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectDeposits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectDeposits_CompanyId_EmployeeId",
                schema: "pay",
                table: "DirectDeposits",
                columns: new[] { "CompanyId", "EmployeeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectDeposits",
                schema: "pay");
        }
    }
}
