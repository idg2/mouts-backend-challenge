using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Application.Common;
using FluentAssertions;
using MediatR;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Common;

// Work item: TD-006
/// <summary>
/// Checks which Application commands are marked with <see cref="ITransactionalCommand"/>.
/// </summary>
public class TransactionalCommandsTests
{
    private static readonly string[] WritePrefixes = ["Create", "Update", "Delete"];

    private static readonly IEnumerable<Type> Commands = typeof(ApplicationLayer).Assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Command")
            && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));

    /// <summary>
    /// Tests that every create, update, and delete command runs in a transaction.
    /// </summary>
    [Fact(DisplayName = "Given the write commands When checking their markers Then all are transactional")]
    public void Given_WriteCommands_When_CheckingMarkers_Then_AllAreTransactional()
    {
        // Arrange
        var writeCommands = Commands.Where(IsWrite).ToList();

        // Act
        var unmarked = writeCommands.Where(type => !typeof(ITransactionalCommand).IsAssignableFrom(type)).Select(type => type.Name);

        // Assert
        writeCommands.Should().NotBeEmpty();
        unmarked.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that read commands do not open a transaction.
    /// </summary>
    [Fact(DisplayName = "Given the read commands When checking their markers Then none is transactional")]
    public void Given_ReadCommands_When_CheckingMarkers_Then_NoneIsTransactional()
    {
        // Arrange
        var readCommands = Commands.Where(type => !IsWrite(type)).ToList();

        // Act
        var marked = readCommands.Where(type => typeof(ITransactionalCommand).IsAssignableFrom(type)).Select(type => type.Name);

        // Assert
        readCommands.Should().NotBeEmpty();
        marked.Should().BeEmpty();
    }

    private static bool IsWrite(Type type) => WritePrefixes.Any(type.Name.StartsWith);
}
