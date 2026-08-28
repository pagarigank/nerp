using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBillingShippingAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCountry",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingState",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingZipCode",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCountry",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingState",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingZipCode",
                schema: "ar",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAddress",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillingCity",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillingCountry",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillingState",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillingZipCode",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingCity",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingCountry",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingState",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingZipCode",
                schema: "ar",
                table: "Customers");
        }
    }
}
