using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.GeneralLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddConsolidationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsolidationRuns",
                schema: "gl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    FiscalPeriod = table.Column<int>(type: "int", nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConsolidationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntercompanyMappings",
                schema: "gl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_IntercompanyMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationRuns_FiscalPeriodId",
                schema: "gl",
                table: "ConsolidationRuns",
                column: "FiscalPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationRuns_ParentCompanyId",
                schema: "gl",
                table: "ConsolidationRuns",
                column: "ParentCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationRuns_Status",
                schema: "gl",
                table: "ConsolidationRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntercompanyMappings_FromCompanyId",
                schema: "gl",
                table: "IntercompanyMappings",
                column: "FromCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_IntercompanyMappings_IsActive",
                schema: "gl",
                table: "IntercompanyMappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IntercompanyMappings_ToCompanyId",
                schema: "gl",
                table: "IntercompanyMappings",
                column: "ToCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsolidationRuns",
                schema: "gl");

            migrationBuilder.DropTable(
                name: "IntercompanyMappings",
                schema: "gl");
        }
    }
}
