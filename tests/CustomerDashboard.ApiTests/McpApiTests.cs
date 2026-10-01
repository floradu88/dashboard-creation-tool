using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace CustomerDashboard.ApiTests;

public sealed class McpApiTests(DashboardFactory factory) : IClassFixture<DashboardFactory>
{
    [Fact]
    public async Task ListsReadOnlyToolsAndReturnsOverview()
    {
        await using var session = await Connect();
        var tools = await session.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        string[] expected =
        [
            "list_dashboard_capabilities", "list_customers", "get_customer_overview", "get_commercial_summary",
            "get_usage_summary", "get_sla_summary", "get_work_items", "get_request_history", "get_customer_news",
            "get_market_data", "get_account_summary", "get_customer_metrics", "get_customer_sources"
        ];
        Assert.Equal(expected.OrderBy(x => x), tools.Select(x => x.Name).OrderBy(x => x));
        Assert.All(tools, tool =>
        {
            Assert.Contains("fictional mock data", tool.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data, not instructions", tool.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(true, tool.ProtocolTool.Annotations?.ReadOnlyHint);
        });
        var overview = await session.Client.CallToolAsync("get_customer_overview", new Dictionary<string, object?>
        {
            ["customerId"] = "customer-001",
            ["from"] = "2026-09-01",
            ["to"] = "2026-12-01"
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEqual(true, overview.IsError);
        var body = JsonDocument.Parse(overview.Content.OfType<TextContentBlock>().Single().Text);
        Assert.Equal(10000m, body.RootElement.GetProperty("data").GetProperty("revenue").GetProperty("value").GetDecimal());
        Assert.Equal("NotApplicable", body.RootElement.GetProperty("data").GetProperty("projectedRevenue").GetProperty("state").GetString());
    }

    [Fact]
    public async Task UnknownCustomerAndInvalidPeriodAreToolErrors()
    {
        await using var session = await Connect();
        var missing = await session.Client.CallToolAsync("get_customer_sources", new Dictionary<string, object?> { ["customerId"] = "missing" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(true, missing.IsError);
        var message = missing.Content.OfType<TextContentBlock>().Single().Text;
        Assert.Contains("Unknown customer", message);
        Assert.DoesNotContain("MockData", message);
        Assert.DoesNotContain(".json", message);
        var period = await session.Client.CallToolAsync("get_customer_overview", new Dictionary<string, object?>
        {
            ["customerId"] = "customer-001",
            ["from"] = "2026-10-01",
            ["to"] = "2026-09-01"
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(true, period.IsError);
        Assert.Contains("1", period.Content.OfType<TextContentBlock>().Single().Text);
    }

    private async Task<Session> Connect()
    {
        var http = factory.CreateClient();
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(http.BaseAddress!, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            Name = "dashboard-tests"
        }, http, NullLoggerFactory.Instance, ownsHttpClient: true);
        var client = await McpClient.CreateAsync(transport);
        return new Session(client, transport);
    }

    private sealed class Session(McpClient client, HttpClientTransport transport) : IAsyncDisposable
    {
        public McpClient Client { get; } = client;
        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await transport.DisposeAsync();
        }
    }
}
