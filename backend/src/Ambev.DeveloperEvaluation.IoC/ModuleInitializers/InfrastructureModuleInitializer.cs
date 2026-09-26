using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    // Work item: TASK-016 (FEAT-010), TD-006, TASK-028 (FEAT-004), TASK-031 (FEAT-004), TASK-061 (FEAT-001)
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
    }
}