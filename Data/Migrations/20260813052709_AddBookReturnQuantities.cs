using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookReturnQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Condition",
                table: "BorrowDetails",
                newName: "MinorDamageQuantity");

            migrationBuilder.AddColumn<int>(
                name: "GoodQuantity",
                table: "BorrowDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LostQuantity",
                table: "BorrowDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MajorDamageQuantity",
                table: "BorrowDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoodQuantity",
                table: "BorrowDetails");

            migrationBuilder.DropColumn(
                name: "LostQuantity",
                table: "BorrowDetails");

            migrationBuilder.DropColumn(
                name: "MajorDamageQuantity",
                table: "BorrowDetails");

            migrationBuilder.RenameColumn(
                name: "MinorDamageQuantity",
                table: "BorrowDetails",
                newName: "Condition");
        }
    }
}
