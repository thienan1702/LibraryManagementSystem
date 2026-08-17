using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookReturnConditionToBorrowDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DamageDescription",
                table: "BorrowDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DamageFee",
                table: "BorrowDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReturnCondition",
                table: "BorrowDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "BorrowDetails",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DamageDescription",
                table: "BorrowDetails");

            migrationBuilder.DropColumn(
                name: "DamageFee",
                table: "BorrowDetails");

            migrationBuilder.DropColumn(
                name: "ReturnCondition",
                table: "BorrowDetails");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "BorrowDetails");
        }
    }
}
