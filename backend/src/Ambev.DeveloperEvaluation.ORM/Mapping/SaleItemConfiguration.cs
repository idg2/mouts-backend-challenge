using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// EF Core mapping for the SaleItem entity. The product is an external identity: no foreign key.
/// </summary>
public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    // Work item: TD-010 (FEAT-010)
    /// <summary>
    /// Configures the SaleItems table
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems", t =>
        {
            t.HasCheckConstraint("CK_SaleItems_LineNumber", "\"LineNumber\" > 0");
            t.HasCheckConstraint("CK_SaleItems_UnitPrice", "\"UnitPrice\" > 0");
            t.HasCheckConstraint("CK_SaleItems_Quantity", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_SaleItems_DiscountPercentage", "\"DiscountPercentage\" >= 0 AND \"DiscountPercentage\" <= 100");
            t.HasCheckConstraint("CK_SaleItems_DiscountAmount", "\"DiscountAmount\" >= 0");
            t.HasCheckConstraint("CK_SaleItems_TotalAmount", "\"TotalAmount\" >= 0");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.ProductDescription).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.DiscountPercentage).HasColumnType("numeric(5,2)");
        builder.Property(i => i.DiscountAmount).HasColumnType("numeric(18,2)");
        builder.Property(i => i.TotalAmount).HasColumnType("numeric(18,2)");
    }
}
