namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-054 (FEAT-017)
/// <summary>
/// Reads the sale id carried by any of the five sale events, for logging and tracing.
/// </summary>
public static class SaleEventIds
{
    // Work item: TASK-054 (FEAT-017), TASK-059 (FEAT-017)
    /// <summary>
    /// Returns the sale id of a sale event, or null for anything else (and for a created or modified event without a
    /// snapshot).
    /// </summary>
    /// <param name="message">A sale event, or any other object</param>
    /// <returns>The sale id, or null</returns>
    public static Guid? SaleIdOf(object? message) => message switch
    {
        SaleCreated created => created.Sale?.SaleId,
        SaleModified modified => modified.Sale?.SaleId,
        ItemCancelled itemCancelled => itemCancelled.SaleId,
        SaleCancelled cancelled => cancelled.SaleId,
        SaleDeleted deleted => deleted.SaleId,
        _ => null
    };
}
