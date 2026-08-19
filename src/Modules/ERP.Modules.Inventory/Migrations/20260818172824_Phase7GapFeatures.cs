using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class Phase7GapFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemStocks_CompanyId_ItemId_WarehouseId_BinId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.AddColumn<Guid>(
                name: "LotId",
                schema: "inv",
                table: "ItemStocks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfOrigin",
                schema: "inv",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HazardClass",
                schema: "inv",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                schema: "inv",
                table: "Items",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HsCode",
                schema: "inv",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHazardousMaterial",
                schema: "inv",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsKit",
                schema: "inv",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                schema: "inv",
                table: "Items",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageCondition",
                schema: "inv",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                schema: "inv",
                table: "Items",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeightUnit",
                schema: "inv",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                schema: "inv",
                table: "Items",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsignmentStocks",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ConsignmentCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignmentStocks_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inv",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsignmentStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryValuationSnapshots",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StandardCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StandardValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AverageValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryValuationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemSubstitutions",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstituteItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_ItemSubstitutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSubstitutions_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inv",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemSubstitutions_Items_SubstituteItemId",
                        column: x => x.SubstituteItemId,
                        principalSchema: "inv",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitComponents",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KitItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityPerKit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitComponents_Items_ComponentItemId",
                        column: x => x.ComponentItemId,
                        principalSchema: "inv",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitComponents_Items_KitItemId",
                        column: x => x.KitItemId,
                        principalSchema: "inv",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LotExpirationAlerts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    AlertDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_LotExpirationAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PutAwayPickingRules",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PutAwayRank = table.Column<int>(type: "int", nullable: false),
                    PickSequence = table.Column<int>(type: "int", nullable: false),
                    PickingPolicy = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutAwayPickingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PutAwayPickingRules_WarehouseBins_BinId",
                        column: x => x.BinId,
                        principalSchema: "inv",
                        principalTable: "WarehouseBins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PutAwayPickingRules_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReorderAlerts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentOnHand = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_ReorderAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlowMovingAlerts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DaysSinceLastMovement = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SlowMovingAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_WarehouseId",
                schema: "inv",
                table: "Lots",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStocks_BinId",
                schema: "inv",
                table: "ItemStocks",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStocks_CompanyId_ItemId_WarehouseId_BinId_LotId",
                schema: "inv",
                table: "ItemStocks",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId", "BinId", "LotId" },
                unique: true,
                filter: "[BinId] IS NOT NULL AND [LotId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStocks_ItemId",
                schema: "inv",
                table: "ItemStocks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStocks_WarehouseId",
                schema: "inv",
                table: "ItemStocks",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentStocks_CompanyId_VendorId_ItemId_WarehouseId",
                schema: "inv",
                table: "ConsignmentStocks",
                columns: new[] { "CompanyId", "VendorId", "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentStocks_ItemId",
                schema: "inv",
                table: "ConsignmentStocks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentStocks_WarehouseId",
                schema: "inv",
                table: "ConsignmentStocks",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationSnapshots_CompanyId_ItemId_WarehouseId_SnapshotDate",
                schema: "inv",
                table: "InventoryValuationSnapshots",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationSnapshots_SnapshotDate",
                schema: "inv",
                table: "InventoryValuationSnapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSubstitutions_CompanyId_ItemId",
                schema: "inv",
                table: "ItemSubstitutions",
                columns: new[] { "CompanyId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSubstitutions_ItemId",
                schema: "inv",
                table: "ItemSubstitutions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSubstitutions_SubstituteItemId",
                schema: "inv",
                table: "ItemSubstitutions",
                column: "SubstituteItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponents_CompanyId_KitItemId",
                schema: "inv",
                table: "KitComponents",
                columns: new[] { "CompanyId", "KitItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_KitComponents_ComponentItemId",
                schema: "inv",
                table: "KitComponents",
                column: "ComponentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponents_KitItemId",
                schema: "inv",
                table: "KitComponents",
                column: "KitItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LotExpirationAlerts_AlertDate",
                schema: "inv",
                table: "LotExpirationAlerts",
                column: "AlertDate");

            migrationBuilder.CreateIndex(
                name: "IX_LotExpirationAlerts_AlertType",
                schema: "inv",
                table: "LotExpirationAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_LotExpirationAlerts_CompanyId_LotId",
                schema: "inv",
                table: "LotExpirationAlerts",
                columns: new[] { "CompanyId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_LotExpirationAlerts_IsAcknowledged",
                schema: "inv",
                table: "LotExpirationAlerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayPickingRules_BinId",
                schema: "inv",
                table: "PutAwayPickingRules",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayPickingRules_CompanyId_WarehouseId_BinId",
                schema: "inv",
                table: "PutAwayPickingRules",
                columns: new[] { "CompanyId", "WarehouseId", "BinId" });

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayPickingRules_WarehouseId",
                schema: "inv",
                table: "PutAwayPickingRules",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderAlerts_AlertDate",
                schema: "inv",
                table: "ReorderAlerts",
                column: "AlertDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderAlerts_CompanyId_ItemId_WarehouseId",
                schema: "inv",
                table: "ReorderAlerts",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReorderAlerts_IsAcknowledged",
                schema: "inv",
                table: "ReorderAlerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_SlowMovingAlerts_AlertDate",
                schema: "inv",
                table: "SlowMovingAlerts",
                column: "AlertDate");

            migrationBuilder.CreateIndex(
                name: "IX_SlowMovingAlerts_CompanyId_ItemId_WarehouseId",
                schema: "inv",
                table: "SlowMovingAlerts",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlowMovingAlerts_IsAcknowledged",
                schema: "inv",
                table: "SlowMovingAlerts",
                column: "IsAcknowledged");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStocks_Items_ItemId",
                schema: "inv",
                table: "ItemStocks",
                column: "ItemId",
                principalSchema: "inv",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStocks_WarehouseBins_BinId",
                schema: "inv",
                table: "ItemStocks",
                column: "BinId",
                principalSchema: "inv",
                principalTable: "WarehouseBins",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStocks_Warehouses_WarehouseId",
                schema: "inv",
                table: "ItemStocks",
                column: "WarehouseId",
                principalSchema: "inv",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lots_Items_ItemId",
                schema: "inv",
                table: "Lots",
                column: "ItemId",
                principalSchema: "inv",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lots_Warehouses_WarehouseId",
                schema: "inv",
                table: "Lots",
                column: "WarehouseId",
                principalSchema: "inv",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemStocks_Items_ItemId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStocks_WarehouseBins_BinId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStocks_Warehouses_WarehouseId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Lots_Items_ItemId",
                schema: "inv",
                table: "Lots");

            migrationBuilder.DropForeignKey(
                name: "FK_Lots_Warehouses_WarehouseId",
                schema: "inv",
                table: "Lots");

            migrationBuilder.DropTable(
                name: "ConsignmentStocks",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "InventoryValuationSnapshots",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ItemSubstitutions",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "KitComponents",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "LotExpirationAlerts",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "PutAwayPickingRules",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ReorderAlerts",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "SlowMovingAlerts",
                schema: "inv");

            migrationBuilder.DropIndex(
                name: "IX_Lots_WarehouseId",
                schema: "inv",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_ItemStocks_BinId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropIndex(
                name: "IX_ItemStocks_CompanyId_ItemId_WarehouseId_BinId_LotId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropIndex(
                name: "IX_ItemStocks_ItemId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropIndex(
                name: "IX_ItemStocks_WarehouseId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropColumn(
                name: "LotId",
                schema: "inv",
                table: "ItemStocks");

            migrationBuilder.DropColumn(
                name: "CountryOfOrigin",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "HazardClass",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Height",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "HsCode",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsHazardousMaterial",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsKit",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Length",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "StorageCondition",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Weight",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "WeightUnit",
                schema: "inv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Width",
                schema: "inv",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStocks_CompanyId_ItemId_WarehouseId_BinId",
                schema: "inv",
                table: "ItemStocks",
                columns: new[] { "CompanyId", "ItemId", "WarehouseId", "BinId" },
                unique: true,
                filter: "[BinId] IS NOT NULL");
        }
    }
}
