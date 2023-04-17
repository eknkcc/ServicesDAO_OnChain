using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_DbService.Migrations
{
    public partial class document_url_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "JobPosts",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "JobPosts");
        }
    }
}
