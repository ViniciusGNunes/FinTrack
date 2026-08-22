using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinTrack.Migrations
{
    /// <inheritdoc />
    public partial class InvestmentMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Investments",
                columns: table => new
                {
                    InvestmentID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    InvestmentType = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TotalInvested = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    PurchasePricePerUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    CurrentPricePerUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    RateType = table.Column<int>(type: "integer", nullable: true),
                    AnnualRate = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    IsTaxExempt = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsLiquidated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investments", x => x.InvestmentID);
                    table.ForeignKey(
                        name: "FK_Investments_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketPriceHistories",
                columns: table => new
                {
                    MarketPriceHistoryID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosePrice = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPriceHistories", x => x.MarketPriceHistoryID);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentTransactions",
                columns: table => new
                {
                    InvestmentTransactionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestmentID = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentTransactions", x => x.InvestmentTransactionID);
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_Investments_InvestmentID",
                        column: x => x.InvestmentID,
                        principalTable: "Investments",
                        principalColumn: "InvestmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Investments_UserID",
                table: "Investments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_InvestmentID",
                table: "InvestmentTransactions",
                column: "InvestmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPriceHistories_Symbol_Date",
                table: "MarketPriceHistories",
                columns: new[] { "Symbol", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentTransactions");

            migrationBuilder.DropTable(
                name: "MarketPriceHistories");

            migrationBuilder.DropTable(
                name: "Investments");
        }
    }
}
