using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.ProjectAccounting.Migrations
{
    /// <inheritdoc />
    public partial class ProjCostBilledFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBilled",
                schema: "proj",
                table: "CostTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CostTransactions_IsBilled",
                schema: "proj",
                table: "CostTransactions",
                column: "IsBilled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CostTransactions_IsBilled",
                schema: "proj",
                table: "CostTransactions");

            migrationBuilder.DropColumn(
                name: "IsBilled",
                schema: "proj",
                table: "CostTransactions");
        }
    }
}
