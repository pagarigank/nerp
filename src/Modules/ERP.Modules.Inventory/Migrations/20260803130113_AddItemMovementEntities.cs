using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddItemMovementEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemMovements",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemMovements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemMovements_CompanyId_ItemId_WarehouseId",
                schema: "inv",
                table: "ItemMovements",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemMovements_CreatedBy",
                schema: "inv",
                table: "ItemMovements",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMovements_LotId",
                schema: "inv",
                table: "ItemMovements",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMovements_MovementDate",
                schema: "inv",
                table: "ItemMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMovements_MovementType",
                schema: "inv",
                table: "ItemMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMovements_ReferenceNumber",
                schema: "inv",
                table: "ItemMovements",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMovements_SerialNumberId",
                schema: "inv",
                table: "ItemMovements",
                column: "SerialNumberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemMovements",
                schema: "inv");
        }
    }
}
