using Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;
using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.CreateDiscountPolicy;
using Ambev.DeveloperEvaluation.WebApi.Features.Products;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;
using Ambev.DeveloperEvaluation.WebApi.Features.Users;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features;

// Work item: BUG-013
/// <summary>
/// Contains unit tests for the Location of the 201 Created responses: every create action points at the GET by id of
/// the resource it created. The sales create is covered by SalesControllerCreateSaleTests.
/// </summary>
public class CreatedLocationTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly Guid _id = Guid.NewGuid();

    /// <summary>
    /// Tests that a created user points at GET /api/users/{id}.
    /// </summary>
    [Fact(DisplayName = "Given a valid user When creating it Then 201 points at GetUser with the new id")]
    public async Task Given_ValidUser_When_Created_Then_LocationPointsAtGetUser()
    {
        // Arrange
        _mediator.Send(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>()).Returns(new CreateUserResult { Id = _id });
        var request = new CreateUserRequest
        {
            Username = "manager", Password = "Secret@123", Phone = "+5511999998888", Email = "manager@example.com",
            Status = UserStatus.Active, Role = UserRole.Manager
        };

        // Act
        var result = await Controller(new UsersController(_mediator, _mapper)).CreateUser(request, CancellationToken.None);

        // Assert
        AssertPointsAt(result, nameof(UsersController.GetUser));
    }

    /// <summary>
    /// Tests that a created customer points at GET /api/customers/{id}.
    /// </summary>
    [Fact(DisplayName = "Given a valid customer When creating it Then 201 points at GetCustomer with the new id")]
    public async Task Given_ValidCustomer_When_Created_Then_LocationPointsAtGetCustomer()
    {
        // Arrange
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>()).Returns(new CreateCustomerResult { Id = _id });
        var request = new CreateCustomerRequest { Name = "Acme Market", Document = "52998224725" };

        // Act
        var result = await Controller(new CustomersController(_mediator, _mapper)).CreateCustomer(request, CancellationToken.None);

        // Assert
        AssertPointsAt(result, nameof(CustomersController.GetCustomer));
    }

    /// <summary>
    /// Tests that a created branch points at GET /api/branches/{id}.
    /// </summary>
    [Fact(DisplayName = "Given a valid branch When creating it Then 201 points at GetBranch with the new id")]
    public async Task Given_ValidBranch_When_Created_Then_LocationPointsAtGetBranch()
    {
        // Arrange
        _mediator.Send(Arg.Any<CreateBranchCommand>(), Arg.Any<CancellationToken>()).Returns(new CreateBranchResult { Id = _id });
        var request = new CreateBranchRequest { Name = "Downtown" };

        // Act
        var result = await Controller(new BranchesController(_mediator, _mapper)).CreateBranch(request, CancellationToken.None);

        // Assert
        AssertPointsAt(result, nameof(BranchesController.GetBranch));
    }

    /// <summary>
    /// Tests that a created product points at GET /api/products/{id}.
    /// </summary>
    [Fact(DisplayName = "Given a valid product When creating it Then 201 points at GetProduct with the new id")]
    public async Task Given_ValidProduct_When_Created_Then_LocationPointsAtGetProduct()
    {
        // Arrange
        _mediator.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>()).Returns(new CreateProductResult { Id = _id });
        var request = new CreateProductRequest { Code = "BEER-350", Description = "Beer 350ml", UnitPrice = 10m };

        // Act
        var result = await Controller(new ProductsController(_mediator, _mapper)).CreateProduct(request, CancellationToken.None);

        // Assert
        AssertPointsAt(result, nameof(ProductsController.GetProduct));
    }

    /// <summary>
    /// Tests that a created discount policy points at GET /api/discount-policies/{id}.
    /// </summary>
    [Fact(DisplayName = "Given a valid discount policy When creating it Then 201 points at GetDiscountPolicy with the new id")]
    public async Task Given_ValidDiscountPolicy_When_Created_Then_LocationPointsAtGetDiscountPolicy()
    {
        // Arrange
        _mediator.Send(Arg.Any<CreateDiscountPolicyCommand>(), Arg.Any<CancellationToken>())
            .Returns(new DiscountPolicyResult { Id = _id });
        var request = new CreateDiscountPolicyRequest
        {
            ProductId = Guid.NewGuid(),
            ValidFrom = DateTime.UtcNow.AddDays(1),
            MaxQuantityPerProduct = 20,
            Tiers =
            [
                new DiscountTierRequest { MinQuantity = 4, MaxQuantity = 9, Percentage = 10m },
                new DiscountTierRequest { MinQuantity = 10, MaxQuantity = 20, Percentage = 20m }
            ]
        };

        // Act
        var result = await Controller(new DiscountPoliciesController(_mediator, _mapper))
            .CreateDiscountPolicy(request, CancellationToken.None);

        // Assert
        AssertPointsAt(result, nameof(DiscountPoliciesController.GetDiscountPolicy));
    }

    private static T Controller<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private void AssertPointsAt(IActionResult result, string actionName)
    {
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.ActionName.Should().Be(actionName);
        created.RouteValues!["id"].Should().Be(_id);
    }
}
