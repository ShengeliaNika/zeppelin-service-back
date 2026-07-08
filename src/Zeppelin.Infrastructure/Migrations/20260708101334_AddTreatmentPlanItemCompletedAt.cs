using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeppelin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPlanItemCompletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "TreatmentPlanItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "TreatmentPlanItems");
        }
    }
}
