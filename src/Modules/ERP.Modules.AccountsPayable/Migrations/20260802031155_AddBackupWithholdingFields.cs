using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsPayable.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupWithholdingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BackupWithholdingAmount",
                schema: "ap",
                table: "Vouchers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Form1099Amount",
                schema: "ap",
                table: "Vouchers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                schema: "ap",
                table: "Vouchers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiptLineId",
                schema: "ap",
                table: "Vouchers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BackupWithholdingFlag",
                schema: "ap",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BackupWithholdingRate",
                schema: "ap",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupWithholdingAmount",
                schema: "ap",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "Form1099Amount",
                schema: "ap",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                schema: "ap",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReceiptLineId",
                schema: "ap",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "BackupWithholdingFlag",
                schema: "ap",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "BackupWithholdingRate",
                schema: "ap",
                table: "Vendors");
        }
    }
}
