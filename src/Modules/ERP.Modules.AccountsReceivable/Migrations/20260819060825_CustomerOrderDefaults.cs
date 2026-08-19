using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class CustomerOrderDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SalesRepId",
                schema: "ar",
                table: "Customers",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxCodeId",
                schema: "ar",
                table: "Customers",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxExemptionCertificateId",
                schema: "ar",
                table: "Customers",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SalesRepId",
                schema: "ar",
                table: "Customers",
                column: "SalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxCodeId",
                schema: "ar",
                table: "Customers",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxExemptionCertificateId",
                schema: "ar",
                table: "Customers",
                column: "TaxExemptionCertificateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_TaxExemptionCertificateId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TaxCodeId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_SalesRepId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxExemptionCertificateId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxCodeId",
                schema: "ar",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SalesRepId",
                schema: "ar",
                table: "Customers");
        }
    }
}
