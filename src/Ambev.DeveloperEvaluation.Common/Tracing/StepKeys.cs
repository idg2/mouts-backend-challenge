namespace Ambev.DeveloperEvaluation.Common.Tracing;

// Work item: TASK-042 (FEAT-017)
/// <summary>
/// Maps a MediatR request type name to its topic key and to the keys of the shared points its topic documents.
/// Keyed by name, because Common cannot reference Application. Only the sale commands document their commit.
/// </summary>
public static class StepKeys
{
    /// <summary>
    /// The topic printed for a request type that is not in the table.
    /// </summary>
    public const string Unknown = "???";

    private sealed record Entry(string Topic, string? Begin = null, string? Commit = null, string? Rollback = null);

    // Work item: TASK-063 (FEAT-001), TD-032
    private static readonly IReadOnlyDictionary<string, Entry> Table = new Dictionary<string, Entry>
    {
        ["AuthenticateUserCommand"] = new("AUT-LGN"),
        ["CreateUserCommand"] = new("USR-CRT"),
        ["GetUserCommand"] = new("USR-GET"),
        ["DeleteUserCommand"] = new("USR-DEL"),
        ["CreateCustomerCommand"] = new("CUS-CRT"),
        ["GetCustomerCommand"] = new("CUS-GET"),
        ["ListCustomersCommand"] = new("CUS-LST"),
        ["UpdateCustomerCommand"] = new("CUS-UPD"),
        ["DeleteCustomerCommand"] = new("CUS-DEL"),
        ["CreateBranchCommand"] = new("BRN-CRT"),
        ["GetBranchCommand"] = new("BRN-GET"),
        ["ListBranchesCommand"] = new("BRN-LST"),
        ["UpdateBranchCommand"] = new("BRN-UPD"),
        ["DeleteBranchCommand"] = new("BRN-DEL"),
        ["CreateProductCommand"] = new("PRD-CRT"),
        ["GetProductCommand"] = new("PRD-GET"),
        ["ListProductsCommand"] = new("PRD-LST"),
        ["UpdateProductCommand"] = new("PRD-UPD"),
        ["DeleteProductCommand"] = new("PRD-DEL"),
        ["CreateSaleCommand"] = new("SAL-CRT", Commit: "SAL-CRT-12"),
        ["GetSaleCommand"] = new("SAL-GET"),
        ["ListSalesCommand"] = new("SAL-LST"),
        ["UpdateSaleCommand"] = new("SAL-UPD", Commit: "SAL-UPD-16"),
        ["DeleteSaleCommand"] = new("SAL-DEL", Commit: "SAL-DEL-05"),
        ["CreateDiscountPolicyCommand"] = new("DSC-CRT"),
        ["GetDiscountPolicyCommand"] = new("DSC-GET"),
        ["ListDiscountPoliciesCommand"] = new("DSC-LST"),
        ["DisableDiscountPoliciesCommand"] = new("DSC-DIS")
    };

    /// <summary>
    /// Gets the request type names in the table.
    /// </summary>
    public static IReadOnlyCollection<string> RequestTypeNames => Table.Keys.ToList();

    /// <summary>
    /// Returns the topic key of a request type, or <see cref="Unknown"/>.
    /// </summary>
    public static string Topic(string requestTypeName) =>
        Table.TryGetValue(requestTypeName, out var entry) ? entry.Topic : Unknown;

    /// <summary>
    /// Returns the step key of a shared point for a request type: the documented key, null when the topic documents
    /// no step for that point, or <see cref="Unknown"/> when the type is not in the table.
    /// </summary>
    public static string? Resolve(string requestTypeName, SharedPoint point)
    {
        if (!Table.TryGetValue(requestTypeName, out var entry))
            return Unknown;

        return point switch
        {
            SharedPoint.TransactionBegin => entry.Begin,
            SharedPoint.TransactionCommit => entry.Commit,
            SharedPoint.TransactionRollback => entry.Rollback,
            _ => null
        };
    }
}
