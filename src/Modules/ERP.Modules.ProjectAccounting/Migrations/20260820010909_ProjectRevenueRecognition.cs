using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.ProjectAccounting.Migrations
{
    /// <inheritdoc />
    public partial class ProjectRevenueRecognition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubcontractDate",
                schema: "proj",
                table: "Subcontracts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "AccountingMethod",
                schema: "proj",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AccruedLoss",
                schema: "proj",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "LossAccruedBy",
                schema: "proj",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LossAccruedOn",
                schema: "proj",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subcontracts_Projects_ProjectId",
                schema: "proj",
                table: "Subcontracts",
                column: "ProjectId",
                principalSchema: "proj",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subcontracts_Projects_ProjectId",
                schema: "proj",
                table: "Subcontracts");

            migrationBuilder.DropColumn(
                name: "SubcontractDate",
                schema: "proj",
                table: "Subcontracts");

            migrationBuilder.DropColumn(
                name: "AccountingMethod",
                schema: "proj",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AccruedLoss",
                schema: "proj",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LossAccruedBy",
                schema: "proj",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LossAccruedOn",
                schema: "proj",
                table: "Projects");
        }
    }
}
