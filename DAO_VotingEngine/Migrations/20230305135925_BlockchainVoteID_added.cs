using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_VotingEngine.Migrations
{
    public partial class BlockchainVoteID_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlockchainVoteID",
                table: "Votes",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockchainVoteID",
                table: "Votes");
        }
    }
}
