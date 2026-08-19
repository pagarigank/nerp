using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ArGapsAllowanceCols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "ar",
                table: "DoubtfulAccountAllowances",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                schema: "ar",
                table: "DoubtfulAccountAllowances",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Method",
                schema: "ar",
                table: "DoubtfulAccountAllowances");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "ar",
                table: "DoubtfulAccountAllowances");
        }
    }
}
