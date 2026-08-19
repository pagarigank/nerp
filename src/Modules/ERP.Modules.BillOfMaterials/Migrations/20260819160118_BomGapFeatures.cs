using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.BillOfMaterials.Migrations
{
    /// <inheritdoc />
    public partial class BomGapFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternateCode",
                schema: "bom",
                table: "BomHeaders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BomComponentSubstitutions",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstituteItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CostVariance = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomComponentSubstitutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomComponentSubstitutions_BomHeaders_BomHeaderId",
                        column: x => x.BomHeaderId,
                        principalSchema: "bom",
                        principalTable: "BomHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentAllocations",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FulfilledQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsReleased = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentAllocations_BomHeaders_BomHeaderId",
                        column: x => x.BomHeaderId,
                        principalSchema: "bom",
                        principalTable: "BomHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EngineeringChangeNotices",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EcnNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PlannedEffectivity = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEffectivity = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reviewer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Approver = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineeringChangeNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EngineeringChangeNotices_BomHeaders_BomHeaderId",
                        column: x => x.BomHeaderId,
                        principalSchema: "bom",
                        principalTable: "BomHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomComponentSubstitutions_BomHeaderId",
                schema: "bom",
                table: "BomComponentSubstitutions",
                column: "BomHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_BomComponentSubstitutions_ComponentLineId",
                schema: "bom",
                table: "BomComponentSubstitutions",
                column: "ComponentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentAllocations_BomHeaderId",
                schema: "bom",
                table: "ComponentAllocations",
                column: "BomHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentAllocations_BuildOrderId",
                schema: "bom",
                table: "ComponentAllocations",
                column: "BuildOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentAllocations_ComponentItemId",
                schema: "bom",
                table: "ComponentAllocations",
                column: "ComponentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EngineeringChangeNotices_BomHeaderId",
                schema: "bom",
                table: "EngineeringChangeNotices",
                column: "BomHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_EngineeringChangeNotices_CompanyId_EcnNumber",
                schema: "bom",
                table: "EngineeringChangeNotices",
                columns: new[] { "CompanyId", "EcnNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomComponentSubstitutions",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "ComponentAllocations",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "EngineeringChangeNotices",
                schema: "bom");

            migrationBuilder.DropColumn(
                name: "AlternateCode",
                schema: "bom",
                table: "BomHeaders");
        }
    }
}
