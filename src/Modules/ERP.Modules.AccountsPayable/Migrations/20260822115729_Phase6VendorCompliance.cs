using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsPayable.Migrations
{
    /// <inheritdoc />
    public partial class Phase6VendorCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiversityClassification",
                schema: "ap",
                table: "Vendors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceCarrier",
                schema: "ap",
                table: "Vendors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InsuranceExpiry",
                schema: "ap",
                table: "Vendors",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsurancePolicyNumber",
                schema: "ap",
                table: "Vendors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnHold",
                schema: "ap",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiversityClassification",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "InsuranceCarrier",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "InsuranceExpiry",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "InsurancePolicyNumber",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "OnHold",
                schema: "ap",
                table: "Vendors");
        }
    }
}
