using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomExpenseTracker.Migrations
{
    /// <inheritdoc />
    public partial class addnewcolumninSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaidToMemberId",
                table: "Settlements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidToMemberId",
                table: "Settlements");
        }
    }
}
