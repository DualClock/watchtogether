using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTogether.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Rooms");
        }
    }
}
