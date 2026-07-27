using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Reservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
