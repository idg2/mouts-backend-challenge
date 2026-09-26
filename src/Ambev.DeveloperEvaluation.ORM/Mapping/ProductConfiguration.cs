using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// EF Core mapping for the Product entity
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    // Work item: TASK-016 (FEAT-010), FEAT-013
    /// <summary>
    /// Configures the Products table
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t => t.HasCheckConstraint("CK_Products_UnitPrice", "\"UnitPrice\" > 0"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Description).IsRequired().HasMaxLength(200);
        builder.Property(p => p.UnitPrice).HasColumnType("numeric(18,2)");
    }
}
