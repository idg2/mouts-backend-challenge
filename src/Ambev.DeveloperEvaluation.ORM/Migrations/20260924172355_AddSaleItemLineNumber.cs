using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleItemLineNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineNumber",
                table: "SaleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing sales never stored the order they were sent in; number their lines by item id.
            migrationBuilder.Sql(
                """
                UPDATE "SaleItems" AS item
                SET "LineNumber" = numbered."LineNumber"
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "SaleId" ORDER BY "Id") AS "LineNumber"
                    FROM "SaleItems"
                ) AS numbered
                WHERE item."Id" = numbered."Id";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleItems_LineNumber",
                table: "SaleItems",
                sql: "\"LineNumber\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleItems_LineNumber",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "SaleItems");
        }
    }
}
