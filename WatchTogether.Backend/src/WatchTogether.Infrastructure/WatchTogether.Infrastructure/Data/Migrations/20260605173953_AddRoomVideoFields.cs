using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTogether.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomVideoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentVideoType",
                table: "Rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentVideoUrl",
                table: "Rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVideoPlaying",
                table: "Rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "VideoCurrentTime",
                table: "Rooms",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VideoLastSyncAt",
                table: "Rooms",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentVideoType",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CurrentVideoUrl",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsVideoPlaying",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "VideoCurrentTime",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "VideoLastSyncAt",
                table: "Rooms");
        }
    }
}
