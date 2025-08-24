using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomExpenseTracker.Migrations
{
    /// <inheritdoc />
    public partial class addedtwocolinexpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSplitExpense",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OwedToMemberId",
                table: "Expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSplitExpense",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "OwedToMemberId",
                table: "Expenses");
        }
    }
}
