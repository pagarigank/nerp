using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddItemVendorAndGLAccountDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemGLAccountDefaults",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryAssetAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    COGSAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VarianceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchasePriceVarianceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesRevenueAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventoryAdjustmentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LandedCostClearingAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGLAccountDefaults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemVendorAssignments",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimaryVendor = table.Column<bool>(type: "bit", nullable: false),
                    VendorItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VendorCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    MinimumOrderQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemVendorAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemGLAccountDefaults_ItemId",
                schema: "inv",
                table: "ItemGLAccountDefaults",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVendorAssignments_ItemId_VendorId",
                schema: "inv",
                table: "ItemVendorAssignments",
                columns: new[] { "ItemId", "VendorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVendorAssignments_VendorId",
                schema: "inv",
                table: "ItemVendorAssignments",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemGLAccountDefaults",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ItemVendorAssignments",
                schema: "inv");
        }
    }
}
