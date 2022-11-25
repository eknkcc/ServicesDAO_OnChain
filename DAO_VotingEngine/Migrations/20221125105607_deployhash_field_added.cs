using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_VotingEngine.Migrations
{
    public partial class deployhash_field_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeployHash",
                table: "Votings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeployHash",
                table: "Votes",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeployHash",
                table: "Votings");

            migrationBuilder.DropColumn(
                name: "DeployHash",
                table: "Votes");
        }
    }
}
