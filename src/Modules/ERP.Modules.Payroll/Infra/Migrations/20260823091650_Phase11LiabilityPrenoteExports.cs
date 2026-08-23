using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Phase11LiabilityPrenoteExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaidVoucherId",
                schema: "pay",
                table: "TaxDepositSchedules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedOn",
                schema: "pay",
                table: "NewHireReportingConfigs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                schema: "pay",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "pay",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "pay",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                schema: "pay",
                table: "Employees",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PrenoteSentOn",
                schema: "pay",
                table: "DirectDeposits",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedOn",
                schema: "pay",
                table: "DirectDeposits",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BenefitRemittancePaidThrough",
                schema: "pay",
                table: "CompanyPayrollSetups",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenAccrualAmount",
                schema: "pay",
                table: "CompanyPayrollSetups",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenAccrualBatchRef",
                schema: "pay",
                table: "CompanyPayrollSetups",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenAccrualEmployerTax",
                schema: "pay",
                table: "CompanyPayrollSetups",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OpenAccrualPostedOn",
                schema: "pay",
                table: "CompanyPayrollSetups",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidVoucherId",
                schema: "pay",
                table: "TaxDepositSchedules");

            migrationBuilder.DropColumn(
                name: "SubmittedOn",
                schema: "pay",
                table: "NewHireReportingConfigs");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                schema: "pay",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "pay",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "pay",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StateCode",
                schema: "pay",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PrenoteSentOn",
                schema: "pay",
                table: "DirectDeposits");

            migrationBuilder.DropColumn(
                name: "VerifiedOn",
                schema: "pay",
                table: "DirectDeposits");

            migrationBuilder.DropColumn(
                name: "BenefitRemittancePaidThrough",
                schema: "pay",
                table: "CompanyPayrollSetups");

            migrationBuilder.DropColumn(
                name: "OpenAccrualAmount",
                schema: "pay",
                table: "CompanyPayrollSetups");

            migrationBuilder.DropColumn(
                name: "OpenAccrualBatchRef",
                schema: "pay",
                table: "CompanyPayrollSetups");

            migrationBuilder.DropColumn(
                name: "OpenAccrualEmployerTax",
                schema: "pay",
                table: "CompanyPayrollSetups");

            migrationBuilder.DropColumn(
                name: "OpenAccrualPostedOn",
                schema: "pay",
                table: "CompanyPayrollSetups");
        }
    }
}
