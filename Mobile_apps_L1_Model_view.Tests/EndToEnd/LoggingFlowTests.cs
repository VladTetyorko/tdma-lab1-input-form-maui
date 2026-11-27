using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Serilog;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// The format contract: what Serilog writes with LogFormat (as MauiProgram configures it) is what LogReader
// and LogLineParser read back. A change of the template that breaks the log viewer fails here.
public class LoggingFlowTests : IDisposable
{
    private const string Barcode = "4006381333931";

    private readonly TempLogPath _temp = new();
    private readonly ServiceProvider _app;
    private readonly Serilog.Core.Logger _serilog;

    public LoggingFlowTests()
    {
        var services = new ServiceCollection().AddStockRoomCore().AddFakePlatform();
        services.AddSingleton<ILogPathProvider>(_temp);

        // The same sink settings as MauiProgram.ConfigureLogging
        _serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(_temp.LogDirectory, LogFormat.FileNamePattern),
                outputTemplate: LogFormat.OutputTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: LogFormat.RetainedFiles,
                shared: true)
            .CreateLogger();
        services.AddLogging(builder => builder.AddSerilog(_serilog, dispose: false));
        _app = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _app.Dispose();
        _serilog.Dispose();
        _temp.Dispose();
    }

    private async Task SignInAsManagerAsync()
    {
        var manager = (await _app.GetRequiredService<IUserRepository>().GetByIdAsync(SeedData.ManagerId))!;
        _app.GetRequiredService<ICurrentUserService>().SignIn(manager);
    }

    private Task<IReadOnlyList<LogEntry>> ReadAsync(LogLevel level) =>
        _app.GetRequiredService<ILogReader>().ReadNewestAsync(level);

    [Fact]
    public async Task AUserFlow_LeavesEntriesTheViewerReadsBack_WithLevelsSourcesAndMessages()
    {
        await SignInAsManagerAsync();
        var products = _app.GetRequiredService<IProductService>();

        await products.AddAsync("Cordless Saw", Barcode);
        await products.AddAsync("Another Saw", Barcode);

        var all = await ReadAsync(LogLevel.Debug);
        var added = Assert.Single(all, e => e.Level == LogLevel.Information && e.ShortSource == "ProductService");
        Assert.Contains($"with barcode {Barcode}", added.Message);
        var rejected = Assert.Single(all, e => e.Level == LogLevel.Warning && e.ShortSource == "ProductService");
        Assert.Contains("rejected", rejected.Message);
        Assert.True(added.Timestamp < rejected.Timestamp || added.Timestamp == rejected.Timestamp);
        Assert.Contains(all, e => e.Level == LogLevel.Debug && e.ShortSource == "SqliteProductRepository");

        // Newest first, and the level filter hides the rest
        Assert.Equal(rejected.Message, all.First(e => e.ShortSource == "ProductService").Message);
        var warnings = await ReadAsync(LogLevel.Warning);
        Assert.All(warnings, e => Assert.True(e.Level >= LogLevel.Warning));
        Assert.Contains(warnings, e => e.Message == rejected.Message);
    }

    [Fact]
    public async Task AnExceptionWithAStackTrace_IsOneEntryWithTheTraceAsItsExceptionText()
    {
        var logger = _app.GetRequiredService<ILogger<LoggingFlowTests>>();
        try
        {
            ThrowDeep();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Operation {OperationId} failed", Guid.Empty);
        }

        logger.LogInformation("After the failure");

        var entries = await ReadAsync(LogLevel.Debug);
        Assert.Equal(["After the failure", $"Operation \"{Guid.Empty}\" failed"], entries.Select(e => e.Message));
        var failed = entries[1];
        Assert.Equal(LogLevel.Error, failed.Level);
        Assert.Equal("LoggingFlowTests", failed.ShortSource);
        Assert.StartsWith("System.InvalidOperationException: deep failure", failed.Exception);
        Assert.Contains(nameof(ThrowDeep), failed.Exception);
        Assert.Null(entries[0].Exception);
    }

    [Fact]
    public async Task DiagnosticsTestEntries_AppearAtEveryLevel()
    {
        await SignInAsManagerAsync();

        _app.GetRequiredService<IDiagnosticsService>().WriteTestEntries();

        var entries = await ReadAsync(LogLevel.Debug);
        var levels = entries.Where(e => e.ShortSource == "DiagnosticsService").Select(e => e.Level).Reverse();
        Assert.Equal([LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical], levels);
    }

    private static void ThrowDeep() => throw new InvalidOperationException("deep failure");
}
