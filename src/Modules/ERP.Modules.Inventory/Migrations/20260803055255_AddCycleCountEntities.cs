using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleCountEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CycleCounts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CycleCounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CycleCountLines",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_CycleCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleCountLines_CycleCounts_CycleCountId",
                        column: x => x.CycleCountId,
                        principalSchema: "inv",
                        principalTable: "CycleCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleCountLines_CycleCountId",
                schema: "inv",
                table: "CycleCountLines",
                column: "CycleCountId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleCountLines_ItemId",
                schema: "inv",
                table: "CycleCountLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleCounts_CompanyId_CountNumber",
                schema: "inv",
                table: "CycleCounts",
                columns: new[] { "CompanyId", "CountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleCounts_CountDate",
                schema: "inv",
                table: "CycleCounts",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_CycleCounts_Status",
                schema: "inv",
                table: "CycleCounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CycleCounts_WarehouseId",
                schema: "inv",
                table: "CycleCounts",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CycleCountLines",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "CycleCounts",
                schema: "inv");
        }
    }
}
