using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.GeneralLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignCurrencySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                schema: "gl",
                table: "JournalEntryLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                schema: "gl",
                table: "JournalEntryLines",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ForeignCredit",
                schema: "gl",
                table: "JournalEntryLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ForeignDebit",
                schema: "gl",
                table: "JournalEntryLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Account",
                schema: "gl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    NormalBalance = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_CurrencyId",
                schema: "gl",
                table: "JournalEntryLines",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_Account_AccountId",
                schema: "gl",
                table: "JournalEntryLines",
                column: "AccountId",
                principalSchema: "gl",
                principalTable: "Account",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_Account_AccountId",
                schema: "gl",
                table: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "Account",
                schema: "gl");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_CurrencyId",
                schema: "gl",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                schema: "gl",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                schema: "gl",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "ForeignCredit",
                schema: "gl",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "ForeignDebit",
                schema: "gl",
                table: "JournalEntryLines");
        }
    }
}
