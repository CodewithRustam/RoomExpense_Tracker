using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomExpenseTracker.Migrations
{
    /// <inheritdoc />
    public partial class addedInmembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Members",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Members",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberId1",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_ApplicationUserId",
                table: "Members",
                column: "ApplicationUserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Members_AspNetUsers_ApplicationUserId",
                table: "Members",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Members_MemberId1",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_AspNetUsers_ApplicationUserId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_ApplicationUserId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_MemberId1",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
