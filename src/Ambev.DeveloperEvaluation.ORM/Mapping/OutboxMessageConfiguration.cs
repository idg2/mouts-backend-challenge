using Ambev.DeveloperEvaluation.ORM.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// EF Core mapping for the OutboxMessages table.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <summary>
    /// Configures the table, the identity dispatch order, the jsonb payload, and the index of pending rows.
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasColumnType("uuid").ValueGeneratedNever();

        builder.Property(message => message.Sequence).UseIdentityByDefaultColumn();
        builder.Property(message => message.Type).IsRequired().HasMaxLength(100);
        builder.Property(message => message.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(message => message.OccurredAt).HasColumnType("timestamp with time zone");
        builder.Property(message => message.ProcessedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(message => message.Sequence)
            .HasDatabaseName("IX_OutboxMessages_Pending")
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}
