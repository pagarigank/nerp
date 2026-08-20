using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class PtoTransactionColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PtoTransaction_PtoLedgers_PtoLedgerId",
                schema: "pay",
                table: "PtoTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PtoTransaction",
                schema: "pay",
                table: "PtoTransaction");

            migrationBuilder.RenameTable(
                name: "PtoTransaction",
                schema: "pay",
                newName: "PtoTransactions",
                newSchema: "pay");

            migrationBuilder.RenameIndex(
                name: "IX_PtoTransaction_PtoLedgerId",
                schema: "pay",
                table: "PtoTransactions",
                newName: "IX_PtoTransactions_PtoLedgerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PtoTransactions",
                schema: "pay",
                table: "PtoTransactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PtoTransactions_PtoLedgers_PtoLedgerId",
                schema: "pay",
                table: "PtoTransactions",
                column: "PtoLedgerId",
                principalSchema: "pay",
                principalTable: "PtoLedgers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PtoTransactions_PtoLedgers_PtoLedgerId",
                schema: "pay",
                table: "PtoTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PtoTransactions",
                schema: "pay",
                table: "PtoTransactions");

            migrationBuilder.RenameTable(
                name: "PtoTransactions",
                schema: "pay",
                newName: "PtoTransaction",
                newSchema: "pay");

            migrationBuilder.RenameIndex(
                name: "IX_PtoTransactions_PtoLedgerId",
                schema: "pay",
                table: "PtoTransaction",
                newName: "IX_PtoTransaction_PtoLedgerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PtoTransaction",
                schema: "pay",
                table: "PtoTransaction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PtoTransaction_PtoLedgers_PtoLedgerId",
                schema: "pay",
                table: "PtoTransaction",
                column: "PtoLedgerId",
                principalSchema: "pay",
                principalTable: "PtoLedgers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
