using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class SalesOrderDiscountApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DiscountApproved",
                schema: "om",
                table: "SalesOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DiscountApprovedBy",
                schema: "om",
                table: "SalesOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDiscountApproval",
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
                name: "DiscountApproved",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "DiscountApprovedBy",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "RequiresDiscountApproval",
                schema: "om",
                table: "SalesOrders");
        }
    }
}
