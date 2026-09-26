using Ambev.DeveloperEvaluation.WebApi.Features.Branches.ListBranches;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.ListDiscountPolicies;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features;

// Work item: TD-007 (FEAT-010), TASK-063 (FEAT-001)
/// <summary>
/// Contains unit tests for the page and size limits of the customer, branch, product, and sale list request validators.
/// </summary>
public class ListRequestValidatorsTests
{
    // Work item: TASK-063 (FEAT-001)
    /// <summary>
    /// The list resources under test.
    /// </summary>
    public static TheoryData<string> Resources => new() { "customers", "branches", "products", "sales", "discount-policies" };

    /// <summary>
    /// Tests that the first page, the largest page number, and sizes 1 and 100 pass.
    /// </summary>
    [Theory(DisplayName = "Given page and size within limits When validated Then is valid")]
    [MemberData(nameof(Resources))]
    public void Given_PageAndSizeWithinLimits_When_Validated_Then_IsValid(string resource)
    {
        // Act
        var results = new[] { Validate(resource, 1, 1), Validate(resource, 1, 100), Validate(resource, int.MaxValue, 100) };

        // Assert
        results.Should().OnlyContain(r => r.IsValid);
    }

    /// <summary>
    /// Tests that page 0, size 0, and size 101 are rejected.
    /// </summary>
    [Theory(DisplayName = "Given page or size out of limits When validated Then is invalid")]
    [MemberData(nameof(Resources))]
    public void Given_PageOrSizeOutOfLimits_When_Validated_Then_IsInvalid(string resource)
    {
        // Act
        var page0 = Validate(resource, 0, 10);
        var size0 = Validate(resource, 1, 0);
        var size101 = Validate(resource, 1, 101);

        // Assert
        page0.Errors.Select(e => e.PropertyName).Should().Contain("Page");
        size0.Errors.Select(e => e.PropertyName).Should().Contain("Size");
        size101.Errors.Select(e => e.PropertyName).Should().Contain("Size");
    }

    // Work item: TASK-063 (FEAT-001)
    private static ValidationResult Validate(string resource, int page, int size) => resource switch
    {
        "customers" => new ListCustomersRequestValidator().Validate(new ListCustomersRequest { Page = page, Size = size }),
        "branches" => new ListBranchesRequestValidator().Validate(new ListBranchesRequest { Page = page, Size = size }),
        "products" => new ListProductsRequestValidator().Validate(new ListProductsRequest { Page = page, Size = size }),
        "discount-policies" => new ListDiscountPoliciesRequestValidator().Validate(new ListDiscountPoliciesRequest { Page = page, Size = size }),
        _ => new ListSalesRequestValidator().Validate(new ListSalesRequest { Page = page, Size = size })
    };
}
