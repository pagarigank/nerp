using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddItemRevaluationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LandedCostId",
                schema: "inv",
                table: "LandedCostAllocationLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemRevaluations",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevaluationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RevaluationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    StandardCostAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalAdjustmentValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRevaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemRevaluationLines",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrentStandardCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NewStandardCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRevaluationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRevaluationLines_ItemRevaluations_RevaluationId",
                        column: x => x.RevaluationId,
                        principalSchema: "inv",
                        principalTable: "ItemRevaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocationLines_LandedCostId",
                schema: "inv",
                table: "LandedCostAllocationLines",
                column: "LandedCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevaluationLines_ItemId",
                schema: "inv",
                table: "ItemRevaluationLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevaluationLines_RevaluationId",
                schema: "inv",
                table: "ItemRevaluationLines",
                column: "RevaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevaluationLines_WarehouseId",
                schema: "inv",
                table: "ItemRevaluationLines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevaluations_CompanyId_RevaluationNumber",
                schema: "inv",
                table: "ItemRevaluations",
                columns: new[] { "CompanyId", "RevaluationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevaluations_RevaluationDate",
                schema: "inv",
                table: "ItemRevaluations",
                column: "RevaluationDate");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevaluations_Status",
                schema: "inv",
                table: "ItemRevaluations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemRevaluationLines",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ItemRevaluations",
                schema: "inv");

            migrationBuilder.DropIndex(
                name: "IX_LandedCostAllocationLines_LandedCostId",
                schema: "inv",
                table: "LandedCostAllocationLines");

            migrationBuilder.DropColumn(
                name: "LandedCostId",
                schema: "inv",
                table: "LandedCostAllocationLines");
        }
    }
}
