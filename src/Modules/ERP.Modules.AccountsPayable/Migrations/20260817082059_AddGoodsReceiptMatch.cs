using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsPayable.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodsReceiptMatches",
                schema: "ap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReceivedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OverReceiptFlag = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptMatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptMatches_OverReceiptFlag",
                schema: "ap",
                table: "GoodsReceiptMatches",
                column: "OverReceiptFlag");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptMatches_PurchaseOrderId",
                schema: "ap",
                table: "GoodsReceiptMatches",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptMatches_ReceiptId",
                schema: "ap",
                table: "GoodsReceiptMatches",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptMatches_VendorId",
                schema: "ap",
                table: "GoodsReceiptMatches",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsReceiptMatches",
                schema: "ap");
        }
    }
}
