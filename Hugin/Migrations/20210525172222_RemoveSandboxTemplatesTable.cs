using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Hugin.Migrations
{
    public partial class RemoveSandboxTemplatesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityTemplates_SandboxTemplates_SandboxTemplateId",
                table: "ActivityTemplates");

            migrationBuilder.DropTable(
                name: "SandboxTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ActivityTemplates_SandboxTemplateId",
                table: "ActivityTemplates");

            migrationBuilder.DropColumn(
                name: "SandboxTemplateId",
                table: "ActivityTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ResourceHub",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ResourceHub",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SandboxTemplateId",
                table: "ActivityTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SandboxTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Commands = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SandboxTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTemplates_SandboxTemplateId",
                table: "ActivityTemplates",
                column: "SandboxTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SandboxTemplates_Name",
                table: "SandboxTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityTemplates_SandboxTemplates_SandboxTemplateId",
                table: "ActivityTemplates",
                column: "SandboxTemplateId",
                principalTable: "SandboxTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
