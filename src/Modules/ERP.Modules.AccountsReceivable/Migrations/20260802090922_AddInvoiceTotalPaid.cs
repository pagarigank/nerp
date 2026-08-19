using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceTotalPaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalPaid",
                schema: "ar",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE ar.Invoices
                SET TotalPaid = ISNULL(
                    (SELECT SUM(a.AppliedAmount) FROM ar.CashReceiptApplications a WHERE a.InvoiceId = ar.Invoices.Id),
                    0);
                """);

            migrationBuilder.Sql(
                """
                UPDATE ar.Invoices
                SET Status = CASE
                    WHEN TotalPaid >= (SELECT ISNULL(SUM((l.Quantity * l.UnitPrice) + l.TaxAmount - l.DiscountAmount), 0)
                                       FROM ar.InvoiceLines l WHERE l.InvoiceId = ar.Invoices.Id)
                        AND Status IN (0, 1) THEN 2
                    WHEN TotalPaid > 0 AND Status = 0 THEN 1
                    ELSE Status
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPaid",
                schema: "ar",
                table: "Invoices");
        }
    }
}
