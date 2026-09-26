using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-021 (FEAT-010), TASK-077 (FEAT-003)
/// <summary>
/// Contains unit tests for the <see cref="GetSaleHandler"/> class.
/// </summary>
public class GetSaleHandlerTests
{
    private readonly ISaleReadStore _store;
    private readonly IMapper _mapper;
    private readonly GetSaleHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public GetSaleHandlerTests()
    {
        _store = Substitute.For<ISaleReadStore>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetSaleHandler(_store, _mapper);
    }

    // Work item: TASK-077 (FEAT-003)
    /// <summary>
    /// Tests that a projected sale is returned mapped from its snapshot.
    /// </summary>
    [Fact(DisplayName = "Given a projected sale id When getting sale Then returns the mapped snapshot")]
    public async Task Given_ProjectedId_When_Handled_Then_ReturnsMappedSnapshot()
    {
        // Arrange
        var snapshot = new SaleSnapshot(Guid.NewGuid(), 1, DateTime.UtcNow, Guid.NewGuid(), "Acme Market", Guid.NewGuid(), "Downtown", 10m, false, []);
        var expected = new SaleResult { Id = snapshot.SaleId, SaleNumber = 1 };
        _store.GetAsync(snapshot.SaleId, Arg.Any<CancellationToken>()).Returns(snapshot);
        _mapper.Map<SaleResult>(snapshot).Returns(expected);

        // Act
        var result = await _handler.Handle(new GetSaleCommand(snapshot.SaleId), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }

    // Work item: TASK-077 (FEAT-003)
    /// <summary>
    /// Tests that an absent or deleted sale (the store answers null) is a 404 (Review Focus 5).
    /// </summary>
    [Fact(DisplayName = "Given an id the store does not know When getting sale Then throws KeyNotFoundException")]
    public async Task Given_UnknownId_When_Handled_Then_ThrowsKeyNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _store.GetAsync(id, Arg.Any<CancellationToken>()).Returns((SaleSnapshot?)null);

        // Act
        var act = () => _handler.Handle(new GetSaleCommand(id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*{id}*");
    }
}
