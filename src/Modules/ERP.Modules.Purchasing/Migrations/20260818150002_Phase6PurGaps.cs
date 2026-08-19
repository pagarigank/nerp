using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Purchasing.Migrations
{
    /// <inheritdoc />
    public partial class Phase6PurGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BlanketAmountLimit",
                schema: "pur",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailedToVendorDate",
                schema: "pur",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FreightAmount",
                schema: "pur",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreightTaxAmount",
                schema: "pur",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintedDate",
                schema: "pur",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReleasedAmount",
                schema: "pur",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TaxExempt",
                schema: "pur",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                schema: "pur",
                table: "PurchaseOrderLines",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                schema: "pur",
                table: "PurchaseOrderLines",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "VendorQuotes",
                schema: "pur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RfxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuoteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuoteFreight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorQuoteLines",
                schema: "pur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorQuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorQuoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorQuoteLines_VendorQuotes_VendorQuoteId",
                        column: x => x.VendorQuoteId,
                        principalSchema: "pur",
                        principalTable: "VendorQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuoteLines_VendorQuoteId",
                schema: "pur",
                table: "VendorQuoteLines",
                column: "VendorQuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotes_CompanyId_RfxNumber",
                schema: "pur",
                table: "VendorQuotes",
                columns: new[] { "CompanyId", "RfxNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorQuoteLines",
                schema: "pur");

            migrationBuilder.DropTable(
                name: "VendorQuotes",
                schema: "pur");

            migrationBuilder.DropColumn(
                name: "BlanketAmountLimit",
                schema: "pur",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "EmailedToVendorDate",
                schema: "pur",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FreightAmount",
                schema: "pur",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FreightTaxAmount",
                schema: "pur",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PrintedDate",
                schema: "pur",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReleasedAmount",
                schema: "pur",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxExempt",
                schema: "pur",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                schema: "pur",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                schema: "pur",
                table: "PurchaseOrderLines");
        }
    }
}
