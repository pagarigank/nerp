using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Reporting.Migrations
{
    /// <inheritdoc />
    public partial class ReportingSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryRetryEntries",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BackoffMinutes = table.Column<double>(type: "float", nullable: false),
                    FailedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NextRetryOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRetryEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportCategories",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_ReportCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportParameterSets",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportParameterSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchIndexEntries",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    IndexedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchIndexEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchIndexSyncState",
                schema: "rpt",
                columns: table => new
                {
                    StringId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastSyncOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecordsIndexed = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchIndexSyncState", x => x.StringId);
                });

            migrationBuilder.CreateTable(
                name: "SearchQueryLogs",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    ModuleFilter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserIdentity = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SearchedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchQueryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncRunLogs",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StagingTable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowsSynced = table.Column<long>(type: "bigint", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRunLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncWatermarks",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StagingTable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastSyncOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalRowsSynced = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncWatermarks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRetryEntries_NextRetryOn",
                schema: "rpt",
                table: "DeliveryRetryEntries",
                column: "NextRetryOn");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRetryEntries_SubscriptionId_Status",
                schema: "rpt",
                table: "DeliveryRetryEntries",
                columns: new[] { "SubscriptionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportCategories_CompanyId_ParentId",
                schema: "rpt",
                table: "ReportCategories",
                columns: new[] { "CompanyId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportParameterSets_CompanyId_ReportDefinitionId",
                schema: "rpt",
                table: "ReportParameterSets",
                columns: new[] { "CompanyId", "ReportDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_Category",
                schema: "rpt",
                table: "SearchIndexEntries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_Module",
                schema: "rpt",
                table: "SearchIndexEntries",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_SourceType_SourceId",
                schema: "rpt",
                table: "SearchIndexEntries",
                columns: new[] { "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueryLogs_Query",
                schema: "rpt",
                table: "SearchQueryLogs",
                column: "Query");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueryLogs_SearchedOn",
                schema: "rpt",
                table: "SearchQueryLogs",
                column: "SearchedOn");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRunLogs_SourceTable",
                schema: "rpt",
                table: "SyncRunLogs",
                column: "SourceTable");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRunLogs_StartedOn",
                schema: "rpt",
                table: "SyncRunLogs",
                column: "StartedOn");

            migrationBuilder.CreateIndex(
                name: "IX_SyncWatermarks_SourceTable",
                schema: "rpt",
                table: "SyncWatermarks",
                column: "SourceTable",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryRetryEntries",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "ReportCategories",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "ReportParameterSets",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "SearchIndexEntries",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "SearchIndexSyncState",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "SearchQueryLogs",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "SyncRunLogs",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "SyncWatermarks",
                schema: "rpt");
        }
    }
}
