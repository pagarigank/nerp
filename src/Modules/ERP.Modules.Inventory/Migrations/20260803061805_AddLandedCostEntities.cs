using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddLandedCostEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LandedCostAllocations",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_LandedCostAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LandedCosts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CostType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandedCosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LandedCostAllocationLines",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandedCostAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AllocationMethod = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandedCostAllocationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandedCostAllocationLines_LandedCostAllocations_LandedCostAllocationId",
                        column: x => x.LandedCostAllocationId,
                        principalSchema: "inv",
                        principalTable: "LandedCostAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocationLines_ItemId",
                schema: "inv",
                table: "LandedCostAllocationLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocationLines_LandedCostAllocationId",
                schema: "inv",
                table: "LandedCostAllocationLines",
                column: "LandedCostAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_AllocationDate",
                schema: "inv",
                table: "LandedCostAllocations",
                column: "AllocationDate");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_CompanyId_AllocationNumber",
                schema: "inv",
                table: "LandedCostAllocations",
                columns: new[] { "CompanyId", "AllocationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_ReceiptTransactionId",
                schema: "inv",
                table: "LandedCostAllocations",
                column: "ReceiptTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_Status",
                schema: "inv",
                table: "LandedCostAllocations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCosts_CompanyId_CostCode",
                schema: "inv",
                table: "LandedCosts",
                columns: new[] { "CompanyId", "CostCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LandedCosts_CostDate",
                schema: "inv",
                table: "LandedCosts",
                column: "CostDate");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCosts_Status",
                schema: "inv",
                table: "LandedCosts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCosts_VendorId",
                schema: "inv",
                table: "LandedCosts",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LandedCostAllocationLines",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "LandedCosts",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "LandedCostAllocations",
                schema: "inv");
        }
    }
}
