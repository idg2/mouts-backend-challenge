using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-062 (FEAT-001)
/// <summary>
/// Checks the backfill of migration AddSaleItemDiscountSnapshot on a database that already holds sales: its own
/// throwaway database is migrated up to AddDiscountPolicies, a sale is stored with the old columns, and the remaining
/// migrations run over it. The shared <see cref="PostgresFixture"/> cannot be used: it applies every migration at once.
/// </summary>
public sealed class SaleItemDiscountSnapshotMigrationTests : IAsyncLifetime
{
    private readonly string _connectionString = PostgresFixture.NewDatabaseConnectionString();

    /// <summary>
    /// Nothing to prepare: the test migrates the database step by step.
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Drops the throwaway database.
    /// </summary>
    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Tests that items stored before the migration point to the default policy, with their stored percentage as the
    /// ceiling and no requested percentage.
    /// </summary>
    [Fact(DisplayName = "Given items stored before the snapshot columns When migrating Then they point to the default policy with their percentage as ceiling")]
    public async Task Given_ItemsStoredBeforeSnapshot_When_Migrating_Then_BackfilledWithDefaultPolicy()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        await using (var context = CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync("AddDiscountPolicies");
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "Sales" ("Id", "SaleDate", "CustomerId", "CustomerName", "BranchId", "BranchName", "TotalAmount", "IsCancelled")
                VALUES ({0}, TIMESTAMPTZ '2026-09-20 10:00:00+00', {1}, 'Acme Market', {2}, 'Downtown', 55.00, false)
                """,
                saleId, Guid.NewGuid(), Guid.NewGuid());
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "SaleItems" ("Id", "SaleId", "LineNumber", "ProductId", "ProductDescription", "UnitPrice", "Quantity",
                                         "DiscountPercentage", "DiscountAmount", "TotalAmount", "IsCancelled")
                VALUES (gen_random_uuid(), {0}, 1, {1}, 'Beer 350ml', 10.00, 5, 10.00, 5.00, 45.00, false),
                       (gen_random_uuid(), {0}, 2, {2}, 'Soda 2L', 5.00, 2, 0.00, 0.00, 10.00, false)
                """,
                saleId, Guid.NewGuid(), Guid.NewGuid());
        }

        // Act
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        // Assert
        await using var check = CreateContext();
        var items = await check.Set<SaleItem>().AsNoTracking()
            .Where(item => item.SaleId == saleId)
            .OrderBy(item => item.LineNumber)
            .ToListAsync();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal(DiscountPolicyConfiguration.DefaultPolicyId, item.DiscountPolicyId));
        Assert.All(items, item => Assert.Null(item.RequestedDiscountPercentage));
        Assert.Equal(new[] { 10m, 0m }, items.Select(item => item.DiscountCeilingPercentage));
        Assert.Equal(new[] { 10m, 0m }, items.Select(item => item.DiscountPercentage));
        Assert.Equal(new[] { 45m, 10m }, items.Select(item => item.TotalAmount));
    }

    private DefaultContext CreateContext() =>
        new(new DbContextOptionsBuilder<DefaultContext>().UseNpgsql(_connectionString).Options);
}
