using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatewayService.AccountCharge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndex_On_InvoiceAppliedDeposits_TxHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAppliedDeposits_InvoiceId_TxHash",
                table: "InvoiceAppliedDeposits",
                columns: new[] { "InvoiceId", "TxHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceAppliedDeposits_InvoiceId_TxHash",
                table: "InvoiceAppliedDeposits");
        }
    }
}
