using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace DAO_ReputationService.Migrations
{
    public partial class db_init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReputationHistories",
                columns: table => new
                {
                    UserReputationHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    EarnedAmount = table.Column<double>(type: "double", nullable: false),
                    LostAmount = table.Column<double>(type: "double", nullable: false),
                    StakedAmount = table.Column<double>(type: "double", nullable: false),
                    StakeReleasedAmount = table.Column<double>(type: "double", nullable: false),
                    LastTotal = table.Column<double>(type: "double", nullable: false),
                    LastStakedTotal = table.Column<double>(type: "double", nullable: false),
                    LastUsableTotal = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReputationHistories", x => x.UserReputationHistoryID);
                });

            migrationBuilder.CreateTable(
                name: "UserReputationStakes",
                columns: table => new
                {
                    UserReputationStakeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ReferenceID = table.Column<int>(type: "int", nullable: true),
                    ReferenceProcessID = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<double>(type: "double", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReputationStakes", x => x.UserReputationStakeID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReputationHistories");

            migrationBuilder.DropTable(
                name: "UserReputationStakes");
        }
    }
}
