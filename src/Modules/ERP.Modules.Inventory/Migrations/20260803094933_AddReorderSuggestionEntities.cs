using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddReorderSuggestionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LotId",
                schema: "inv",
                table: "ItemCostLayers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReorderSuggestions",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SuggestionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReorderSuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReorderSuggestionLines",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReorderSuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentOnHand = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrentAllocated = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SafetyStock = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LeadTimeDemand = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SuggestedOrderQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EstimatedStockoutDate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReorderSuggestionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReorderSuggestionLines_ReorderSuggestions_ReorderSuggestionId",
                        column: x => x.ReorderSuggestionId,
                        principalSchema: "inv",
                        principalTable: "ReorderSuggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCostLayers_LotId",
                schema: "inv",
                table: "ItemCostLayers",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestionLines_ItemId",
                schema: "inv",
                table: "ReorderSuggestionLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestionLines_ReorderSuggestionId",
                schema: "inv",
                table: "ReorderSuggestionLines",
                column: "ReorderSuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestionLines_Status",
                schema: "inv",
                table: "ReorderSuggestionLines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestionLines_WarehouseId",
                schema: "inv",
                table: "ReorderSuggestionLines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestions_CompanyId_SuggestionNumber",
                schema: "inv",
                table: "ReorderSuggestions",
                columns: new[] { "CompanyId", "SuggestionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestions_Status",
                schema: "inv",
                table: "ReorderSuggestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestions_SuggestionDate",
                schema: "inv",
                table: "ReorderSuggestions",
                column: "SuggestionDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReorderSuggestionLines",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ReorderSuggestions",
                schema: "inv");

            migrationBuilder.DropIndex(
                name: "IX_ItemCostLayers_LotId",
                schema: "inv",
                table: "ItemCostLayers");

            migrationBuilder.DropColumn(
                name: "LotId",
                schema: "inv",
                table: "ItemCostLayers");
        }
    }
}
