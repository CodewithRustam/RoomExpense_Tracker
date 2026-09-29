using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberJoinedDateAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedDate",
                table: "Members",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // Safe deduplication in case any duplicates exist before creating unique index
            migrationBuilder.Sql(@"
                WITH CTE AS (
                    SELECT ExpenseId, MemberId, ROW_NUMBER() OVER (PARTITION BY ExpenseId, MemberId ORDER BY ExpenseSplitId) AS rn
                    FROM ExpenseSplits
                )
                DELETE FROM CTE WHERE rn > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSplits_ExpenseId_MemberId",
                table: "ExpenseSplits",
                columns: new[] { "ExpenseId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_RoomId_Date",
                table: "Expenses",
                columns: new[] { "RoomId", "Date" });

            // 1. Backfill JoinedDate for existing members to the earliest expense date in their room (so historical math stays exact)
            migrationBuilder.Sql(@"
                UPDATE m
                SET m.JoinedDate = ISNULL(
                    (SELECT MIN(e.Date) FROM Expenses e WHERE e.RoomId = m.RoomId),
                    m.JoinedDate
                )
                FROM Members m;
            ");

            // 2. Backfill ExpenseSplits for any existing legacy expenses that have no split records
            migrationBuilder.Sql(@"
                INSERT INTO ExpenseSplits (ExpenseId, MemberId, OwedAmount)
                SELECT e.ExpenseId, m.MemberId, ROUND(e.Amount / NULLIF(COUNT(*) OVER (PARTITION BY e.ExpenseId), 0), 2)
                FROM Expenses e
                JOIN Members m ON e.RoomId = m.RoomId AND (m.IsDeleted = 0 OR m.IsDeleted IS NULL)
                LEFT JOIN ExpenseSplits es ON e.ExpenseId = es.ExpenseId
                WHERE es.ExpenseSplitId IS NULL 
                  AND (e.IsDeleted = 0 OR e.IsDeleted IS NULL)
                  AND (e.Amount > 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseSplits_ExpenseId_MemberId",
                table: "ExpenseSplits");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_RoomId_Date",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "JoinedDate",
                table: "Members");
        }
    }
}
