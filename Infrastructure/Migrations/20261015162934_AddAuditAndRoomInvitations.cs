using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndRoomInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Settlements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Settlements",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(MINUTE, 330, GETUTCDATE())");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Settlements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Settlements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Rooms",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Rooms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Members",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(MINUTE, 330, GETUTCDATE())");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ExpenseSplits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ExpenseSplits",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(MINUTE, 330, GETUTCDATE())");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ExpenseSplits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "ExpenseSplits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Expenses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Expenses",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(MINUTE, 330, GETUTCDATE())");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Expenses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Expenses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    InvitedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InvitedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInvitations_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_UpdatedByUserId",
                table: "Rooms",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvitations_RoomId_InvitedEmail_Status",
                table: "RoomInvitations",
                columns: new[] { "RoomId", "InvitedEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvitations_Token",
                table: "RoomInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_AspNetUsers_UpdatedByUserId",
                table: "Rooms",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Safe backfill: preserve original historical timestamps for all existing records
            migrationBuilder.Sql("UPDATE Expenses SET CreatedDate = [Date] WHERE CreatedDate = '0001-01-01' OR CreatedDate IS NULL;");
            migrationBuilder.Sql("UPDATE Members SET CreatedDate = JoinedDate WHERE CreatedDate = '0001-01-01' OR CreatedDate IS NULL;");
            migrationBuilder.Sql("UPDATE Settlements SET CreatedDate = SettlementDate WHERE CreatedDate = '0001-01-01' OR CreatedDate IS NULL;");
            migrationBuilder.Sql("UPDATE ExpenseSplits SET CreatedDate = DATEADD(MINUTE, 330, GETUTCDATE()) WHERE CreatedDate = '0001-01-01' OR CreatedDate IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_AspNetUsers_UpdatedByUserId",
                table: "Rooms");

            migrationBuilder.DropTable(
                name: "RoomInvitations");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_UpdatedByUserId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ExpenseSplits");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ExpenseSplits");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ExpenseSplits");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "ExpenseSplits");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Expenses");
        }
    }
}
