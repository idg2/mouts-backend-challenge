using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing products have no code; they are removed so the column can be required and unique.
            migrationBuilder.Sql("DELETE FROM \"Products\";");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Code",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Products");
        }
    }
}
