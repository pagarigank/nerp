using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ManualCheckComplianceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GrossPay",
                schema: "pay",
                table: "ManualChecks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Is1099",
                schema: "pay",
                table: "ManualChecks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirectDeposit",
                schema: "pay",
                table: "ManualChecks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "NetPay",
                schema: "pay",
                table: "ManualChecks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrossPay",
                schema: "pay",
                table: "ManualChecks");

            migrationBuilder.DropColumn(
                name: "Is1099",
                schema: "pay",
                table: "ManualChecks");

            migrationBuilder.DropColumn(
                name: "IsDirectDeposit",
                schema: "pay",
                table: "ManualChecks");

            migrationBuilder.DropColumn(
                name: "NetPay",
                schema: "pay",
                table: "ManualChecks");
        }
    }
}
