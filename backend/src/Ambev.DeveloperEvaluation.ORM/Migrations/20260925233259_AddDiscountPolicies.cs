using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscountPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxQuantityPerProduct = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountPolicies", x => x.Id);
                    table.CheckConstraint("CK_DiscountPolicies_MaxQuantityPerProduct", "\"MaxQuantityPerProduct\" > 0");
                    table.CheckConstraint("CK_DiscountPolicies_ValidTo", "\"ValidTo\" IS NULL OR \"ValidTo\" > \"ValidFrom\"");
                });

            migrationBuilder.CreateTable(
                name: "DiscountTiers",
                columns: table => new
                {
                    DiscountPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinQuantity = table.Column<int>(type: "integer", nullable: false),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountTiers", x => new { x.DiscountPolicyId, x.Id });
                    table.CheckConstraint("CK_DiscountTiers_MaxQuantity", "\"MaxQuantity\" IS NULL OR \"MaxQuantity\" >= \"MinQuantity\"");
                    table.CheckConstraint("CK_DiscountTiers_MinQuantity", "\"MinQuantity\" >= 1");
                    table.CheckConstraint("CK_DiscountTiers_Percentage", "\"Percentage\" > 0 AND \"Percentage\" <= 100");
                    table.ForeignKey(
                        name: "FK_DiscountTiers_DiscountPolicies_DiscountPolicyId",
                        column: x => x.DiscountPolicyId,
                        principalTable: "DiscountPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountPolicies_ProductId_BranchId_ValidFrom",
                table: "DiscountPolicies",
                columns: new[] { "ProductId", "BranchId", "ValidFrom" });

            // The default policy reproduces the README rules (spec section 4). Literal values keep the migration
            // deterministic; CreatedAt equals ValidFrom (A14). DiscountPolicyConfiguration.DefaultPolicyId holds the same id.
            migrationBuilder.InsertData(
                table: "DiscountPolicies",
                columns: new[] { "Id", "ProductId", "BranchId", "ValidFrom", "ValidTo", "MaxQuantityPerProduct", "CreatedAt" },
                columnTypes: new[] { "uuid", "uuid", "uuid", "timestamp with time zone", "timestamp with time zone", "integer", "timestamp with time zone" },
                values: new object[]
                {
                    new Guid("7d0c5a6e-2f4b-4c1d-9a39-0f6f2b8a1c01"),
                    null,
                    null,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    null,
                    20,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });

            migrationBuilder.InsertData(
                table: "DiscountTiers",
                columns: new[] { "DiscountPolicyId", "Id", "MinQuantity", "MaxQuantity", "Percentage" },
                columnTypes: new[] { "uuid", "integer", "integer", "integer", "numeric(5,2)" },
                values: new object[,]
                {
                    { new Guid("7d0c5a6e-2f4b-4c1d-9a39-0f6f2b8a1c01"), 1, 4, 9, 10m },
                    { new Guid("7d0c5a6e-2f4b-4c1d-9a39-0f6f2b8a1c01"), 2, 10, 20, 20m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountTiers");

            migrationBuilder.DropTable(
                name: "DiscountPolicies");
        }
    }
}
