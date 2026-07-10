using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeppelin.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDecidedAtUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalDecidedByUserId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            // defaultValue is Approved (1), not Pending (0): existing rows (already-active
            // staff created before this feature existed, including the bootstrap admin)
            // must not be retroactively locked out. Only the register endpoint explicitly
            // sets Pending for newly self-registered accounts.
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalDecidedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ApprovalDecidedByUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "AspNetUsers");
        }
    }
}
