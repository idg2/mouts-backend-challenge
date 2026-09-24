using Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;
using Ambev.DeveloperEvaluation.Application.Branches.GetBranch;
using Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;
using Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.Application.Products.GetProduct;
using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

// Work item: TD-007 (FEAT-010)
/// <summary>
/// Contains unit tests for the error paths shared by the get, update, and delete handlers of the customer,
/// branch, product, and sale CRUDs: an unknown id raises KeyNotFoundException (404) and an empty id raises
/// ValidationException (400).
/// </summary>
public class NotFoundHandlersTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ISaleRepository _sales = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    /// <summary>
    /// The handler operations under test, as resource and operation names.
    /// </summary>
    public static TheoryData<string> Operations => new()
    {
        "customers/get", "customers/update", "customers/delete",
        "branches/get", "branches/update", "branches/delete",
        "products/get", "products/update", "products/delete",
        "sales/get", "sales/delete"
    };

    /// <summary>
    /// Tests that an id the repository does not know raises KeyNotFoundException.
    /// </summary>
    [Theory(DisplayName = "Given an unknown id When handling Then throws KeyNotFoundException")]
    [MemberData(nameof(Operations))]
    public async Task Given_UnknownId_When_Handled_Then_ThrowsKeyNotFound(string operation)
    {
        // Act
        var act = () => HandleAsync(operation, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that an empty id is rejected before the repository is called.
    /// </summary>
    [Theory(DisplayName = "Given an empty id When handling Then throws ValidationException")]
    [MemberData(nameof(Operations))]
    public async Task Given_EmptyId_When_Handled_Then_ThrowsValidationException(string operation)
    {
        // Act
        var act = () => HandleAsync(operation, Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    private Task HandleAsync(string operation, Guid id) => operation switch
    {
        "customers/get" => new GetCustomerHandler(_customers, _mapper).Handle(new GetCustomerCommand(id), CancellationToken.None),
        "customers/update" => new UpdateCustomerHandler(_customers, _mapper)
            .Handle(new UpdateCustomerCommand { Id = id, Name = "Acme Market" }, CancellationToken.None),
        "customers/delete" => new DeleteCustomerHandler(_customers).Handle(new DeleteCustomerCommand(id), CancellationToken.None),
        "branches/get" => new GetBranchHandler(_branches, _mapper).Handle(new GetBranchCommand(id), CancellationToken.None),
        "branches/update" => new UpdateBranchHandler(_branches, _mapper)
            .Handle(new UpdateBranchCommand { Id = id, Name = "Downtown" }, CancellationToken.None),
        "branches/delete" => new DeleteBranchHandler(_branches).Handle(new DeleteBranchCommand(id), CancellationToken.None),
        "products/get" => new GetProductHandler(_products, _mapper).Handle(new GetProductCommand(id), CancellationToken.None),
        "products/update" => new UpdateProductHandler(_products, _mapper)
            .Handle(new UpdateProductCommand { Id = id, Description = "Beer 350ml", UnitPrice = 10m }, CancellationToken.None),
        "products/delete" => new DeleteProductHandler(_products).Handle(new DeleteProductCommand(id), CancellationToken.None),
        "sales/get" => new GetSaleHandler(_sales, _mapper).Handle(new GetSaleCommand(id), CancellationToken.None),
        _ => new DeleteSaleHandler(_sales).Handle(new DeleteSaleCommand(id), CancellationToken.None)
    };
}
