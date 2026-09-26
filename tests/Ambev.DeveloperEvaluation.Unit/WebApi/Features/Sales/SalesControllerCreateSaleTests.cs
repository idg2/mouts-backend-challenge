using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Rebus.Bus;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

// Work item: TASK-039 (FEAT-006)
/// <summary>
/// Contains unit tests for <see cref="SalesController.CreateSale"/>: the synchronous and the queued modes.
/// </summary>
public class SalesControllerCreateSaleTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IBus _bus = Substitute.For<IBus>();
    private readonly SalesController _controller;

    /// <summary>
    /// Initializes the controller with the real request and response mappings and substituted mediator and bus.
    /// </summary>
    public SalesControllerCreateSaleTests()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CreateSaleProfile>();
            cfg.AddProfile<SaleResponseProfile>();
        }).CreateMapper();

        _controller = new SalesController(_mediator, mapper, _bus)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    /// <summary>
    /// Tests that respond-async queues the command under a new id and answers 202 pointing at the future sale.
    /// </summary>
    [Fact(DisplayName = "Given Prefer respond-async When creating sale Then queues the command and answers 202")]
    public async Task Given_PreferRespondAsync_When_CreatingSale_Then_QueuesCommandAndAnswers202()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = await _controller.CreateSale(request, "respond-async", CancellationToken.None);

        // Assert
        var sent = _bus.ReceivedCalls().Should().ContainSingle().Which.GetArguments()[0]
            .Should().BeOfType<CreateSaleCommand>().Subject;
        sent.Id.Should().HaveValue().And.NotBe(Guid.Empty);
        sent.CustomerId.Should().Be(request.CustomerId);
        sent.Items.Should().ContainSingle().Which.ProductId.Should().Be(request.Items[0].ProductId);

        var accepted = result.Should().BeOfType<AcceptedAtActionResult>().Subject;
        accepted.ActionName.Should().Be(nameof(SalesController.GetSale));
        accepted.RouteValues!["id"].Should().Be(sent.Id!.Value);
        var body = accepted.Value.Should().BeOfType<ApiResponseWithData<SaleAcceptedResponse>>().Subject;
        body.Success.Should().BeTrue();
        body.Message.Should().Be("Sale sent for processing");
        body.Data!.Id.Should().Be(sent.Id!.Value);
        _controller.Response.Headers["Preference-Applied"].ToString().Should().Be("respond-async");
        await _mediator.DidNotReceive().Send(Arg.Any<CreateSaleCommand>(), Arg.Any<CancellationToken>());
    }

    // Work item: BUG-013
    /// <summary>
    /// Tests that without respond-async the sale is created synchronously, with no preset id, and nothing is queued,
    /// and that the 201 points at the new sale.
    /// </summary>
    [Theory(DisplayName = "Given no respond-async When creating sale Then creates it synchronously without an id and answers 201")]
    [InlineData(null)]
    [InlineData("return=minimal")]
    public async Task Given_NoRespondAsync_When_CreatingSale_Then_CreatesSynchronouslyWithoutIdAndAnswers201(string? prefer)
    {
        // Arrange
        var request = ValidRequest();
        var saleId = Guid.NewGuid();
        _mediator.Send(Arg.Any<CreateSaleCommand>(), Arg.Any<CancellationToken>())
            .Returns(new SaleResult { Id = saleId });

        // Act
        var result = await _controller.CreateSale(request, prefer, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(SalesController.GetSale));
        created.RouteValues!["id"].Should().Be(saleId);
        await _mediator.Received(1).Send(
            Arg.Is<CreateSaleCommand>(c => c.Id == null && c.CustomerId == request.CustomerId),
            Arg.Any<CancellationToken>());
        _bus.ReceivedCalls().Should().BeEmpty();
        _controller.Response.Headers.ContainsKey("Preference-Applied").Should().BeFalse();
    }

    /// <summary>
    /// Tests that an invalid body is rejected before anything is queued.
    /// </summary>
    [Fact(DisplayName = "Given respond-async and an invalid body When creating sale Then answers 400 and queues nothing")]
    public async Task Given_RespondAsyncAndInvalidBody_When_CreatingSale_Then_Answers400AndQueuesNothing()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [];

        // Act
        var result = await _controller.CreateSale(request, "respond-async", CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _bus.ReceivedCalls().Should().BeEmpty();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateSaleCommand>(), Arg.Any<CancellationToken>());
    }

    // Work item: TASK-064 (FEAT-001)
    private static CreateSaleRequest ValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        BranchId = Guid.NewGuid(),
        Items = [new CreateSaleItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
    };
}
