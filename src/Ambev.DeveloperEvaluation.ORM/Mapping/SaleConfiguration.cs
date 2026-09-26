using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// EF Core mapping for the Sale entity. Customer and branch are external identities: no foreign keys.
/// </summary>
public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    // Work item: TD-039
    /// <summary>
    /// Configures the Sales table and its one-to-many relationship with SaleItems
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales", t => t.HasCheckConstraint("CK_Sales_TotalAmount", "\"TotalAmount\" >= 0"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.SaleNumber).HasDefaultValueSql("nextval('\"SaleNumbers\"')");
        builder.HasIndex(s => s.SaleNumber).IsUnique();

        builder.Property(s => s.SaleDate).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.BranchName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.TotalAmount).HasColumnType("numeric(18,2)");

        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.BranchId);
        builder.HasIndex(s => s.SaleDate);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
