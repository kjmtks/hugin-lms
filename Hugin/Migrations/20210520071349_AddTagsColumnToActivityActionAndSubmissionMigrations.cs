using Microsoft.EntityFrameworkCore.Migrations;

namespace Hugin.Migrations
{
    public partial class AddTagsColumnToActivityActionAndSubmissionMigrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "ActivityActions",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "ActivityActions");
        }
    }
}
