namespace Ambev.DeveloperEvaluation.Domain.Exceptions;

// Work item: BUG-003
/// <summary>
/// Thrown when an entry would repeat a value that must be unique, such as a user's e-mail or a product's code.
/// </summary>
public class DuplicateEntryException : DomainException
{
    /// <summary>
    /// Initializes a new instance of DuplicateEntryException
    /// </summary>
    /// <param name="message">The message naming the duplicated value</param>
    public DuplicateEntryException(string message) : base(message)
    {
    }
}
