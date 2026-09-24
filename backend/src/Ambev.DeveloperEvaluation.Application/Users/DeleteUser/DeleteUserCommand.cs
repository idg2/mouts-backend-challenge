using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.DeleteUser;

// Work item: TD-006
/// <summary>
/// Command for deleting a user
/// </summary>
public record DeleteUserCommand : IRequest<DeleteUserResponse>, ITransactionalCommand
{
    /// <summary>
    /// The unique identifier of the user to delete
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of DeleteUserCommand
    /// </summary>
    /// <param name="id">The ID of the user to delete</param>
    public DeleteUserCommand(Guid id)
    {
        Id = id;
    }
}
