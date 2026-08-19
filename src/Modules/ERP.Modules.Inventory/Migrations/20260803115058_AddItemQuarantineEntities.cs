using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddItemQuarantineEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemQuarantines",
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
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuarantineDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuarantinedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleasedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReleaseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemQuarantines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuarantineDispositions",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuarantineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationBinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DispositionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarantineDispositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuarantineDispositions_ItemQuarantines_QuarantineId",
                        column: x => x.QuarantineId,
                        principalSchema: "inv",
                        principalTable: "ItemQuarantines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuarantines_BinId",
                schema: "inv",
                table: "ItemQuarantines",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuarantines_CompanyId_ItemId_WarehouseId",
                schema: "inv",
                table: "ItemQuarantines",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuarantines_LotId",
                schema: "inv",
                table: "ItemQuarantines",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuarantines_QuarantineDate",
                schema: "inv",
                table: "ItemQuarantines",
                column: "QuarantineDate");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuarantines_QuarantinedBy",
                schema: "inv",
                table: "ItemQuarantines",
                column: "QuarantinedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuarantines_SerialNumberId",
                schema: "inv",
                table: "ItemQuarantines",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuarantines_Status",
                schema: "inv",
                table: "ItemQuarantines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_Action",
                schema: "inv",
                table: "QuarantineDispositions",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_DispositionDate",
                schema: "inv",
                table: "QuarantineDispositions",
                column: "DispositionDate");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_PerformedBy",
                schema: "inv",
                table: "QuarantineDispositions",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_QuarantineId",
                schema: "inv",
                table: "QuarantineDispositions",
                column: "QuarantineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuarantineDispositions",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ItemQuarantines",
                schema: "inv");
        }
    }
}
