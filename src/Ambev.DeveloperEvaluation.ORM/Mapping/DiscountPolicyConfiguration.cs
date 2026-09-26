using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

// Work item: TASK-061 (FEAT-001)
/// <summary>
/// EF Core mapping for the DiscountPolicy aggregate: the DiscountPolicies table and its tiers in the DiscountTiers child
/// table. Product and branch are external identities: no foreign keys.
/// </summary>
public class DiscountPolicyConfiguration : IEntityTypeConfiguration<DiscountPolicy>
{
    // Work item: TD-043
    /// <summary>
    /// The id of the default policy that migration AddDiscountPolicies seeds with the challenge rules (CHALLENGE.md).
    /// </summary>
    public static readonly Guid DefaultPolicyId = Guid.Parse("7d0c5a6e-2f4b-4c1d-9a39-0f6f2b8a1c01");

    /// <summary>
    /// The start, and the creation instant, of the seeded default policy.
    /// </summary>
    public static readonly DateTime DefaultPolicyValidFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Work item: TASK-061 (FEAT-001), TD-032
    /// <summary>
    /// Configures the DiscountPolicies and DiscountTiers tables
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<DiscountPolicy> builder)
    {
        builder.ToTable("DiscountPolicies", table =>
        {
            table.HasCheckConstraint("CK_DiscountPolicies_ValidTo", "\"ValidTo\" IS NULL OR \"ValidTo\" > \"ValidFrom\"");
            table.HasCheckConstraint("CK_DiscountPolicies_MaxQuantityPerProduct", "\"MaxQuantityPerProduct\" > 0");
        });

        builder.HasKey(policy => policy.Id);
        builder.Property(policy => policy.Id).HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(policy => policy.ProductId).HasColumnType("uuid");
        builder.Property(policy => policy.BranchId).HasColumnType("uuid");
        builder.Property(policy => policy.ValidFrom).HasColumnType("timestamp with time zone");
        builder.Property(policy => policy.ValidTo).HasColumnType("timestamp with time zone");
        builder.Property(policy => policy.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(policy => policy.DisabledAt).HasColumnType("timestamp with time zone");
        builder.Ignore(policy => policy.SpecificityRank);
        builder.HasIndex(policy => new { policy.ProductId, policy.BranchId, policy.ValidFrom });

        builder.OwnsMany(policy => policy.Tiers, tier =>
        {
            tier.ToTable("DiscountTiers", table =>
            {
                table.HasCheckConstraint("CK_DiscountTiers_MinQuantity", "\"MinQuantity\" >= 1");
                table.HasCheckConstraint("CK_DiscountTiers_MaxQuantity", "\"MaxQuantity\" IS NULL OR \"MaxQuantity\" >= \"MinQuantity\"");
                table.HasCheckConstraint("CK_DiscountTiers_Percentage", "\"Percentage\" > 0 AND \"Percentage\" <= 100");
            });
            tier.WithOwner().HasForeignKey("DiscountPolicyId");
            tier.Property<int>("Id");
            tier.HasKey("DiscountPolicyId", "Id");
            tier.Property(value => value.Percentage).HasPrecision(5, 2);
        });
        builder.Navigation(policy => policy.Tiers).HasField("_tiers").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
