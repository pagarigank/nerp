using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class Phase8GapFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedOrderId",
                schema: "om",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsQuote",
                schema: "om",
                table: "SalesOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuoteAcceptedDate",
                schema: "om",
                table: "SalesOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuoteExpiryDate",
                schema: "om",
                table: "SalesOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuoteSentDate",
                schema: "om",
                table: "SalesOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuoteStatus",
                schema: "om",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                schema: "om",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedFreight",
                schema: "om",
                table: "SalesOrderLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BackorderSubstitutionOffers",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstituteItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ApprovedUnitPrice = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RespondedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackorderSubstitutionOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlanketSalesOrders",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlanketSalesOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReturnToVendors",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShippedToVendorDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchasingReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnToVendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderChangeHistories",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderChangeHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderNotes",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsCustomerFacing = table.Column<bool>(type: "bit", nullable: false),
                    NoteType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttachmentLink = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlanketReleases",
                schema: "om",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlanketOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedSalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlanketReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlanketReleases_BlanketSalesOrders_BlanketOrderId",
                        column: x => x.BlanketOrderId,
                        principalSchema: "om",
                        principalTable: "BlanketSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackorderSubstitutionOffers_CompanyId_SalesOrderId",
                schema: "om",
                table: "BackorderSubstitutionOffers",
                columns: new[] { "CompanyId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackorderSubstitutionOffers_SalesOrderLineId",
                schema: "om",
                table: "BackorderSubstitutionOffers",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_BlanketReleases_BlanketOrderId",
                schema: "om",
                table: "BlanketReleases",
                column: "BlanketOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BlanketSalesOrders_CompanyId_OrderNumber",
                schema: "om",
                table: "BlanketSalesOrders",
                columns: new[] { "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlanketSalesOrders_CustomerId",
                schema: "om",
                table: "BlanketSalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BlanketSalesOrders_Status",
                schema: "om",
                table: "BlanketSalesOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnToVendors_ReturnId",
                schema: "om",
                table: "ReturnToVendors",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnToVendors_VendorId",
                schema: "om",
                table: "ReturnToVendors",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderChangeHistories_CompanyId_SalesOrderId",
                schema: "om",
                table: "SalesOrderChangeHistories",
                columns: new[] { "CompanyId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderNotes_CompanyId_SalesOrderId",
                schema: "om",
                table: "SalesOrderNotes",
                columns: new[] { "CompanyId", "SalesOrderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackorderSubstitutionOffers",
                schema: "om");

            migrationBuilder.DropTable(
                name: "BlanketReleases",
                schema: "om");

            migrationBuilder.DropTable(
                name: "ReturnToVendors",
                schema: "om");

            migrationBuilder.DropTable(
                name: "SalesOrderChangeHistories",
                schema: "om");

            migrationBuilder.DropTable(
                name: "SalesOrderNotes",
                schema: "om");

            migrationBuilder.DropTable(
                name: "BlanketSalesOrders",
                schema: "om");

            migrationBuilder.DropColumn(
                name: "ConvertedOrderId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "IsQuote",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "QuoteAcceptedDate",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "QuoteExpiryDate",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "QuoteSentDate",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "QuoteStatus",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "RevisionNumber",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "AllocatedFreight",
                schema: "om",
                table: "SalesOrderLines");
        }
    }
}
