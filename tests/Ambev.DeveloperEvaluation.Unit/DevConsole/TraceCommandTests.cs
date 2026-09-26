using Ambev.DeveloperEvaluation.DevConsole.Trace;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Contains unit tests for the settings the <see cref="TraceCommand"/> reads and hands to the hosted API.
/// </summary>
public class TraceCommandTests
{
    private const string Connection = "Host=localhost;Port=5432;Database=scratch;Username=u;Password=p";

    private static IConfiguration Configuration(params (string Key, string? Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

    [Fact(DisplayName = "Given command-line overrides When building the host settings Then they and the Serilog level reach the API")]
    public void Given_CommandLineOverrides_When_BuildingHostSettings_Then_ForwardedWithSerilogLevel()
    {
        // Arrange
        var configuration = Configuration(("Trace:AppLogMinimumLevel", "Warning"));
        string[] args = [$"--ConnectionStrings:DefaultConnection={Connection}", "--Seed:Admin:Email=a@b.c"];

        // Act
        var settings = TraceCommand.HostSettings(configuration, args);

        // Assert
        settings.Should().Contain("ConnectionStrings:DefaultConnection", Connection);
        settings.Should().Contain("Seed:Admin:Email", "a@b.c");
        settings.Should().Contain("Serilog:MinimumLevel:Default", "Warning");
        settings.Values.Should().NotContainNulls();
    }

    // Work item: TASK-056 (FEAT-017), TASK-078 (FEAT-003)
    [Fact(DisplayName = "Given databases and admin resolved outside the command line When building the host settings Then they reach the API")]
    public void Given_ResolvedDatabasesAndAdmin_When_BuildingHostSettings_Then_Forwarded()
    {
        // Arrange
        var configuration = Configuration(
            ("Trace:AppLogMinimumLevel", "Warning"),
            ("ConnectionStrings:DefaultConnection", Connection),
            ("ConnectionStrings:MessageBus", "mongodb://u:p@localhost:27017/scratch_bus?authSource=admin"),
            ("ConnectionStrings:ReadModel", "mongodb://u:p@localhost:27017/?authSource=admin"),
            ("ReadModel:Database", "scratch_read"),
            ("Seed:Admin:Email", "secret@b.c"),
            ("Seed:Admin:Password", "S3cret!"),
            ("Jwt:SecretKey", "not-forwarded"));

        // Act
        var settings = TraceCommand.HostSettings(configuration, []);

        // Assert
        settings.Should().Contain("ConnectionStrings:DefaultConnection", Connection);
        settings.Should().Contain("ConnectionStrings:MessageBus", "mongodb://u:p@localhost:27017/scratch_bus?authSource=admin");
        settings.Should().Contain("ConnectionStrings:ReadModel", "mongodb://u:p@localhost:27017/?authSource=admin");
        settings.Should().Contain("ReadModel:Database", "scratch_read");
        settings.Should().Contain("Seed:Admin:Email", "secret@b.c");
        settings.Should().Contain("Seed:Admin:Password", "S3cret!");
        settings.Should().NotContainKey("Jwt:SecretKey");
    }

    [Fact(DisplayName = "Given no app log level When building the host settings Then it throws naming the key")]
    public void Given_NoAppLogLevel_When_BuildingHostSettings_Then_ThrowsNamingKey()
    {
        // Arrange
        var configuration = Configuration(("Trace:AppLogMinimumLevel", ""));

        // Act
        var act = () => TraceCommand.HostSettings(configuration, []);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Trace:AppLogMinimumLevel*");
    }

    [Theory(DisplayName = "Given a blank, malformed, or non-positive wait timeout When reading it Then it throws naming the key")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("soon")]
    [InlineData("00:00:00")]
    [InlineData("-00:00:05")]
    public void Given_BadWaitTimeout_When_Reading_Then_ThrowsNamingKey(string? value)
    {
        // Arrange
        var configuration = Configuration(("Trace:WaitTimeout", value));

        // Act
        var act = () => TraceCommand.WaitTimeout(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Trace:WaitTimeout*");
    }

    [Fact(DisplayName = "Given a positive wait timeout When reading it Then it parses")]
    public void Given_PositiveWaitTimeout_When_Reading_Then_Parses()
    {
        // Arrange
        var configuration = Configuration(("Trace:WaitTimeout", "00:01:30"));

        // Act
        var timeout = TraceCommand.WaitTimeout(configuration);

        // Assert
        timeout.Should().Be(TimeSpan.FromSeconds(90));
    }

    // Work item: TASK-057 (FEAT-017)
    [Fact(DisplayName = "Given a scenario that throws When running the scenarios Then it prints one failure line, runs the next, and reports failure")]
    public async Task Given_ThrowingScenario_When_RunningScenarios_Then_FailureLinePrintedAndNextRuns()
    {
        // Arrange
        var output = new StringWriter();
        var context = Context(output);
        var next = new FakeScenario("next", null);
        IScenario[] scenarios = [new FakeScenario("boom", new InvalidOperationException("line one\nline two")), next];

        // Act
        var allSucceeded = await TraceCommand.RunScenariosAsync(scenarios, context);

        // Assert
        allSucceeded.Should().BeFalse();
        next.Ran.Should().BeTrue();
        output.ToString().Should().Contain("!!! scenario boom failed: InvalidOperationException: line one line two");
        output.ToString().Should().Contain("===== next: fake next (run run1)");
    }

    // Work item: TASK-057 (FEAT-017)
    [Fact(DisplayName = "Given scenarios that all finish When running the scenarios Then no failure line is printed and success is reported")]
    public async Task Given_ScenariosThatFinish_When_RunningScenarios_Then_Success()
    {
        // Arrange
        var output = new StringWriter();
        var first = new FakeScenario("first", null);
        var second = new FakeScenario("second", null);

        // Act
        var allSucceeded = await TraceCommand.RunScenariosAsync([first, second], Context(output));

        // Assert
        allSucceeded.Should().BeTrue();
        first.Ran.Should().BeTrue();
        second.Ran.Should().BeTrue();
        output.ToString().Should().NotContain("!!!");
    }

    // Work item: TASK-057 (FEAT-017), TD-030
    private static ScenarioContext Context(TextWriter output) =>
        new(new HttpClient(), "token", new StepWaiter(), TimeSpan.FromSeconds(1), "run1", output, "admin@example.com", "unused",
            new ServiceCollection().BuildServiceProvider(), new TraceFaults());

    // Work item: TASK-057 (FEAT-017)
    /// <summary>
    /// A scenario that records it ran and optionally throws.
    /// </summary>
    private sealed class FakeScenario : IScenario
    {
        private readonly Exception? _failure;

        public FakeScenario(string name, Exception? failure)
        {
            Name = name;
            _failure = failure;
        }

        public string Name { get; }

        public string Description => $"fake {Name}";

        public bool Ran { get; private set; }

        public Task RunAsync(ScenarioContext context)
        {
            Ran = true;
            return _failure is null ? Task.CompletedTask : Task.FromException(_failure);
        }
    }
}
