using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalCountEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhysicalCounts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BlindCount = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalCounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalCountLines",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhysicalCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalCountLines_PhysicalCounts_PhysicalCountId",
                        column: x => x.PhysicalCountId,
                        principalSchema: "inv",
                        principalTable: "PhysicalCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountLines_ItemId",
                schema: "inv",
                table: "PhysicalCountLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountLines_PhysicalCountId",
                schema: "inv",
                table: "PhysicalCountLines",
                column: "PhysicalCountId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCounts_CompanyId_CountNumber",
                schema: "inv",
                table: "PhysicalCounts",
                columns: new[] { "CompanyId", "CountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCounts_CountDate",
                schema: "inv",
                table: "PhysicalCounts",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCounts_Status",
                schema: "inv",
                table: "PhysicalCounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCounts_WarehouseId",
                schema: "inv",
                table: "PhysicalCounts",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhysicalCountLines",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "PhysicalCounts",
                schema: "inv");
        }
    }
}
