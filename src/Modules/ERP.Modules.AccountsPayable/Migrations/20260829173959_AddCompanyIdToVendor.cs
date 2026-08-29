using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsPayable.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendors_VendorId",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "ap",
                table: "Vendors",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_CompanyId",
                schema: "ap",
                table: "Vendors",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_CompanyId_VendorId",
                schema: "ap",
                table: "Vendors",
                columns: new[] { "CompanyId", "VendorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendors_CompanyId",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_CompanyId_VendorId",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorId",
                schema: "ap",
                table: "Vendors",
                column: "VendorId",
                unique: true);
        }
    }
}
