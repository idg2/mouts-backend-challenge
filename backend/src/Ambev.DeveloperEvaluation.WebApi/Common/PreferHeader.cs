namespace Ambev.DeveloperEvaluation.WebApi.Common;

// Work item: TASK-039 (FEAT-006)
/// <summary>
/// Reads the RFC 7240 Prefer request header.
/// </summary>
public static class PreferHeader
{
    /// <summary>
    /// The preference that asks the server to accept the request and process it later.
    /// </summary>
    public const string RespondAsync = "respond-async";

    /// <summary>
    /// Tells whether a Prefer value asks for asynchronous processing. The value is a comma-separated list of
    /// preferences, each optionally followed by ';' parameters; the token is matched ignoring case and spaces.
    /// </summary>
    /// <param name="prefer">The Prefer header value, or null when absent</param>
    /// <returns>True when respond-async is one of the preferences</returns>
    public static bool RequestsRespondAsync(string? prefer) =>
        !string.IsNullOrWhiteSpace(prefer) &&
        prefer.Split(',').Any(preference =>
            string.Equals(preference.Split(';')[0].Trim(), RespondAsync, StringComparison.OrdinalIgnoreCase));
}
