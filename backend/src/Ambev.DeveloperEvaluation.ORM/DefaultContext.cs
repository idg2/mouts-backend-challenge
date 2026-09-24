using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Ambev.DeveloperEvaluation.ORM;

public class DefaultContext : DbContext
{
    public DbSet<User> Users { get; set; }

    // Work item: TASK-016 (FEAT-010)
    public DbSet<Customer> Customers { get; set; }

    // Work item: TASK-016 (FEAT-010)
    public DbSet<Branch> Branches { get; set; }

    // Work item: TASK-016 (FEAT-010)
    public DbSet<Product> Products { get; set; }

    // Work item: TASK-016 (FEAT-010)
    public DbSet<Sale> Sales { get; set; }

    public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
    {
    }

    // Work item: TASK-016 (FEAT-010)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("SaleNumbers");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
public class YourDbContextFactory : IDesignTimeDbContextFactory<DefaultContext>
{
    // Work item: TASK-013 (FEAT-010)
    public DefaultContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<DefaultContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(
               connectionString,
               b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
        );

        return new DefaultContext(builder.Options);
    }
}