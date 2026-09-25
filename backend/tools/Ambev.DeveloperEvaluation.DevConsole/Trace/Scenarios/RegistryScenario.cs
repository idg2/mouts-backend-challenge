using System.Text.Json;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-057 (FEAT-017)
/// <summary>
/// The shape shared by customers, branches, and products: create, duplicate (when the registry has a unique field),
/// get, unknown id, list with a filter, an order, and a page, an invalid filter, a page past the end, update, update
/// to another's unique value, delete, delete again.
/// </summary>
public sealed class RegistryScenario : IScenario
{
    private readonly string _route;
    private readonly Func<ScenarioContext, object> _createBody;
    private readonly Func<ScenarioContext, JsonElement, object> _updateBody;
    private readonly string? _uniqueField;
    private readonly string _filterField;

    /// <summary>
    /// Initializes a new instance of RegistryScenario
    /// </summary>
    /// <param name="name">The scenario name</param>
    /// <param name="route">The API route, such as /api/customers</param>
    /// <param name="createBody">Builds a new valid body</param>
    /// <param name="updateBody">Builds an update body from the created entity's JSON</param>
    /// <param name="uniqueField">The JSON field with a unique index, or null (branches)</param>
    /// <param name="filterField">A text field to filter and order by</param>
    public RegistryScenario(
        string name, string route, Func<ScenarioContext, object> createBody, Func<ScenarioContext, JsonElement, object> updateBody,
        string? uniqueField, string filterField)
    {
        Name = name;
        _route = route;
        _createBody = createBody;
        _updateBody = updateBody;
        _uniqueField = uniqueField;
        _filterField = filterField;
    }

    /// <inheritdoc />
    public string Name { get; }

    // Work item: TASK-057 (FEAT-017), TASK-058 (FEAT-017)
    /// <inheritdoc />
    public string Description => _uniqueField is null
        ? $"{_route}: create, get, list, update, delete"
        : $"{_route}: create, duplicate, get, list, update, delete";

    // Work item: TASK-058 (FEAT-017)
    /// <summary>
    /// Builds a new valid body, as the scenario's create requests do.
    /// </summary>
    /// <param name="context">The scenario context, for the run id and Bogus data</param>
    /// <returns>The body</returns>
    public object CreateBody(ScenarioContext context) => _createBody(context);

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var first = await context.ReadJsonAsync(await context.SendAsync(HttpMethod.Post, _route, _createBody(context)));
        var firstData = first.GetProperty("data");
        var firstId = firstData.GetProperty("id").GetString();
        var second = await context.ReadJsonAsync(await context.SendAsync(HttpMethod.Post, _route, _createBody(context)));
        var secondData = second.GetProperty("data");
        var secondId = secondData.GetProperty("id").GetString();

        if (_uniqueField is not null)
        {
            // The same unique value again: 409 from the handler's check.
            var duplicate = AsDictionary(_createBody(context));
            duplicate[_uniqueField] = firstData.GetProperty(_uniqueField).GetString();
            await context.SendAsync(HttpMethod.Post, _route, duplicate);
        }

        await context.SendAsync(HttpMethod.Get, $"{_route}/{firstId}");
        await context.SendAsync(HttpMethod.Get, $"{_route}/{Guid.NewGuid()}");
        await context.SendAsync(HttpMethod.Get, $"{_route}/not-a-guid");

        var filterValue = firstData.GetProperty(_filterField).GetString()!;
        var order = Uri.EscapeDataString($"{_filterField} desc");
        await context.SendAsync(HttpMethod.Get, $"{_route}?{_filterField}={Uri.EscapeDataString(filterValue)}&_order={order}&_page=1&_size=5");
        await context.SendAsync(HttpMethod.Get, $"{_route}?{_filterField}=*{Uri.EscapeDataString(context.RunId)}*");
        await context.SendAsync(HttpMethod.Get, $"{_route}?noSuchField=1");
        await context.SendAsync(HttpMethod.Get, $"{_route}?_page=0");
        await context.SendAsync(HttpMethod.Get, $"{_route}?_page=999&_size=10");

        await context.SendAsync(HttpMethod.Put, $"{_route}/{firstId}", _updateBody(context, firstData));
        if (_uniqueField is not null)
        {
            // Another entity's unique value: 409 from the update handler's check.
            var clash = AsDictionary(_updateBody(context, firstData));
            clash[_uniqueField] = secondData.GetProperty(_uniqueField).GetString();
            await context.SendAsync(HttpMethod.Put, $"{_route}/{firstId}", clash);
        }

        await context.SendAsync(HttpMethod.Put, $"{_route}/{Guid.NewGuid()}", _updateBody(context, firstData));
        await context.SendAsync(HttpMethod.Delete, $"{_route}/{secondId}");
        await context.SendAsync(HttpMethod.Delete, $"{_route}/{secondId}");
    }

    private static Dictionary<string, object?> AsDictionary(object body) =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(body))!;
}
