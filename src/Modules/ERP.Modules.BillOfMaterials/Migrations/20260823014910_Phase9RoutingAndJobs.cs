using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.BillOfMaterials.Migrations
{
    /// <inheritdoc />
    public partial class Phase9RoutingAndJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoutingOperationId",
                schema: "bom",
                table: "BomComponentLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoutingOperations",
                schema: "bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StandardSetupTimeMinutes = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StandardRunTimeMinutesPerUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_RoutingOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_CompanyId_OperationCode",
                schema: "bom",
                table: "RoutingOperations",
                columns: new[] { "CompanyId", "OperationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_WorkCenterId",
                schema: "bom",
                table: "RoutingOperations",
                column: "WorkCenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoutingOperations",
                schema: "bom");

            migrationBuilder.DropColumn(
                name: "RoutingOperationId",
                schema: "bom",
                table: "BomComponentLines");
        }
    }
}
