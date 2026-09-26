using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.ORM;
using Bogus;
using Bogus.Extensions.Brazil;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Npgsql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

// Work item: TD-043
/// <summary>
/// Hosts the whole API in process against throwaway databases: a PostgreSQL database that the API migrates and seeds
/// (the administrator and the default discount policy), and MongoDB databases for the queue, the read model, and the
/// logs. The server addresses and credentials come from the WebApi appsettings (and environment variables); only the
/// database names change. Every database is dropped when the tests of the class end.
/// </summary>
public sealed class SalesApiFixture : IAsyncLifetime
{
    private readonly Faker _faker = new();
    private IConfiguration _configuration = null!;
    private Dictionary<string, string?> _overrides = null!;
    private SalesApiFactory _factory = null!;

    /// <summary>Gets a client that sends the seeded administrator's token.</summary>
    public HttpClient Client { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _configuration = AppConfiguration();
        var suffix = Guid.NewGuid().ToString("N");
        _overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = new NpgsqlConnectionStringBuilder(Required("ConnectionStrings:DefaultConnection"))
            {
                Database = $"functional_{suffix}"
            }.ConnectionString,
            ["ConnectionStrings:MessageBus"] = new MongoUrlBuilder(Required("ConnectionStrings:MessageBus"))
            {
                DatabaseName = $"functional_bus_{suffix}"
            }.ToString(),
            ["ReadModel:Database"] = $"functional_read_{suffix}",
            ["LogStorage:Database"] = $"functional_logs_{suffix}"
        };

        _factory = new SalesApiFactory(_overrides);
        Client = _factory.CreateClient();
        var login = await Client.PostAsJsonAsync("/api/auth",
            new { email = Required("Seed:Admin:Email"), password = Required("Seed:Admin:Password") });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("token").GetString();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates a customer, a branch, and a product through the API and returns their ids.
    /// </summary>
    /// <param name="unitPrice">The product's catalog price</param>
    public async Task<(Guid CustomerId, Guid BranchId, Guid ProductId)> CreateReferencesAsync(decimal unitPrice)
    {
        // Faker.Person stays the same person for the life of the Faker, so each customer gets a new one.
        var person = new Person();
        var customerId = await CreateAsync("/api/customers", new { name = person.FullName, document = person.Cpf(includeFormatSymbols: false) });
        var branchId = await CreateAsync("/api/branches", new { name = _faker.Address.City() });
        var productId = await CreateProductAsync(unitPrice);
        return (customerId, branchId, productId);
    }

    /// <summary>
    /// Creates a product through the API and returns its id.
    /// </summary>
    /// <param name="unitPrice">The product's catalog price</param>
    public Task<Guid> CreateProductAsync(decimal unitPrice) =>
        CreateAsync("/api/products",
            new { code = _faker.Random.AlphaNumeric(10).ToUpperInvariant(), description = _faker.Commerce.ProductName(), unitPrice });

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();

        var options = new DbContextOptionsBuilder<DefaultContext>().UseNpgsql(_overrides["ConnectionStrings:DefaultConnection"]).Options;
        await using (var context = new DefaultContext(options))
            await context.Database.EnsureDeletedAsync();

        var bus = MongoUrl.Create(_overrides["ConnectionStrings:MessageBus"]);
        await new MongoClient(bus).DropDatabaseAsync(bus.DatabaseName);
        var readModel = new MongoClient(Required("ConnectionStrings:ReadModel"));
        await readModel.DropDatabaseAsync(_overrides["ReadModel:Database"]);
        var logs = new MongoClient(Required("ConnectionStrings:LogStorage"));
        await logs.DropDatabaseAsync(_overrides["LogStorage:Database"]);
    }

    private async Task<Guid> CreateAsync(string route, object body)
    {
        var response = await Client.PostAsJsonAsync(route, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();
    }

    private string Required(string key) =>
        !string.IsNullOrWhiteSpace(_configuration[key])
            ? _configuration[key]!
            : throw new InvalidOperationException($"{key} is not configured. Set it in the WebApi appsettings or as an environment variable.");

    /// <summary>
    /// Reads the WebApi appsettings and the environment, the configuration the API itself starts with.
    /// </summary>
    internal static IConfiguration AppConfiguration()
    {
        var webApiDirectory = Path.Combine(SolutionDirectory(), "src", "Ambev.DeveloperEvaluation.WebApi");
        return new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string SolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ambev.DeveloperEvaluation.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate Ambev.DeveloperEvaluation.sln from the test output directory");
    }

    // Work item: TD-043
    /// <summary>
    /// The API host. The overrides enter as host configuration before Program runs, the only way they reach
    /// AddDbContext and AddMessaging.
    /// </summary>
    internal sealed class SalesApiFactory : WebApplicationFactory<WebApi.Program>
    {
        private readonly IReadOnlyDictionary<string, string?> _overrides;

        public SalesApiFactory(IReadOnlyDictionary<string, string?> overrides)
        {
            _overrides = overrides;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseSolutionRelativeContentRoot("src/Ambev.DeveloperEvaluation.WebApi");

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(_overrides));
            return base.CreateHost(builder);
        }
    }
}
