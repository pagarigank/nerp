using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class DropShipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DropShipVendorId",
                schema: "om",
                table: "SalesOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDropShip",
                schema: "om",
                table: "SalesOrderLines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropShipVendorId",
                schema: "om",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "IsDropShip",
                schema: "om",
                table: "SalesOrderLines");
        }
    }
}
