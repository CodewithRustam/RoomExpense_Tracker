using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomExpenseTracker.Migrations
{
    /// <inheritdoc />
    public partial class updatedcolinexpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsSplitExpense",
                table: "Expenses",
                newName: "IsNonSplitExpense");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsNonSplitExpense",
                table: "Expenses",
                newName: "IsSplitExpense");
        }
    }
}
