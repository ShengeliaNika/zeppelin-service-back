using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeppelin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryPricingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsForSale",
                table: "InventoryItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "InventoryItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseFee",
                table: "InventoryItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleFee",
                table: "InventoryItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsForSale",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "PurchaseFee",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SaleFee",
                table: "InventoryItems");
        }
    }
}
