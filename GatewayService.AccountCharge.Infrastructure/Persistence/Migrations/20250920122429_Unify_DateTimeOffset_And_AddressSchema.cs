using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatewayService.AccountCharge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Unify_DateTimeOffset_And_AddressSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceAppliedDeposits_TxHash",
                table: "InvoiceAppliedDeposits");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ObservedAt",
                table: "InvoiceAppliedDeposits",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "DepositNetwork",
                table: "InvoiceAddresses",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "DepositAddress",
                table: "InvoiceAddresses",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "InvoiceAddresses",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAddresses_CreatedAt",
                table: "InvoiceAddresses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAddresses_InvoiceId_DepositAddress_DepositNetwork_DepositTag_WalletId",
                table: "InvoiceAddresses",
                columns: new[] { "InvoiceId", "DepositAddress", "DepositNetwork", "DepositTag", "WalletId" },
                unique: true,
                filter: "[DepositNetwork] IS NOT NULL AND [DepositTag] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceAddresses_CreatedAt",
                table: "InvoiceAddresses");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceAddresses_InvoiceId_DepositAddress_DepositNetwork_DepositTag_WalletId",
                table: "InvoiceAddresses");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ObservedAt",
                table: "InvoiceAppliedDeposits",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "DepositNetwork",
                table: "InvoiceAddresses",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DepositAddress",
                table: "InvoiceAddresses",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "InvoiceAddresses",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAppliedDeposits_TxHash",
                table: "InvoiceAppliedDeposits",
                column: "TxHash",
                unique: true);
        }
    }
}
