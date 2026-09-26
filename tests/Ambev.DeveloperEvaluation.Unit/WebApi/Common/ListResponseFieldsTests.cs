using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches.ListBranches;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.ListDiscountPolicies;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: TASK-027 (FEAT-011), TASK-063 (FEAT-001)
/// <summary>
/// Pins the list response fields that clients filter and order by to the entity properties the ORM queries.
/// </summary>
public class ListResponseFieldsTests
{
    // Work item: TASK-063 (FEAT-001)
    /// <summary>
    /// Gets each list response with its entity.
    /// </summary>
    public static TheoryData<Type, Type> ListResponses => new()
    {
        { typeof(ListCustomersResponse), typeof(Customer) },
        { typeof(ListBranchesResponse), typeof(Branch) },
        { typeof(ListProductsResponse), typeof(Product) },
        { typeof(ListSalesResponse), typeof(Sale) },
        { typeof(DiscountPolicyListFields), typeof(DiscountPolicy) }
    };

    /// <summary>
    /// Tests that every list response field exists on the entity with the same type.
    /// </summary>
    [Theory(DisplayName = "Given a list response When comparing it with its entity Then every field exists with the same type")]
    [MemberData(nameof(ListResponses))]
    public void Given_ListResponse_When_ComparingWithEntity_Then_EveryFieldExistsWithSameType(Type response, Type entity)
    {
        // Act
        var mismatches = response.GetProperties()
            .Where(field => entity.GetProperty(field.Name)?.PropertyType != field.PropertyType)
            .Select(field => field.Name);

        // Assert
        mismatches.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the parser supports every list response field type.
    /// </summary>
    [Theory(DisplayName = "Given a list response When checking its field types Then the parser supports all of them")]
    [MemberData(nameof(ListResponses))]
    public void Given_ListResponse_When_CheckingFieldTypes_Then_ParserSupportsAllOfThem(Type response, Type entity)
    {
        // Act
        var unsupported = response.GetProperties()
            .Where(field => !ListQueryParser.SupportedTypes.Contains(field.PropertyType))
            .Select(field => $"{entity.Name}.{field.Name}");

        // Assert
        unsupported.Should().BeEmpty();
    }
}
