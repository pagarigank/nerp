using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class PricingRuleCategoryAndLinePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppliedPricingRuleId",
                schema: "om",
                table: "SalesOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemCategoryId",
                schema: "om",
                table: "SalesOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemCategoryId",
                schema: "om",
                table: "PricingRules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_ItemCategoryId",
                schema: "om",
                table: "PricingRules",
                column: "ItemCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PricingRules_ItemCategoryId",
                schema: "om",
                table: "PricingRules");

            migrationBuilder.DropColumn(
                name: "AppliedPricingRuleId",
                schema: "om",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "ItemCategoryId",
                schema: "om",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "ItemCategoryId",
                schema: "om",
                table: "PricingRules");
        }
    }
}
