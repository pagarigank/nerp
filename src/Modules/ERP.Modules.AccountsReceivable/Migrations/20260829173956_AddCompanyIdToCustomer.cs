using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "ar",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId",
                schema: "ar",
                table: "Customers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_CustomerId",
                schema: "ar",
                table: "Customers",
                columns: new[] { "CompanyId", "CustomerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId_CustomerId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerId",
                schema: "ar",
                table: "Customers",
                column: "CustomerId",
                unique: true);
        }
    }
}
