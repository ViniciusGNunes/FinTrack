using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Debts",
                columns: table => new
                {
                    DebtID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DebtType = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OriginalPrincipal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    RateType = table.Column<int>(type: "integer", nullable: false),
                    PaymentFrequency = table.Column<int>(type: "integer", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalInstallments = table.Column<int>(type: "integer", nullable: true),
                    PaidInstallments = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: true),
                    MaturityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPaidOff = table.Column<bool>(type: "boolean", nullable: false),
                    AutoGenerateExpenses = table.Column<bool>(type: "boolean", nullable: false),
                    TransactionID = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Debts", x => x.DebtID);
                    table.ForeignKey(
                        name: "FK_Debts_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Debts_Transactions_TransactionID",
                        column: x => x.TransactionID,
                        principalTable: "Transactions",
                        principalColumn: "TransactionID");
                });

            migrationBuilder.CreateTable(
                name: "DebtPayments",
                columns: table => new
                {
                    DebtPaymentID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DebtID = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    InterestAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpenseID = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtPayments", x => x.DebtPaymentID);
                    table.ForeignKey(
                        name: "FK_DebtPayments_Debts_DebtID",
                        column: x => x.DebtID,
                        principalTable: "Debts",
                        principalColumn: "DebtID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebtPayments_Expenses_ExpenseID",
                        column: x => x.ExpenseID,
                        principalTable: "Expenses",
                        principalColumn: "ExpenseID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DebtPayments_DebtID",
                table: "DebtPayments",
                column: "DebtID");

            migrationBuilder.CreateIndex(
                name: "IX_DebtPayments_ExpenseID",
                table: "DebtPayments",
                column: "ExpenseID");

            migrationBuilder.CreateIndex(
                name: "IX_Debts_TransactionID",
                table: "Debts",
                column: "TransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_Debts_UserID",
                table: "Debts",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DebtPayments");

            migrationBuilder.DropTable(
                name: "Debts");
        }
    }
}
