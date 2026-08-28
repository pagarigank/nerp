using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Reporting.Migrations
{
    /// <inheritdoc />
    public partial class Phase13ReportingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rpt");

            migrationBuilder.CreateTable(
                name: "DashboardWidgets",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DashboardId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WidgetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataSourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataSourceConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionX = table.Column<int>(type: "int", nullable: false),
                    PositionY = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    RefreshIntervalSeconds = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DashboardWidgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialStatementLayouts",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RowDefinitionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnDefinitionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TreeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuppressZero = table.Column<bool>(type: "bit", nullable: false),
                    RoundToNearestDollar = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_FinancialStatementLayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuickQueries",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColumnSelectionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludeArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUser = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    LastRunOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_QuickQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SqlQuery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LayoutJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportSubscriptions",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExportFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScheduleConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastRunOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastRunStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastRunError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_ReportSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportUsageLogs",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReportDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SavedQueryId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutedByUser = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExportFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExecutedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportUsageLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedQueries",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QueryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColumnSelectionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    LastRunOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SavedQueries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgets_CompanyId_DashboardId",
                schema: "rpt",
                table: "DashboardWidgets",
                columns: new[] { "CompanyId", "DashboardId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementLayouts_CompanyId_Name",
                schema: "rpt",
                table: "FinancialStatementLayouts",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_QuickQueries_CompanyId_Name",
                schema: "rpt",
                table: "QuickQueries",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_CompanyId_Module",
                schema: "rpt",
                table: "ReportDefinitions",
                columns: new[] { "CompanyId", "Module" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_CompanyId_Name",
                schema: "rpt",
                table: "ReportDefinitions",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportSubscriptions_CompanyId_ReportDefinitionId",
                schema: "rpt",
                table: "ReportSubscriptions",
                columns: new[] { "CompanyId", "ReportDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportUsageLogs_CompanyId",
                schema: "rpt",
                table: "ReportUsageLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportUsageLogs_ExecutedOn",
                schema: "rpt",
                table: "ReportUsageLogs",
                column: "ExecutedOn");

            migrationBuilder.CreateIndex(
                name: "IX_ReportUsageLogs_ReportDefinitionId_ExecutedOn",
                schema: "rpt",
                table: "ReportUsageLogs",
                columns: new[] { "ReportDefinitionId", "ExecutedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedQueries_CompanyId_Name",
                schema: "rpt",
                table: "SavedQueries",
                columns: new[] { "CompanyId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardWidgets",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "FinancialStatementLayouts",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "QuickQueries",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "ReportDefinitions",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "ReportSubscriptions",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "ReportUsageLogs",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "SavedQueries",
                schema: "rpt");
        }
    }
}
