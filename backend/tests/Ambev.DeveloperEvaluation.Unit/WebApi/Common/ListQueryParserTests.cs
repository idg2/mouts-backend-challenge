using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: TASK-026 (FEAT-011)
/// <summary>
/// Contains unit tests for the <see cref="ListQueryParser"/> class. Query strings are decoded as ASP.NET Core decodes
/// a request, so the tests use the same text a client sends.
/// </summary>
public class ListQueryParserTests
{
    private static readonly DateTime Day = new(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Tests that a text filter key matches its field in any case and becomes an exact pattern.
    /// </summary>
    [Theory(DisplayName = "Given a text filter in any key case When parsing Then returns an exact pattern")]
    [InlineData("description=Beer")]
    [InlineData("Description=Beer")]
    [InlineData("DESCRIPTION=Beer")]
    public void Given_TextFilterInAnyKeyCase_When_Parsing_Then_ReturnsExactPattern(string queryString)
    {
        // Act
        var (filters, order) = Parse<ListProductsResponse>(queryString);

        // Assert
        filters.Should().Equal(new FieldFilter("Description", FilterOperator.Like, "Beer"));
        order.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that wildcards at the start and end become LIKE wildcards.
    /// </summary>
    [Theory(DisplayName = "Given a wildcard value When parsing Then builds the pattern")]
    [InlineData("Beer*", "Beer%")]
    [InlineData("*350ml", "%350ml")]
    [InlineData("*beer*", "%beer%")]
    [InlineData("*", "%")]
    [InlineData("***", "%")]
    [InlineData("", "")]
    public void Given_WildcardValue_When_Parsing_Then_BuildsPattern(string value, string pattern)
    {
        // Act
        var (filters, _) = Parse<ListProductsResponse>($"description={Uri.EscapeDataString(value)}");

        // Assert
        filters.Should().Equal(new FieldFilter("Description", FilterOperator.Like, pattern));
    }

    /// <summary>
    /// Tests that LIKE metacharacters in the value are escaped.
    /// </summary>
    [Fact(DisplayName = "Given LIKE metacharacters When parsing Then escapes them")]
    public void Given_LikeMetacharacters_When_Parsing_Then_EscapesThem()
    {
        // Act
        var (filters, _) = Parse<ListProductsResponse>($"code={Uri.EscapeDataString("a_b%c\\d*")}");

        // Assert
        filters.Should().Equal(new FieldFilter("Code", FilterOperator.Like, "a\\_b\\%c\\\\d%"));
    }

    /// <summary>
    /// Tests that a wildcard inside the value or on a non-text field is rejected.
    /// </summary>
    [Theory(DisplayName = "Given a misplaced wildcard When parsing Then fails")]
    [InlineData("description=be*er")]
    [InlineData("description=**beer")]
    [InlineData("unitPrice=1*")]
    public void Given_MisplacedWildcard_When_Parsing_Then_Fails(string queryString)
    {
        // Act & Assert
        ErrorCodes<ListProductsResponse>(queryString).Should().Equal("InvalidWildcard");
    }

    /// <summary>
    /// Tests that a repeated field key yields one filter per value.
    /// </summary>
    [Fact(DisplayName = "Given a repeated field When parsing Then returns one filter per value")]
    public void Given_RepeatedField_When_Parsing_Then_ReturnsOneFilterPerValue()
    {
        // Act
        var (filters, _) = Parse<ListProductsResponse>("code=A&code=B*");

        // Assert
        filters.Should().Equal(
            new FieldFilter("Code", FilterOperator.Like, "A"),
            new FieldFilter("Code", FilterOperator.Like, "B%"));
    }

    /// <summary>
    /// Tests that equality values convert to the field type with the invariant culture.
    /// </summary>
    [Theory(DisplayName = "Given an equality value When parsing Then converts it to the field type")]
    [InlineData("isCancelled=TRUE", "IsCancelled")]
    [InlineData("saleNumber=42", "SaleNumber")]
    [InlineData("totalAmount=109.95", "TotalAmount")]
    public void Given_EqualityValue_When_Parsing_Then_ConvertsItToFieldType(string queryString, string field)
    {
        // Arrange
        var expected = new Dictionary<string, object> { ["IsCancelled"] = true, ["SaleNumber"] = 42L, ["TotalAmount"] = 109.95m };

        // Act
        var (filters, _) = Parse<ListSalesResponse>(queryString);

        // Assert
        filters.Should().Equal(new FieldFilter(field, FilterOperator.Equal, expected[field]));
    }

    /// <summary>
    /// Tests that an id filter converts to a Guid.
    /// </summary>
    [Fact(DisplayName = "Given an id value When parsing Then converts it to a Guid")]
    public void Given_IdValue_When_Parsing_Then_ConvertsItToGuid()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var (filters, _) = Parse<ListSalesResponse>($"customerId={customerId.ToString().ToUpperInvariant()}");

        // Assert
        filters.Should().Equal(new FieldFilter("CustomerId", FilterOperator.Equal, customerId));
    }

    /// <summary>
    /// Tests that a value that does not convert to the field type is rejected.
    /// </summary>
    [Theory(DisplayName = "Given an invalid value When parsing Then fails")]
    [InlineData("totalAmount=10,5")]
    [InlineData("totalAmount=1,000.00")]
    [InlineData("totalAmount=abc")]
    [InlineData("saleNumber=1.5")]
    [InlineData("customerId=not-a-guid")]
    [InlineData("isCancelled=yes")]
    [InlineData("saleDate=09/24/2026")]
    [InlineData("saleDate=2026-02-30")]
    [InlineData("_minTotalAmount=10,5")]
    [InlineData("saleDate=05%2F09%2F2026%2010%3A30%20GMT")]
    [InlineData("saleDate=Thu%2C%2024%20Sep%202026%2010%3A30%3A00%20GMT")]
    [InlineData("saleDate=9999-12-31")]
    [InlineData("_maxSaleDate=9999-12-31")]
    public void Given_InvalidValue_When_Parsing_Then_Fails(string queryString)
    {
        // Act & Assert
        ErrorCodes<ListSalesResponse>(queryString).Should().Equal("InvalidValue");
    }

    /// <summary>
    /// Tests that a date without a time filters the whole UTC day.
    /// </summary>
    [Fact(DisplayName = "Given a date without time When parsing an equality Then filters the UTC day")]
    public void Given_DateWithoutTime_When_ParsingEquality_Then_FiltersUtcDay()
    {
        // Act
        var (filters, _) = Parse<ListSalesResponse>("saleDate=2026-09-24");

        // Assert
        filters.Should().Equal(new FieldFilter("SaleDate", FilterOperator.OnDay, Day));
        ((DateTime)filters[0].Value).Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that a date with a time and an offset is an exact UTC instant.
    /// </summary>
    [Fact(DisplayName = "Given a date with time and offset When parsing an equality Then uses the UTC instant")]
    public void Given_DateWithTimeAndOffset_When_ParsingEquality_Then_UsesUtcInstant()
    {
        // Act
        var (filters, _) = Parse<ListSalesResponse>($"saleDate={Uri.EscapeDataString("2026-09-24T10:30:00-03:00")}");

        // Assert
        filters.Should().Equal(new FieldFilter("SaleDate", FilterOperator.Equal, Day.AddHours(13.5)));
        ((DateTime)filters[0].Value).Kind.Should().Be(DateTimeKind.Utc);
    }

    // Work item: TASK-026 (FEAT-011)
    /// <summary>
    /// Tests that ISO 8601 dates with a time, with or without seconds, fraction, or zone, are UTC instants.
    /// </summary>
    [Theory(DisplayName = "Given an ISO date and time When parsing Then returns the UTC instant")]
    [InlineData("2026-09-24T10:30:00Z", 0)]
    [InlineData("2026-09-24T10:30Z", 0)]
    [InlineData("2026-09-24T10:30:00.5Z", 500)]
    [InlineData("2026-09-24T10:30:00", 0)]
    [InlineData("2026-09-24T07:30:00-03:00", 0)]
    public void Given_IsoDateAndTime_When_Parsing_Then_ReturnsUtcInstant(string value, int milliseconds)
    {
        // Act
        var (filters, _) = Parse<ListSalesResponse>($"saleDate={Uri.EscapeDataString(value)}");

        // Assert
        filters.Should().Equal(new FieldFilter("SaleDate", FilterOperator.Equal, Day.AddHours(10.5).AddMilliseconds(milliseconds)));
        ((DateTime)filters[0].Value).Kind.Should().Be(DateTimeKind.Utc);
    }

    // Work item: TASK-026 (FEAT-011)
    /// <summary>
    /// Tests that a field may repeat up to the limit of values.
    /// </summary>
    [Fact(DisplayName = "Given a field repeated up to the limit When parsing Then returns one filter per value")]
    public void Given_FieldRepeatedUpToLimit_When_Parsing_Then_ReturnsOneFilterPerValue()
    {
        // Act
        var (filters, _) = Parse<ListProductsResponse>(string.Join('&', Enumerable.Range(1, 50).Select(value => $"code=C{value}")));

        // Assert
        filters.Should().HaveCount(50);
    }

    // Work item: TASK-026 (FEAT-011)
    /// <summary>
    /// Tests that a field repeated past the limit is rejected before any query is built.
    /// </summary>
    [Fact(DisplayName = "Given a field repeated past the limit When parsing Then fails")]
    public void Given_FieldRepeatedPastLimit_When_Parsing_Then_Fails()
    {
        // Act & Assert
        ErrorCodes<ListProductsResponse>(string.Join('&', Enumerable.Range(1, 51).Select(value => $"code=C{value}")))
            .Should().Equal("TooManyValues");
    }

    /// <summary>
    /// Tests that numeric ranges are inclusive on both ends.
    /// </summary>
    [Fact(DisplayName = "Given numeric range keys When parsing Then returns inclusive bounds")]
    public void Given_NumericRangeKeys_When_Parsing_Then_ReturnsInclusiveBounds()
    {
        // Act
        var (filters, _) = Parse<ListProductsResponse>("_minUnitPrice=10&_maxunitprice=20.5");

        // Assert
        filters.Should().Equal(
            new FieldFilter("UnitPrice", FilterOperator.GreaterThanOrEqual, 10m),
            new FieldFilter("UnitPrice", FilterOperator.LessThanOrEqual, 20.5m));
    }

    /// <summary>
    /// Tests that date-only range keys cover whole UTC days and a range with a time is an inclusive instant.
    /// </summary>
    [Fact(DisplayName = "Given date range keys When parsing Then date-only bounds cover whole days")]
    public void Given_DateRangeKeys_When_Parsing_Then_DateOnlyBoundsCoverWholeDays()
    {
        // Act
        var (days, _) = Parse<ListSalesResponse>("_minSaleDate=2026-09-24&_maxSaleDate=2026-09-24");
        var (instant, _) = Parse<ListSalesResponse>($"_maxSaleDate={Uri.EscapeDataString("2026-09-24T12:00:00Z")}");

        // Assert
        days.Should().Equal(
            new FieldFilter("SaleDate", FilterOperator.GreaterThanOrEqual, Day),
            new FieldFilter("SaleDate", FilterOperator.LessThan, Day.AddDays(1)));
        instant.Should().Equal(new FieldFilter("SaleDate", FilterOperator.LessThanOrEqual, Day.AddHours(12)));
    }

    /// <summary>
    /// Tests that a range on a text, id, or flag field is rejected.
    /// </summary>
    [Theory(DisplayName = "Given a range on a field without order When parsing Then fails")]
    [InlineData("_minCustomerName=a")]
    [InlineData("_minIsCancelled=true")]
    [InlineData("_maxCustomerId=0f8fad5b-d9cb-469f-a165-70867728950e")]
    public void Given_RangeOnFieldWithoutOrder_When_Parsing_Then_Fails(string queryString)
    {
        // Act & Assert
        ErrorCodes<ListSalesResponse>(queryString).Should().Equal("InvalidRange");
    }

    /// <summary>
    /// Tests that a repeated range key is rejected, whatever its case.
    /// </summary>
    [Fact(DisplayName = "Given a repeated range key When parsing Then fails")]
    public void Given_RepeatedRangeKey_When_Parsing_Then_Fails()
    {
        // Act & Assert
        ErrorCodes<ListProductsResponse>("_minUnitPrice=1&_minunitprice=2").Should().Equal("RepeatedRange");
    }

    /// <summary>
    /// Tests that unknown fields and unknown underscore parameters are rejected.
    /// </summary>
    [Theory(DisplayName = "Given an unknown key When parsing Then fails")]
    [InlineData("colour=red", "UnknownField")]
    [InlineData("_minWeight=1", "UnknownField")]
    [InlineData("_limit=5", "UnknownParameter")]
    public void Given_UnknownKey_When_Parsing_Then_Fails(string queryString, string errorCode)
    {
        // Act & Assert
        ErrorCodes<ListProductsResponse>(queryString).Should().Equal(errorCode);
    }

    /// <summary>
    /// Tests that paging keys are left to the page and size binding.
    /// </summary>
    [Fact(DisplayName = "Given only paging keys When parsing Then returns no filters and no order")]
    public void Given_OnlyPagingKeys_When_Parsing_Then_ReturnsNoFiltersAndNoOrder()
    {
        // Act
        var (filters, order) = Parse<ListProductsResponse>("_page=2&_size=5");

        // Assert
        filters.Should().BeEmpty();
        order.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that _order text, as general-api.md writes it, becomes sort fields.
    /// </summary>
    [Theory(DisplayName = "Given order text When parsing Then returns the sort fields")]
    [InlineData("_order=%22unitPrice%20desc,%20description%22")]
    [InlineData("_order=unitPrice+DESC,description+asc")]
    [InlineData("_order=unitPrice%20desc,description")]
    public void Given_OrderText_When_Parsing_Then_ReturnsSortFields(string queryString)
    {
        // Act
        var (_, order) = Parse<ListProductsResponse>(queryString);

        // Assert
        order.Should().Equal(new SortField("UnitPrice", true), new SortField("Description", false));
    }

    /// <summary>
    /// Tests that a blank _order means the default order.
    /// </summary>
    [Theory(DisplayName = "Given a blank order When parsing Then returns no sort fields")]
    [InlineData("_order=")]
    [InlineData("_order=%20%20")]
    [InlineData("_order=%22%22")]
    public void Given_BlankOrder_When_Parsing_Then_ReturnsNoSortFields(string queryString)
    {
        // Act
        var (_, order) = Parse<ListProductsResponse>(queryString);

        // Assert
        order.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a malformed _order is rejected.
    /// </summary>
    [Theory(DisplayName = "Given a malformed order When parsing Then fails")]
    [InlineData("_order=weight", "UnknownField")]
    [InlineData("_order=unitPrice%20up", "InvalidOrder")]
    [InlineData("_order=unitPrice%20desc%20extra", "InvalidOrder")]
    [InlineData("_order=unitPrice,,description", "InvalidOrder")]
    [InlineData("_order=unitPrice,unitPrice%20desc", "InvalidOrder")]
    [InlineData("_order=code&_order=description", "RepeatedOrder")]
    public void Given_MalformedOrder_When_Parsing_Then_Fails(string queryString, string errorCode)
    {
        // Act & Assert
        ErrorCodes<ListProductsResponse>(queryString).Should().Equal(errorCode);
    }

    /// <summary>
    /// Tests that every problem in the query is reported at once.
    /// </summary>
    [Fact(DisplayName = "Given several problems When parsing Then reports all of them")]
    public void Given_SeveralProblems_When_Parsing_Then_ReportsAllOfThem()
    {
        // Act & Assert
        ErrorCodes<ListProductsResponse>("colour=red&_minCode=a&_order=weight")
            .Should().BeEquivalentTo("UnknownField", "InvalidRange", "UnknownField");
    }

    private static (IReadOnlyList<FieldFilter> Filters, IReadOnlyList<SortField> Order) Parse<TResponse>(string queryString) =>
        ListQueryParser.Parse<TResponse>(new QueryCollection(QueryHelpers.ParseQuery("?" + queryString)));

    private static IEnumerable<string> ErrorCodes<TResponse>(string queryString)
    {
        var act = () => Parse<TResponse>(queryString);
        return act.Should().Throw<ValidationException>().Which.Errors.Select(error => error.ErrorCode).ToList();
    }
}
