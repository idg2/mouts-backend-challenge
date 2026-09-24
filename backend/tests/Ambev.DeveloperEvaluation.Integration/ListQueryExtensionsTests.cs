using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-024 (FEAT-011)
/// <summary>
/// Contains integration tests for the translation of list filters and sort fields by <see cref="ListQueryExtensions"/>.
/// Each test tags its rows with a unique marker and filters on it, because the test class shares one database.
/// </summary>
public class ListQueryExtensionsTests : IClassFixture<PostgresFixture>
{
    private static readonly SortField[] ByName = [new("Name", false)];
    private static readonly SortField[] ByCode = [new("Code", false)];
    private static readonly SortField[] BySaleDate = [new("SaleDate", false)];

    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public ListQueryExtensionsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    // Work item: TASK-024 (FEAT-011), FEAT-012
    /// <summary>
    /// Tests that an exact text filter ignores case.
    /// </summary>
    [Fact(DisplayName = "Given text in another case When filtering exactly Then matches it")]
    public async Task Given_TextInAnotherCase_When_FilteringExact_Then_MatchesIt()
    {
        // Arrange
        var marker = NewMarker();
        await SeedAsync(new Customer { Name = $"{marker} Acme Market", Document = TestDocuments.Next() }, new Customer { Name = $"{marker} Acme Market Two", Document = TestDocuments.Next() });

        // Act
        var customers = await ListAsync<Customer>([Like("Name", $"{marker} ACME MARKET")], [], ByName);

        // Assert
        Assert.Equal([$"{marker} Acme Market"], customers.Select(customer => customer.Name));
    }

    // Work item: TASK-024 (FEAT-011), FEAT-012
    /// <summary>
    /// Tests that leading and trailing wildcards match the texts that fit them.
    /// </summary>
    [Fact(DisplayName = "Given texts When filtering with wildcards Then matches the ones that fit")]
    public async Task Given_Texts_When_FilteringWithWildcards_Then_MatchesTheOnesThatFit()
    {
        // Arrange
        var marker = NewMarker();
        await SeedAsync(
            new Customer { Name = $"{marker} Acme Market", Document = TestDocuments.Next() },
            new Customer { Name = $"{marker} Acme Store", Document = TestDocuments.Next() },
            new Customer { Name = $"{marker} Beta Market", Document = TestDocuments.Next() });

        // Act
        var startingWithAcme = await ListAsync<Customer>([Like("Name", $"{marker} acme%")], [], ByName);
        var endingWithMarket = await ListAsync<Customer>([Like("Name", $"{marker}%market")], [], ByName);

        // Assert
        Assert.Equal([$"{marker} Acme Market", $"{marker} Acme Store"], startingWithAcme.Select(customer => customer.Name));
        Assert.Equal([$"{marker} Acme Market", $"{marker} Beta Market"], endingWithMarket.Select(customer => customer.Name));
    }

    // Work item: TASK-024 (FEAT-011), FEAT-012
    /// <summary>
    /// Tests that escaped LIKE metacharacters match only themselves.
    /// </summary>
    [Fact(DisplayName = "Given LIKE metacharacters in a value When filtering escaped Then matches them literally")]
    public async Task Given_LikeMetacharactersInValue_When_FilteringEscaped_Then_MatchesThemLiterally()
    {
        // Arrange
        var marker = NewMarker();
        await SeedAsync(
            new Customer { Name = $"{marker} a_b", Document = TestDocuments.Next() },
            new Customer { Name = $"{marker} acb", Document = TestDocuments.Next() },
            new Customer { Name = $"{marker} 100%", Document = TestDocuments.Next() },
            new Customer { Name = $"{marker} 1000", Document = TestDocuments.Next() });

        // Act
        var underscore = await ListAsync<Customer>([Like("Name", $"{marker} a\\_b")], [], ByName);
        var percent = await ListAsync<Customer>([Like("Name", $"{marker} 100\\%")], [], ByName);

        // Assert
        Assert.Equal([$"{marker} a_b"], underscore.Select(customer => customer.Name));
        Assert.Equal([$"{marker} 100%"], percent.Select(customer => customer.Name));
    }

    // Work item: TASK-024 (FEAT-011), FEAT-012
    /// <summary>
    /// Tests that a value with a SQL quote is sent as a parameter and matches.
    /// </summary>
    [Fact(DisplayName = "Given a quote in the value When filtering Then matches it")]
    public async Task Given_QuoteInValue_When_Filtering_Then_MatchesIt()
    {
        // Arrange
        var marker = NewMarker();
        await SeedAsync(new Customer { Name = $"{marker} O'Brien Market", Document = TestDocuments.Next() });

        // Act
        var customers = await ListAsync<Customer>([Like("Name", $"{marker} o'brien%")], [], ByName);

        // Assert
        Assert.Equal([$"{marker} O'Brien Market"], customers.Select(customer => customer.Name));
    }

    /// <summary>
    /// Tests that repeated matches on one field are combined with OR and other filters with AND.
    /// </summary>
    [Fact(DisplayName = "Given a repeated field and another field When filtering Then ORs the repeated and ANDs the rest")]
    public async Task Given_RepeatedFieldAndOtherField_When_Filtering_Then_OrsTheRepeatedAndAndsTheRest()
    {
        // Arrange
        var marker = NewMarker();
        await SeedProductsAsync(marker);

        // Act
        var products = await ListAsync<Product>(
            [Like("Code", $"{marker}-1"), Like("Code", $"{marker}-3"), new("UnitPrice", FilterOperator.GreaterThanOrEqual, 10m)],
            [],
            ByCode);

        // Assert
        Assert.Equal([$"{marker}-3"], products.Select(product => product.Code));
    }

    /// <summary>
    /// Tests that a closed numeric range includes both bounds.
    /// </summary>
    [Fact(DisplayName = "Given prices When filtering a closed range Then both bounds are included")]
    public async Task Given_Prices_When_FilteringClosedRange_Then_BothBoundsAreIncluded()
    {
        // Arrange
        var marker = NewMarker();
        await SeedProductsAsync(marker);

        // Act
        var products = await ListAsync<Product>(
            [
                Like("Code", $"{marker}%"),
                new("UnitPrice", FilterOperator.GreaterThanOrEqual, 10m),
                new("UnitPrice", FilterOperator.LessThanOrEqual, 20m)
            ],
            [],
            ByCode);

        // Assert
        Assert.Equal([$"{marker}-2", $"{marker}-3"], products.Select(product => product.Code));
    }

    /// <summary>
    /// Tests that a day filter and a half-open day range keep only instants of that UTC day.
    /// </summary>
    [Fact(DisplayName = "Given sales around midnight When filtering one UTC day Then keeps only that day")]
    public async Task Given_SalesAroundMidnight_When_FilteringOneUtcDay_Then_KeepsOnlyThatDay()
    {
        // Arrange
        var marker = NewMarker();
        var day = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
        await SeedAsync(
            NewSale(marker, day.AddSeconds(-1)),
            NewSale(marker, day),
            NewSale(marker, day.AddDays(1).AddSeconds(-1)),
            NewSale(marker, day.AddDays(1)));

        // Act
        var onDay = await ListAsync<Sale>([Like("CustomerName", marker), new("SaleDate", FilterOperator.OnDay, day)], [], BySaleDate);
        var inRange = await ListAsync<Sale>(
            [
                Like("CustomerName", marker),
                new("SaleDate", FilterOperator.GreaterThanOrEqual, day),
                new("SaleDate", FilterOperator.LessThan, day.AddDays(1))
            ],
            [],
            BySaleDate);

        // Assert
        DateTime[] expected = [day, day.AddDays(1).AddSeconds(-1)];
        Assert.Equal(expected, onDay.Select(sale => sale.SaleDate));
        Assert.Equal(expected, inRange.Select(sale => sale.SaleDate));
    }

    /// <summary>
    /// Tests that id and flag filters match exactly.
    /// </summary>
    [Fact(DisplayName = "Given sales When filtering an id and a flag Then matches exactly")]
    public async Task Given_Sales_When_FilteringIdAndFlag_Then_MatchesExactly()
    {
        // Arrange
        var marker = NewMarker();
        var customerId = Guid.NewGuid();
        var day = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
        await SeedAsync(
            NewSale(marker, day, customerId, isCancelled: false),
            NewSale(marker, day, customerId, isCancelled: true),
            NewSale(marker, day, Guid.NewGuid(), isCancelled: true));

        // Act
        var sales = await ListAsync<Sale>(
            [new("CustomerId", FilterOperator.Equal, customerId), new("IsCancelled", FilterOperator.Equal, true)],
            [],
            BySaleDate);

        // Assert
        var sale = Assert.Single(sales);
        Assert.Equal(customerId, sale.CustomerId);
        Assert.True(sale.IsCancelled);
    }

    /// <summary>
    /// Tests that sort fields apply in turn, each in its own direction.
    /// </summary>
    [Fact(DisplayName = "Given sort fields When ordering Then sorts by each in turn")]
    public async Task Given_SortFields_When_Ordering_Then_SortsByEachInTurn()
    {
        // Arrange
        var marker = NewMarker();
        await SeedAsync(
            new Product { Code = $"{marker}-1", Description = $"{marker} b", UnitPrice = 10m },
            new Product { Code = $"{marker}-2", Description = $"{marker} a", UnitPrice = 10m },
            new Product { Code = $"{marker}-3", Description = $"{marker} c", UnitPrice = 20m });

        // Act
        var products = await ListAsync<Product>(
            [Like("Code", $"{marker}%")],
            [new("UnitPrice", true), new("Description", false)],
            ByCode);

        // Assert
        Assert.Equal([$"{marker}-3", $"{marker}-2", $"{marker}-1"], products.Select(product => product.Code));
    }

    // Work item: TASK-024 (FEAT-011), FEAT-012
    /// <summary>
    /// Tests that without sort fields the default order applies and ties are broken by id.
    /// </summary>
    [Fact(DisplayName = "Given no sort fields When ordering Then uses the default order and breaks ties by id")]
    public async Task Given_NoSortFields_When_Ordering_Then_UsesDefaultOrderAndBreaksTiesById()
    {
        // Arrange
        var marker = NewMarker();
        await SeedAsync(
            new Customer { Name = $"{marker} same", Document = TestDocuments.Next() },
            new Customer { Name = $"{marker} alpha", Document = TestDocuments.Next() },
            new Customer { Name = $"{marker} same", Document = TestDocuments.Next() });

        // Act
        var customers = await ListAsync<Customer>([Like("Name", $"{marker}%")], [], ByName);

        // Assert
        Assert.Equal($"{marker} alpha", customers[0].Name);
        var tied = customers.Skip(1).Select(customer => customer.Id.ToString()).ToList();
        Assert.Equal(tied.OrderBy(id => id, StringComparer.Ordinal), tied);
    }

    // Work item: TASK-024 (FEAT-011), FEAT-012
    /// <summary>
    /// Tests that many matches on one field translate without exhausting the stack.
    /// </summary>
    [Fact(DisplayName = "Given thousands of matches on one field When filtering Then translates and matches")]
    public async Task Given_ThousandsOfMatchesOnOneField_When_Filtering_Then_TranslatesAndMatches()
    {
        // Arrange
        var marker = NewMarker();
        await SeedAsync(new Customer { Name = $"{marker} target", Document = TestDocuments.Next() });
        var filters = Enumerable.Range(0, 2000).Select(value => Like("Name", $"{marker} miss {value}")).ToList();
        filters.Add(Like("Name", $"{marker} target"));

        // Act
        var customers = await ListAsync<Customer>(filters, [], ByName);

        // Assert
        Assert.Equal([$"{marker} target"], customers.Select(customer => customer.Name));
    }

    private static string NewMarker() => Guid.NewGuid().ToString("N");

    private static FieldFilter Like(string field, string pattern) => new(field, FilterOperator.Like, pattern);

    private static Sale NewSale(string marker, DateTime saleDate, Guid customerId = default, bool isCancelled = false) => new()
    {
        SaleDate = saleDate,
        CustomerId = customerId == default ? Guid.NewGuid() : customerId,
        CustomerName = marker,
        BranchId = Guid.NewGuid(),
        BranchName = "Downtown",
        TotalAmount = 10m,
        IsCancelled = isCancelled,
        Items =
        [
            new SaleItem
            {
                LineNumber = 1,
                ProductId = Guid.NewGuid(),
                ProductDescription = "Beer 350ml",
                UnitPrice = 10m,
                Quantity = 1,
                TotalAmount = 10m
            }
        ]
    };

    private Task SeedProductsAsync(string marker) => SeedAsync(
        new Product { Code = $"{marker}-1", Description = $"{marker} beer", UnitPrice = 5m },
        new Product { Code = $"{marker}-2", Description = $"{marker} beer", UnitPrice = 10m },
        new Product { Code = $"{marker}-3", Description = $"{marker} beer", UnitPrice = 20m });

    private async Task SeedAsync(params object[] entities)
    {
        await using var context = _fixture.CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private async Task<List<T>> ListAsync<T>(IReadOnlyList<FieldFilter> filters, IReadOnlyList<SortField> order, IReadOnlyList<SortField> defaultOrder)
        where T : class
    {
        await using var context = _fixture.CreateContext();
        return await context.Set<T>().AsNoTracking().ApplyFilters(filters).ApplyOrder(order, defaultOrder).ToListAsync();
    }
}
