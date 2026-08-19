using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class CreditMemoLineLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_CreditDebitMemos_CreditDebitMemoId",
                schema: "ar",
                table: "InvoiceLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "InvoiceId",
                schema: "ar",
                table: "InvoiceLines",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_CreditDebitMemos_CreditDebitMemoId",
                schema: "ar",
                table: "InvoiceLines",
                column: "CreditDebitMemoId",
                principalSchema: "ar",
                principalTable: "CreditDebitMemos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_CreditDebitMemos_CreditDebitMemoId",
                schema: "ar",
                table: "InvoiceLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "InvoiceId",
                schema: "ar",
                table: "InvoiceLines",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_CreditDebitMemos_CreditDebitMemoId",
                schema: "ar",
                table: "InvoiceLines",
                column: "CreditDebitMemoId",
                principalSchema: "ar",
                principalTable: "CreditDebitMemos",
                principalColumn: "Id");
        }
    }
}
