using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing customers have no document; they are removed so the column can be required and unique.
            // Sales keep their own copy of the customer name and have no foreign key to customers.
            migrationBuilder.Sql("DELETE FROM \"Customers\";");

            migrationBuilder.AddColumn<string>(
                name: "Document",
                table: "Customers",
                type: "character varying(14)",
                maxLength: 14,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Document",
                table: "Customers",
                column: "Document",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Document",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Document",
                table: "Customers");
        }
    }
}
