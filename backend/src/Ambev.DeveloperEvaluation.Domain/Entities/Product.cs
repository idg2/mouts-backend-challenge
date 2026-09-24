using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Represents a product. Sale items reference products by id and keep a copy of the description and unit price.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Validates the product against the <see cref="ProductValidator"/> rules.
    /// </summary>
    /// <returns>The validation result with any errors found.</returns>
    public ValidationResultDetail Validate()
    {
        var validator = new ProductValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }
}
