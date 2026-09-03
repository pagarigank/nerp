using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.MalaysianTax.Migrations
{
    /// <inheritdoc />
    public partial class InitialMalaysianTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tax");

            migrationBuilder.CreateTable(
                name: "company_sst_registration",
                schema: "tax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRegisteredForSalesTax = table.Column<bool>(type: "bit", nullable: false),
                    SalesTaxRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SalesTaxRegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SalesTaxDeregistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRegisteredForServiceTax = table.Column<bool>(type: "bit", nullable: false),
                    ServiceTaxRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServiceTaxRegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceTaxDeregistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRegisteredForTourismTax = table.Column<bool>(type: "bit", nullable: false),
                    TourismTaxRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RmcdPortalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_sst_registration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "exempt_supply_category",
                schema: "tax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SupplyType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LhdnReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exempt_supply_category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tax_code",
                schema: "tax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LhdntTaxType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_code", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tax_determination_rule",
                schema: "tax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlaceOfSupply = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_determination_rule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SstReturns",
                schema: "tax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalTaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTaxCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTaxPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExemptAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FiledAt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LhdnReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SstRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SstReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SstReturns_company_sst_registration_SstRegistrationId",
                        column: x => x.SstRegistrationId,
                        principalSchema: "tax",
                        principalTable: "company_sst_registration",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tax_rate",
                schema: "tax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePercent = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_rate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tax_rate_tax_code_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalSchema: "tax",
                        principalTable: "tax_code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_sst_registration_CompanyId",
                schema: "tax",
                table: "company_sst_registration",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_company_sst_registration_CompanyId_IsActive",
                schema: "tax",
                table: "company_sst_registration",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_exempt_supply_category_CompanyId",
                schema: "tax",
                table: "exempt_supply_category",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_exempt_supply_category_CompanyId_CategoryCode",
                schema: "tax",
                table: "exempt_supply_category",
                columns: new[] { "CompanyId", "CategoryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exempt_supply_category_SupplyType",
                schema: "tax",
                table: "exempt_supply_category",
                column: "SupplyType");

            migrationBuilder.CreateIndex(
                name: "IX_exempt_supply_category_TaxCodeId",
                schema: "tax",
                table: "exempt_supply_category",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SstReturns_CompanyId",
                schema: "tax",
                table: "SstReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SstReturns_CompanyId_PeriodStart_ReturnType",
                schema: "tax",
                table: "SstReturns",
                columns: new[] { "CompanyId", "PeriodStart", "ReturnType" });

            migrationBuilder.CreateIndex(
                name: "IX_SstReturns_SstRegistrationId",
                schema: "tax",
                table: "SstReturns",
                column: "SstRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_SstReturns_Status",
                schema: "tax",
                table: "SstReturns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tax_code_Code",
                schema: "tax",
                table: "tax_code",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_code_LhdntTaxType",
                schema: "tax",
                table: "tax_code",
                column: "LhdntTaxType");

            migrationBuilder.CreateIndex(
                name: "IX_tax_determination_rule_CompanyId",
                schema: "tax",
                table: "tax_determination_rule",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_determination_rule_CompanyId_IsActive_Priority",
                schema: "tax",
                table: "tax_determination_rule",
                columns: new[] { "CompanyId", "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_tax_determination_rule_CustomerType",
                schema: "tax",
                table: "tax_determination_rule",
                column: "CustomerType");

            migrationBuilder.CreateIndex(
                name: "IX_tax_determination_rule_ItemCategory",
                schema: "tax",
                table: "tax_determination_rule",
                column: "ItemCategory");

            migrationBuilder.CreateIndex(
                name: "IX_tax_determination_rule_PlaceOfSupply",
                schema: "tax",
                table: "tax_determination_rule",
                column: "PlaceOfSupply");

            migrationBuilder.CreateIndex(
                name: "IX_tax_determination_rule_TaxCodeId",
                schema: "tax",
                table: "tax_determination_rule",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rate_EffectiveFrom",
                schema: "tax",
                table: "tax_rate",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rate_TaxCodeId",
                schema: "tax",
                table: "tax_rate",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rate_TaxCodeId_EffectiveFrom",
                schema: "tax",
                table: "tax_rate",
                columns: new[] { "TaxCodeId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exempt_supply_category",
                schema: "tax");

            migrationBuilder.DropTable(
                name: "SstReturns",
                schema: "tax");

            migrationBuilder.DropTable(
                name: "tax_determination_rule",
                schema: "tax");

            migrationBuilder.DropTable(
                name: "tax_rate",
                schema: "tax");

            migrationBuilder.DropTable(
                name: "company_sst_registration",
                schema: "tax");

            migrationBuilder.DropTable(
                name: "tax_code",
                schema: "tax");
        }
    }
}
