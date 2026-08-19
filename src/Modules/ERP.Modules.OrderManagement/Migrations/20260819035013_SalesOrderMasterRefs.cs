using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class SalesOrderMasterRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PricingRuleId",
                schema: "om",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesOrderTypeId",
                schema: "om",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxCodeId",
                schema: "om",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxExemptionCertificateId",
                schema: "om",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_PricingRuleId",
                schema: "om",
                table: "SalesOrders",
                column: "PricingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesOrderTypeId",
                schema: "om",
                table: "SalesOrders",
                column: "SalesOrderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TaxCodeId",
                schema: "om",
                table: "SalesOrders",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TaxExemptionCertificateId",
                schema: "om",
                table: "SalesOrders",
                column: "TaxExemptionCertificateId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_PricingRules_PricingRuleId",
                schema: "om",
                table: "SalesOrders",
                column: "PricingRuleId",
                principalSchema: "om",
                principalTable: "PricingRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_SalesOrderTypes_SalesOrderTypeId",
                schema: "om",
                table: "SalesOrders",
                column: "SalesOrderTypeId",
                principalSchema: "om",
                principalTable: "SalesOrderTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_TaxCodes_TaxCodeId",
                schema: "om",
                table: "SalesOrders",
                column: "TaxCodeId",
                principalSchema: "om",
                principalTable: "TaxCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_TaxExemptionCertificates_TaxExemptionCertificateId",
                schema: "om",
                table: "SalesOrders",
                column: "TaxExemptionCertificateId",
                principalSchema: "om",
                principalTable: "TaxExemptionCertificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_PricingRules_PricingRuleId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_SalesOrderTypes_SalesOrderTypeId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_TaxCodes_TaxCodeId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_TaxExemptionCertificates_TaxExemptionCertificateId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_PricingRuleId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_SalesOrderTypeId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TaxCodeId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TaxExemptionCertificateId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "PricingRuleId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "SalesOrderTypeId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TaxCodeId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TaxExemptionCertificateId",
                schema: "om",
                table: "SalesOrders");
        }
    }
}
