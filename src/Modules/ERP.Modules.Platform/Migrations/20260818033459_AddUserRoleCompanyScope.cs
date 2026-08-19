using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleCompanyScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_RoleId",
                schema: "platform",
                table: "UserRoles");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "platform",
                table: "UserRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId_CompanyId",
                schema: "platform",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_RoleId_CompanyId",
                schema: "platform",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "platform",
                table: "UserRoles");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                schema: "platform",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);
        }
    }
}
