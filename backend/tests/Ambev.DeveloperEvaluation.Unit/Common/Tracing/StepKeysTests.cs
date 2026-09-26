using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using FluentAssertions;
using MediatR;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Tracing;

// Work item: TASK-042 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepKeys"/> table.
/// </summary>
public class StepKeysTests
{
    [Fact(DisplayName = "Given the table When checking Then every name is a MediatR request of the Application assembly")]
    public void Given_Table_When_Checking_Then_EveryNameIsARequest()
    {
        // Arrange
        var requests = typeof(ApplicationLayer).Assembly.GetTypes()
            .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Select(type => type.Name)
            .ToHashSet();

        // Assert
        StepKeys.RequestTypeNames.Should().BeSubsetOf(requests);
    }

    [Fact(DisplayName = "Given the transactional commands When checking Then every one is in the table")]
    public void Given_TransactionalCommands_When_Checking_Then_AllInTable()
    {
        // Arrange
        var transactional = typeof(ApplicationLayer).Assembly.GetTypes()
            .Where(type => typeof(ITransactionalCommand).IsAssignableFrom(type) && !type.IsInterface)
            .Select(type => type.Name);

        // Assert
        transactional.Should().BeSubsetOf(StepKeys.RequestTypeNames);
    }

    // Work item: TASK-063 (FEAT-001), TD-032
    [Theory(DisplayName = "Given a command When resolving the commit Then the documented key or null")]
    [InlineData("CreateSaleCommand", "SAL-CRT-12")]
    [InlineData("UpdateSaleCommand", "SAL-UPD-16")]
    [InlineData("DeleteSaleCommand", "SAL-DEL-05")]
    [InlineData("CreateCustomerCommand", null)]
    [InlineData("CreateDiscountPolicyCommand", null)]
    [InlineData("DisableDiscountPoliciesCommand", null)]
    public void Given_Command_When_ResolvingCommit_Then_DocumentedKey(string name, string? expected)
    {
        StepKeys.Resolve(name, SharedPoint.TransactionCommit).Should().Be(expected);
    }

    [Fact(DisplayName = "Given an unknown name When resolving Then ??? for the topic and the point")]
    public void Given_UnknownName_When_Resolving_Then_Unknown()
    {
        StepKeys.Topic("Nope").Should().Be(StepKeys.Unknown);
        StepKeys.Resolve("Nope", SharedPoint.TransactionCommit).Should().Be(StepKeys.Unknown);
    }
}
