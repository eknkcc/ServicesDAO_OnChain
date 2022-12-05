using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_ReputationService.Migrations
{
    public partial class walletAddress_added_stakes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeployHash",
                table: "UserReputationStakes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "UserReputationStakes",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeployHash",
                table: "UserReputationStakes");

            migrationBuilder.DropColumn(
                name: "WalletAddress",
                table: "UserReputationStakes");
        }
    }
}
