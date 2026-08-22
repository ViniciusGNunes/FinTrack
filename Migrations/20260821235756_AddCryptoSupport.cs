using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddCryptoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ClosePrice",
                table: "MarketPriceHistories",
                type: "numeric(28,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "InvestmentTransactions",
                type: "numeric(28,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InvestmentTransactions",
                type: "numeric(28,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Investments",
                type: "numeric(28,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchasePricePerUnit",
                table: "Investments",
                type: "numeric(28,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPricePerUnit",
                table: "Investments",
                type: "numeric(28,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ClosePrice",
                table: "MarketPriceHistories",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "InvestmentTransactions",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InvestmentTransactions",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Investments",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchasePricePerUnit",
                table: "Investments",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPricePerUnit",
                table: "Investments",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldNullable: true);
        }
    }
}
