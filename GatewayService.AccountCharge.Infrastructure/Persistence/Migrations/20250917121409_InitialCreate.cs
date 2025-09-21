using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatewayService.AccountCharge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    ExpectedCurrency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DepositTag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DepositNetwork = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    WalletCurrency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceAddresses_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceAppliedDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TxHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DepositAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DepositTag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DepositNetwork = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WasConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    Confirmations = table.Column<int>(type: "int", nullable: false),
                    RequiredConfirmations = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAppliedDeposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceAppliedDeposits_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAddresses_InvoiceId",
                table: "InvoiceAddresses",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAppliedDeposits_InvoiceId",
                table: "InvoiceAppliedDeposits",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAppliedDeposits_ObservedAt",
                table: "InvoiceAppliedDeposits",
                column: "ObservedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAppliedDeposits_TxHash",
                table: "InvoiceAppliedDeposits",
                column: "TxHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedAt",
                table: "Invoices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceAddresses");

            migrationBuilder.DropTable(
                name: "InvoiceAppliedDeposits");

            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
