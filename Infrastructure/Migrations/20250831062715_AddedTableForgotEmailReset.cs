using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedTableForgotEmailReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Members_MemberId1",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_MemberId1",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "Expenses");

            migrationBuilder.CreateTable(
                name: "PasswordResetLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expiry = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetLink", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_MemberId",
                table: "Settlements",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_Members_MemberId",
                table: "Settlements",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_Members_MemberId",
                table: "Settlements");

            migrationBuilder.DropTable(
                name: "PasswordResetLink");

            migrationBuilder.DropIndex(
                name: "IX_Settlements_MemberId",
                table: "Settlements");

            migrationBuilder.AddColumn<int>(
                name: "MemberId1",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_MemberId1",
                table: "Expenses",
                column: "MemberId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Members_MemberId1",
                table: "Expenses",
                column: "MemberId1",
                principalTable: "Members",
                principalColumn: "MemberId");
        }
    }
}
