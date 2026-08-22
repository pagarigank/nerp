using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class Phase6DropShipConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_PricingRules_PricingRuleId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_PricingRuleId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "PricingRuleId",
                schema: "om",
                table: "SalesOrders");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DropShipConfirmedOn",
                schema: "om",
                table: "SalesOrderLines",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropShipConfirmedOn",
                schema: "om",
                table: "SalesOrderLines");

            migrationBuilder.AddColumn<Guid>(
                name: "PricingRuleId",
                schema: "om",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_PricingRuleId",
                schema: "om",
                table: "SalesOrders",
                column: "PricingRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_PricingRules_PricingRuleId",
                schema: "om",
                table: "SalesOrders",
                column: "PricingRuleId",
                principalSchema: "om",
                principalTable: "PricingRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
