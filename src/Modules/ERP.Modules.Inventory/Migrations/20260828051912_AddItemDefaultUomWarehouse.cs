using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddItemDefaultUomWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultUnitOfMeasure",
                schema: "inv",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultWarehouseId",
                schema: "inv",
                table: "Items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE [inv].[Items] SET [DefaultUnitOfMeasure] = [BaseUnitOfMeasure] WHERE [DefaultUnitOfMeasure] = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultUnitOfMeasure",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DefaultWarehouseId",
                schema: "inv",
                table: "Items");
        }
    }
}
