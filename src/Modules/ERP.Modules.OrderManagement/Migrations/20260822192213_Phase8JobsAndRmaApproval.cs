using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class Phase8JobsAndRmaApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredOn",
                schema: "om",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BackorderReleasedOn",
                schema: "om",
                table: "SalesOrderLines",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "om",
                table: "Returns",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                schema: "om",
                table: "Returns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "om",
                table: "Returns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommissionRuns",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRunLines",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevenueBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRunLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionRunLines_CommissionRuns_CommissionRunId",
                        column: x => x.CommissionRunId,
                        principalSchema: "om",
                        principalTable: "CommissionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRunLines_CommissionRunId",
                schema: "om",
                table: "CommissionRunLines",
                column: "CommissionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRunLines_PeriodStart_SalesRepId",
                schema: "om",
                table: "CommissionRunLines",
                columns: new[] { "PeriodStart", "SalesRepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuns_PeriodStart",
                schema: "om",
                table: "CommissionRuns",
                column: "PeriodStart",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuns_RunNumber",
                schema: "om",
                table: "CommissionRuns",
                column: "RunNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionRunLines",
                schema: "om");

            migrationBuilder.DropTable(
                name: "CommissionRuns",
                schema: "om");

            migrationBuilder.DropColumn(
                name: "DeliveredOn",
                schema: "om",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "BackorderReleasedOn",
                schema: "om",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "om",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                schema: "om",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "om",
                table: "Returns");
        }
    }
}
