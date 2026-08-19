using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.BillOfMaterials.Migrations
{
    /// <inheritdoc />
    public partial class Phase9BomModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bom");

            migrationBuilder.CreateTable(
                name: "BomHeaders",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BomType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    YieldPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EstimatedMaterialCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    EstimatedLaborCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    EstimatedOverheadCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuildOrders",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    BomHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityToBuild = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActualYield = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TotalMaterialCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalLaborCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalOverheadCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CapacityHoursPerDay = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EfficiencyPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CostRatePerHour = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BomComponentLines",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityPerParent = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScrapFactor = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OperationSequence = table.Column<int>(type: "int", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPhantom = table.Column<bool>(type: "bit", nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomComponentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomComponentLines_BomHeaders_BomHeaderId",
                        column: x => x.BomHeaderId,
                        principalSchema: "bom",
                        principalTable: "BomHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomRevisionHistories",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangeDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReasonForChange = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomRevisionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomRevisionHistories_BomHeaders_BomHeaderId",
                        column: x => x.BomHeaderId,
                        principalSchema: "bom",
                        principalTable: "BomHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuildOrderLines",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantityIssued = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExtendedCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsLabor = table.Column<bool>(type: "bit", nullable: false),
                    IsOverhead = table.Column<bool>(type: "bit", nullable: false),
                    VarianceQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    VarianceCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
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
                    table.PrimaryKey("PK_BuildOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildOrderLines_BuildOrders_BuildOrderId",
                        column: x => x.BuildOrderId,
                        principalSchema: "bom",
                        principalTable: "BuildOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomComponentLines_BomHeaderId",
                schema: "bom",
                table: "BomComponentLines",
                column: "BomHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_BomComponentLines_ComponentItemId",
                schema: "bom",
                table: "BomComponentLines",
                column: "ComponentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeaders_CompanyId_ParentItemId_Revision",
                schema: "bom",
                table: "BomHeaders",
                columns: new[] { "CompanyId", "ParentItemId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomHeaders_Status",
                schema: "bom",
                table: "BomHeaders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BomRevisionHistories_BomHeaderId",
                schema: "bom",
                table: "BomRevisionHistories",
                column: "BomHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildOrderLines_BuildOrderId",
                schema: "bom",
                table: "BuildOrderLines",
                column: "BuildOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildOrderLines_ComponentItemId",
                schema: "bom",
                table: "BuildOrderLines",
                column: "ComponentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildOrders_BuildDate",
                schema: "bom",
                table: "BuildOrders",
                column: "BuildDate");

            migrationBuilder.CreateIndex(
                name: "IX_BuildOrders_CompanyId_BuildNumber",
                schema: "bom",
                table: "BuildOrders",
                columns: new[] { "CompanyId", "BuildNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildOrders_ParentItemId",
                schema: "bom",
                table: "BuildOrders",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildOrders_Status",
                schema: "bom",
                table: "BuildOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_CompanyId_Code",
                schema: "bom",
                table: "WorkCenters",
                columns: new[] { "CompanyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomComponentLines",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "BomRevisionHistories",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "BuildOrderLines",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "WorkCenters",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "BomHeaders",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "BuildOrders",
                schema: "bom");
        }
    }
}
