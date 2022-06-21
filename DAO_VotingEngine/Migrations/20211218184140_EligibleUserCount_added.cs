using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_VotingEngine.Migrations
{
    public partial class EligibleUserCount_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EligibleUserCount",
                table: "Votings",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EligibleUserCount",
                table: "Votings");
        }
    }
}
