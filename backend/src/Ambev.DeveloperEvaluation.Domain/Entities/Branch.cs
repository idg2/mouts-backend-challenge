using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Represents a branch where sales are made. Sales reference branches by id and keep a copy of the name.
/// </summary>
public class Branch : BaseEntity
{
    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Validates the branch against the <see cref="BranchValidator"/> rules.
    /// </summary>
    /// <returns>The validation result with any errors found.</returns>
    public ValidationResultDetail Validate()
    {
        var validator = new BranchValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }
}
