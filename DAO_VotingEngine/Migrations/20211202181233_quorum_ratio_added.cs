using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_VotingEngine.Migrations
{
    public partial class quorum_ratio_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "QuorumRatio",
                table: "Votings",
                type: "double",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuorumRatio",
                table: "Votings");
        }
    }
}
