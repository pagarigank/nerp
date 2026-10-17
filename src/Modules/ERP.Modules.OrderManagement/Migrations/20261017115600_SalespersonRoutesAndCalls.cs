// <copyright file="20261017115600_SalespersonRoutesAndCalls.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class SalespersonRoutesAndCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesCalls",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CallDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsOfflineCreated = table.Column<bool>(type: "bit", nullable: false),
                    ClientCallId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCalls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalespersonRoutes",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    EstimatedTime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SalespersonRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesCallLines",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesCallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCallLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesCallLines_SalesCalls_SalesCallId",
                        column: x => x.SalesCallId,
                        principalSchema: "om",
                        principalTable: "SalesCalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCallLines_ItemId",
                schema: "om",
                table: "SalesCallLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCallLines_SalesCallId",
                schema: "om",
                table: "SalesCallLines",
                column: "SalesCallId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCalls_ClientCallId",
                schema: "om",
                table: "SalesCalls",
                column: "ClientCallId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCalls_CompanyId_CallNumber",
                schema: "om",
                table: "SalesCalls",
                columns: new[] { "CompanyId", "CallNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCalls_CompanyId_SalesRepId_CallDate",
                schema: "om",
                table: "SalesCalls",
                columns: new[] { "CompanyId", "SalesRepId", "CallDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCalls_CustomerId",
                schema: "om",
                table: "SalesCalls",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalespersonRoutes_CompanyId_SalesRepId_DayOfWeek_CustomerId",
                schema: "om",
                table: "SalespersonRoutes",
                columns: new[] { "CompanyId", "SalesRepId", "DayOfWeek", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalespersonRoutes_CompanyId_SalesRepId_DayOfWeek_SequenceOrder",
                schema: "om",
                table: "SalespersonRoutes",
                columns: new[] { "CompanyId", "SalesRepId", "DayOfWeek", "SequenceOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SalespersonRoutes_CustomerId",
                schema: "om",
                table: "SalespersonRoutes",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesCallLines",
                schema: "om");

            migrationBuilder.DropTable(
                name: "SalespersonRoutes",
                schema: "om");

            migrationBuilder.DropTable(
                name: "SalesCalls",
                schema: "om");
        }
    }
}
