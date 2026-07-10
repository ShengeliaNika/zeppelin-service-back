using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeppelin.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryProductDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dimensions",
                table: "InventoryItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Package",
                table: "InventoryItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleType",
                table: "InventoryItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "InventoryItems",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dimensions",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Package",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SaleType",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "InventoryItems");
        }
    }
}
