using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Ambev.DeveloperEvaluation.ORM.ReadModel;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    // Work item: TASK-016 (FEAT-010), TD-006, TASK-028 (FEAT-004), TASK-031 (FEAT-004), TASK-061 (FEAT-001), TASK-075 (FEAT-003)
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<IBranchRepository, BranchRepository>();
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
        builder.Services.AddScoped<IDiscountPolicyRepository, DiscountPolicyRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IOutbox, OutboxWriter>();
        builder.Services.AddScoped<IOutboxRelay, OutboxRelay>();

        // The read model client is separate from the Rebus transport and the log sink, which create their own;
        // it connects on first use, so a missing MongoDB fails the first projection or read, not the startup.
        var readModel = ReadModelSettings.FromConfiguration(builder.Configuration);
        builder.Services.AddSingleton(readModel);
        builder.Services.AddSingleton<IMongoClient>(new MongoClient(readModel.ConnectionString));
        builder.Services.AddScoped<ISaleReadStore, SaleReadStore>();
    }
}