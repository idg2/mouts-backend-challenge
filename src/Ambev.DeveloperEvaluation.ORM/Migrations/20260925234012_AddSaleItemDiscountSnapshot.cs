using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleItemDiscountSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RequestedDiscountPercentage",
                table: "SaleItems",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DiscountPolicyId",
                table: "SaleItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountCeilingPercentage",
                table: "SaleItems",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            // Items stored before the discount policies were priced by hand: point them to the seeded default policy
            // (AddDiscountPolicies) and take their stored percentage as the ceiling, so the applied percentage never
            // exceeds it. The requested percentage stays null: no client asked for one.
            migrationBuilder.Sql(
                """
                UPDATE "SaleItems"
                SET "DiscountPolicyId" = '7d0c5a6e-2f4b-4c1d-9a39-0f6f2b8a1c01',
                    "DiscountCeilingPercentage" = "DiscountPercentage";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleItems_DiscountCeilingPercentage",
                table: "SaleItems",
                sql: "\"DiscountCeilingPercentage\" >= 0 AND \"DiscountCeilingPercentage\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleItems_RequestedDiscountPercentage",
                table: "SaleItems",
                sql: "\"RequestedDiscountPercentage\" IS NULL OR (\"RequestedDiscountPercentage\" >= 0 AND \"RequestedDiscountPercentage\" <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleItems_DiscountCeilingPercentage",
                table: "SaleItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleItems_RequestedDiscountPercentage",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "RequestedDiscountPercentage",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "DiscountPolicyId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "DiscountCeilingPercentage",
                table: "SaleItems");
        }
    }
}
