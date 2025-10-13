using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatewayService.AccountCharge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrepaidInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DepositNetwork",
                table: "InvoiceAppliedDeposits",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateTable(
                name: "PrepaidInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Network = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TxHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ObservedAmount = table.Column<decimal>(type: "decimal(38,18)", nullable: true),
                    ObservedCurrency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ObservedAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ObservedTag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ObservedWalletId = table.Column<int>(type: "int", nullable: true),
                    ConfirmationsObserved = table.Column<int>(type: "int", nullable: false),
                    RequiredConfirmationsObserved = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepaidInvoices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrepaidInvoices_CreatedAt",
                table: "PrepaidInvoices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PrepaidInvoices_Status",
                table: "PrepaidInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PrepaidInvoices_TxHash",
                table: "PrepaidInvoices",
                column: "TxHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrepaidInvoices");

            migrationBuilder.AlterColumn<string>(
                name: "DepositNetwork",
                table: "InvoiceAppliedDeposits",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}
