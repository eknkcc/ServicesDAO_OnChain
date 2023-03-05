using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace DAO_DbService.Migrations
{
    public partial class DaoSettingAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlockchainJobPostID",
                table: "JobPosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BlockchainAuctionID",
                table: "Auctions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BlockchainBidID",
                table: "AuctionBids",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DaoSettings",
                columns: table => new
                {
                    DaoSettingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DaoSettings", x => x.DaoSettingID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DaoSettings");

            migrationBuilder.DropColumn(
                name: "BlockchainJobPostID",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "BlockchainAuctionID",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "BlockchainBidID",
                table: "AuctionBids");
        }
    }
}
