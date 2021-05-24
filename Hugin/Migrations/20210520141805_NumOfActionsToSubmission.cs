using Microsoft.EntityFrameworkCore.Migrations;

namespace Hugin.Migrations
{
    public partial class NumOfActionsToSubmission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumOfRuns",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumOfSaves",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumOfValidateAccepts",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumOfValidateRejects",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumOfRuns",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "NumOfSaves",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "NumOfValidateAccepts",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "NumOfValidateRejects",
                table: "Submissions");
        }
    }
}
