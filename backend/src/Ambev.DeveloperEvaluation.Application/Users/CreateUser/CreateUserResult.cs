using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.Application.Users.CreateUser;

// Work item: BUG-010
/// <summary>
/// Represents the response returned after successfully creating a new user.
/// </summary>
/// <remarks>
/// This response contains the unique identifier of the newly created user,
/// which can be used for subsequent operations or reference, and the data it was stored with.
/// </remarks>
public class CreateUserResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the newly created user.
    /// </summary>
    /// <value>A GUID that uniquely identifies the created user in the system.</value>
    public Guid Id { get; set; }

    // Work item: BUG-010
    /// <summary>
    /// Gets or sets the user's name, taken from the username.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    // Work item: BUG-010
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    // Work item: BUG-010
    /// <summary>
    /// Gets or sets the user's phone number.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    // Work item: BUG-010
    /// <summary>
    /// Gets or sets the user's role in the system.
    /// </summary>
    public UserRole Role { get; set; }

    // Work item: BUG-010
    /// <summary>
    /// Gets or sets the current status of the user.
    /// </summary>
    public UserStatus Status { get; set; }
}
