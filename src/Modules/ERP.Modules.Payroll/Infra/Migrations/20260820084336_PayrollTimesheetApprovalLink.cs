using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PayrollTimesheetApprovalLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalRequestId",
                schema: "pay",
                table: "Timesheets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_ApprovalRequestId",
                schema: "pay",
                table: "Timesheets",
                column: "ApprovalRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Timesheets_ApprovalRequestId",
                schema: "pay",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "ApprovalRequestId",
                schema: "pay",
                table: "Timesheets");
        }
    }
}
