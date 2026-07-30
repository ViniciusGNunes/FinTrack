using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinTrack.Migrations
{
    /// <inheritdoc />
    public partial class CategoryMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Transactions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CategoryID",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceTargetDay",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Expenses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "Expenses",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidDate",
                table: "Expenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Expenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    UserID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryID",
                table: "Transactions",
                column: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Categories_CategoryID",
                table: "Transactions",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Categories_CategoryID",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CategoryID",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CategoryID",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurrenceTargetDay",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaidDate",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Expenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
