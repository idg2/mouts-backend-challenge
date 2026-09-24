using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// EF Core mapping for the Customer entity
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    // Work item: TASK-016 (FEAT-010), FEAT-012
    /// <summary>
    /// Configures the Customers table
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        builder.Property(c => c.Document).IsRequired().HasMaxLength(14);
        builder.HasIndex(c => c.Document).IsUnique();
    }
}
