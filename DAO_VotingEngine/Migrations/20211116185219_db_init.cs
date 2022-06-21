using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace DAO_VotingEngine.Migrations
{
    public partial class db_init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    VoteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VotingID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.VoteID);
                });

            migrationBuilder.CreateTable(
                name: "Votings",
                columns: table => new
                {
                    VotingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    IsFormal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuorumCount = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    StakedFor = table.Column<double>(type: "double", nullable: true),
                    StakedAgainst = table.Column<double>(type: "double", nullable: true),
                    PolicingRate = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votings", x => x.VotingID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Votings");
        }
    }
}
