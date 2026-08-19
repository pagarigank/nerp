using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddItemExpirationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemExpirations",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_ItemExpirations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemExpirationAlerts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemExpirationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    AlertDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemExpirationAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemExpirationAlerts_ItemExpirations_ItemExpirationId",
                        column: x => x.ItemExpirationId,
                        principalSchema: "inv",
                        principalTable: "ItemExpirations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirationAlerts_AlertDate",
                schema: "inv",
                table: "ItemExpirationAlerts",
                column: "AlertDate");

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirationAlerts_AlertType",
                schema: "inv",
                table: "ItemExpirationAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirationAlerts_IsAcknowledged",
                schema: "inv",
                table: "ItemExpirationAlerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirationAlerts_ItemExpirationId",
                schema: "inv",
                table: "ItemExpirationAlerts",
                column: "ItemExpirationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirations_CompanyId_ItemId_WarehouseId",
                schema: "inv",
                table: "ItemExpirations",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirations_ExpirationDate",
                schema: "inv",
                table: "ItemExpirations",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirations_LotId",
                schema: "inv",
                table: "ItemExpirations",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirations_SerialNumberId",
                schema: "inv",
                table: "ItemExpirations",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemExpirations_Status",
                schema: "inv",
                table: "ItemExpirations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemExpirationAlerts",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ItemExpirations",
                schema: "inv");
        }
    }
}
