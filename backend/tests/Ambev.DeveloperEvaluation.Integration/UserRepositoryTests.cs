using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: BUG-011
/// <summary>
/// Contains integration tests for the e-mail uniqueness in <see cref="UserRepository"/>.
/// </summary>
public class UserRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public UserRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that a create racing past the handler check still fails as a duplicate entry, not as a stored second user.
    /// </summary>
    [Fact(DisplayName = "Given an e-mail already stored When creating a user with it Then throws duplicate entry exception")]
    public async Task Given_EmailAlreadyStored_When_Creating_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var email = $"bug011-{Guid.NewGuid():N}@example.com";
        await using (var seedContext = _fixture.CreateContext())
        {
            await new UserRepository(seedContext).CreateAsync(NewUser(email));
        }
        await using var context = _fixture.CreateContext();
        var repository = new UserRepository(context);

        // Act
        var act = () => repository.CreateAsync(NewUser(email));

        // Assert
        var exception = await Assert.ThrowsAsync<DuplicateEntryException>(act);
        Assert.Equal($"User with email {email} already exists", exception.Message);
    }

    private static User NewUser(string email) => new()
    {
        Username = "bug011",
        Password = "hashed-password",
        Email = email,
        Phone = "+5511999998888",
        Role = UserRole.Customer,
        Status = UserStatus.Active
    };
}
