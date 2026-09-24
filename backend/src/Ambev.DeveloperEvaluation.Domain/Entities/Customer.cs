using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Represents a customer. Sales reference customers by id and keep a copy of the name.
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Validates the customer against the <see cref="CustomerValidator"/> rules.
    /// </summary>
    /// <returns>The validation result with any errors found.</returns>
    public ValidationResultDetail Validate()
    {
        var validator = new CustomerValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }
}
