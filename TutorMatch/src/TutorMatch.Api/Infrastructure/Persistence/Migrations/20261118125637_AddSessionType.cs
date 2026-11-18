using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorMatch.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Sessions",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Sessions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Sessions",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "Sessions",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Sessions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Online");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Sessions");
        }
    }
}
