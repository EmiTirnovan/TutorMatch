using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorMatch.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionSubjectAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Sessions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "Sessions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "Sessions");
        }
    }
}
