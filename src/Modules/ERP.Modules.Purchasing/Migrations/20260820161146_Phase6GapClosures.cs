using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Purchasing.Migrations
{
    /// <inheritdoc />
    public partial class Phase6GapClosures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OverReceiptApprovals",
                schema: "pur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OverReceiptTolerance = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverReceiptApprovals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptsWithoutPO",
                schema: "pur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PackingSlipNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptsWithoutPO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptsWithoutPOLines",
                schema: "pur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptsWithoutPOLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptsWithoutPOLines_ReceiptsWithoutPO_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "pur",
                        principalTable: "ReceiptsWithoutPO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OverReceiptApprovals_ReceiptId",
                schema: "pur",
                table: "OverReceiptApprovals",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsWithoutPO_CompanyId_ReceiptNumber",
                schema: "pur",
                table: "ReceiptsWithoutPO",
                columns: new[] { "CompanyId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsWithoutPOLines_ReceiptId_LineNumber",
                schema: "pur",
                table: "ReceiptsWithoutPOLines",
                columns: new[] { "ReceiptId", "LineNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OverReceiptApprovals",
                schema: "pur");

            migrationBuilder.DropTable(
                name: "ReceiptsWithoutPOLines",
                schema: "pur");

            migrationBuilder.DropTable(
                name: "ReceiptsWithoutPO",
                schema: "pur");
        }
    }
}
