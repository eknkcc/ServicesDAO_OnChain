using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_VotingEngine.Migrations
{
    public partial class walletAddress_vote_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "Votes",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WalletAddress",
                table: "Votes");
        }
    }
}
