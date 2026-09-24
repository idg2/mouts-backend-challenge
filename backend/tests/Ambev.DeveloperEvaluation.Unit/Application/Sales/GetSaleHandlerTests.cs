using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="GetSaleHandler"/> class.
/// </summary>
public class GetSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly GetSaleHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public GetSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetSaleHandler(_saleRepository, _mapper);
    }

    /// <summary>
    /// Tests that an existing sale is returned with its items.
    /// </summary>
    [Fact(DisplayName = "Given an existing sale id When getting sale Then returns the sale")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsSale()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid(), SaleNumber = 1, CustomerName = "Acme Market", BranchName = "Downtown" };
        var expected = new SaleResult { Id = sale.Id, SaleNumber = sale.SaleNumber };
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<SaleResult>(sale).Returns(expected);

        // Act
        var result = await _handler.Handle(new GetSaleCommand(sale.Id), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }
}
