using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.ProjectAccounting.Migrations
{
    /// <inheritdoc />
    public partial class ProjectLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillingApprovedBy",
                schema: "proj",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingApprovedOn",
                schema: "proj",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimateAtCompletion",
                schema: "proj",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCloseOutComplete",
                schema: "proj",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingApprovedBy",
                schema: "proj",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BillingApprovedOn",
                schema: "proj",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EstimateAtCompletion",
                schema: "proj",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsCloseOutComplete",
                schema: "proj",
                table: "Projects");
        }
    }
}
