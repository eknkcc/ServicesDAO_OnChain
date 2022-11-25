using Microsoft.EntityFrameworkCore.Migrations;

namespace DAO_DbService.Migrations
{
    public partial class deployhash_field_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeployHash",
                table: "JobPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeployHash",
                table: "Auctions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeployHash",
                table: "AuctionBids",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeployHash",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "DeployHash",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "DeployHash",
                table: "AuctionBids");
        }
    }
}
