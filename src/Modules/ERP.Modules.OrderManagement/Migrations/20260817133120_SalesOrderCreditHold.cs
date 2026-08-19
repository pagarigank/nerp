using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class SalesOrderCreditHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditHoldReason",
                schema: "om",
                table: "SalesOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnCreditHold",
                schema: "om",
                table: "SalesOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditHoldReason",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "IsOnCreditHold",
                schema: "om",
                table: "SalesOrders");
        }
    }
}
