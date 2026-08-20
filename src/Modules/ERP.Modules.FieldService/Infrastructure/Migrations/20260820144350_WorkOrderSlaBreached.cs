using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.FieldService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorkOrderSlaBreached : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SlaBreached",
                schema: "fs",
                table: "WorkOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlaBreached",
                schema: "fs",
                table: "WorkOrders");
        }
    }
}
