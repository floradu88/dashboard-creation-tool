using System.Net;
using CustomerDashboard.Api.Mcp;
using CustomerDashboard.Application;
using CustomerDashboard.Infrastructure;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("This mock scaffold runs only in Development or Testing. Add authentication before real-data deployment.");
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);
var fixtureArgument = args.FirstOrDefault(argument => argument.StartsWith("--fixtures=", StringComparison.Ordinal));
var root = fixtureArgument is null ? builder.Configuration["MockData:RootPath"] : fixtureArgument["--fixtures=".Length..];
var path = string.IsNullOrWhiteSpace(root) ? Path.Combine(AppContext.BaseDirectory, "MockData")
    : Path.GetFullPath(root, builder.Environment.ContentRootPath);
var minutes = builder.Configuration.GetValue<int>("MockData:FreshnessMinutes", 1440);
if (minutes <= 0) throw new InvalidOperationException("MockData:FreshnessMinutes must be positive.");
var availability = new Dictionary<string, string>();
var freshness = new Dictionary<string, int>();
foreach (var source in JsonFixtureStore.Manifest.Values.Where(x => x != "CustomerCatalog"))
{
    if (builder.Configuration[$"DataSources:{source}:Mode"] is string mode && mode != "Mock")
        throw new InvalidOperationException($"{source}: only Mock mode is implemented.");
    var state = builder.Configuration[$"DataSources:{source}:Availability"] ?? "Available";
    if (state is not ("Available" or "Unavailable"))
        throw new InvalidOperationException($"{source}: availability must be Available or Unavailable.");
    availability[source] = state;
    if (builder.Configuration[$"DataSources:{source}:FreshnessMinutes"] is string raw)
    {
        if (!int.TryParse(raw, out var value) || value <= 0)
            throw new InvalidOperationException($"{source}: FreshnessMinutes must be positive.");
        freshness[source] = value;
    }
}
JsonFixtureStore store;
try
{
    store = new JsonFixtureStore(path);
}
catch (InvalidDataException error) when (args.Contains("--validate"))
{
    Console.Error.WriteLine(error.Message);
    Environment.ExitCode = 1;
    return;
}
if (args.Contains("--validate"))
{
    Console.WriteLine($"Valid. {store.CustomerCount} customers.");
    return;
}
builder.Services.AddSingleton(store);
builder.Services.AddSingleton<ICustomerReader, CustomerDataService>();
builder.Services.AddSingleton<ISalesforceReader, SalesforceDataService>();
builder.Services.AddSingleton<IAlexisReader, AlexisDataService>();
builder.Services.AddSingleton<IJiraReader, JiraDataService>();
builder.Services.AddSingleton<IConfluenceReader, ConfluenceDataService>();
builder.Services.AddSingleton<ISlackReader, SlackDataService>();
builder.Services.AddSingleton<INewsReader, NewsDataService>();
builder.Services.AddSingleton<IMarketReader, MarketDataService>();
builder.Services.AddSingleton<ISourceStatusReader>(sp => new SourceStatusService(store, sp.GetRequiredService<TimeProvider>(), minutes, availability, freshness));
builder.Services.AddScoped<QuerySupport>();
builder.Services.AddScoped<GetCustomersHandler>();
builder.Services.AddScoped<GetOverviewHandler>();
builder.Services.AddScoped<GetCommercialHandler>();
builder.Services.AddScoped<GetUsageHandler>();
builder.Services.AddScoped<GetSlaHandler>();
builder.Services.AddScoped<GetWorkHandler>();
builder.Services.AddScoped<GetRequestsHandler>();
builder.Services.AddScoped<GetNewsHandler>();
builder.Services.AddScoped<GetMarketHandler>();
builder.Services.AddScoped<GetAccountHandler>();
builder.Services.AddScoped<GetMetricsHandler>();
builder.Services.AddScoped<GetSourcesHandler>();
builder.Services.AddScoped<GetCapabilitiesHandler>();
builder.Services.AddMcpServer(options => options.ServerInfo = new Implementation { Name = "customer-dashboard", Version = "1.0.0" })
    .WithHttpTransport()
    .WithTools<DashboardMcpTools>();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mcp"))
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is not null && !IPAddress.IsLoopback(remote))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }
    await next();
});
app.MapControllers();
app.MapMcp("/mcp");
app.MapOpenApi();
app.MapHealthChecks("/health/ready");
app.Run();

public partial class Program { }
