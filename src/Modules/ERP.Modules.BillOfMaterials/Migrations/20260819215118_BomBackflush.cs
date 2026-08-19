using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.BillOfMaterials.Migrations
{
    /// <inheritdoc />
    public partial class BomBackflush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackflushRecords",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityBuilt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StandardComponentCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualComponentCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackflushRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackflushRecords_BomHeaderId",
                schema: "bom",
                table: "BackflushRecords",
                column: "BomHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_BackflushRecords_BuildOrderId",
                schema: "bom",
                table: "BackflushRecords",
                column: "BuildOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackflushRecords",
                schema: "bom");
        }
    }
}
