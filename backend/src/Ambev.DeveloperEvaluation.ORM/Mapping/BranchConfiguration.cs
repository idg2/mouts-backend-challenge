using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// EF Core mapping for the Branch entity
/// </summary>
public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    /// <summary>
    /// Configures the Branches table
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
    }
}
