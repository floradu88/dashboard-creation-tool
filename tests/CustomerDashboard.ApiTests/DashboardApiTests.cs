using System.Net;
using System.Net.Http.Json;
using CustomerDashboard.Application;
using CustomerDashboard.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerDashboard.ApiTests;

public sealed class FixedClock(DateTimeOffset instant) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => instant;
}

public sealed class DashboardFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing").ConfigureTestServices(services =>
    {
        foreach (var registration in services.Where(x => x.ServiceType == typeof(TimeProvider)).ToArray()) services.Remove(registration);
        services.AddSingleton<TimeProvider>(new FixedClock(DateTimeOffset.Parse("2026-09-29T12:00:00Z")));
    });
}

public sealed class DashboardApiTests : IClassFixture<DashboardFactory>
{
    private readonly HttpClient client;
    public DashboardApiTests(DashboardFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task OverviewLoadsFixturesWithStableExtractionTimes()
    {
        var result = await client.GetFromJsonAsync<DashboardResponse<OverviewData>>("/api/v1/customers/customer-001/overview?from=2026-09-01&to=2026-12-01");
        Assert.NotNull(result);
        Assert.Equal(10000m, result.Data.Revenue.Value);
        Assert.Equal("Value", result.Data.Revenue.State);
        Assert.Equal(18000m, result.Data.WeightedPipeline.Value);
        Assert.Equal("NotApplicable", result.Data.ProjectedRevenue.State);
        Assert.Equal(650m, result.Data.DeliverySpend.Value);
        Assert.Equal(3, result.Data.Trend.Count);
        Assert.Equal(1, result.Meta.SchemaVersion);
        Assert.Contains(result.Data.Attention, item => item.Code == "OverdueAction");
        Assert.All(result.Meta.Sources, source => Assert.Equal(DateTimeOffset.Parse("2026-09-29T08:00:00Z"), source.LastSuccessfulExtractionAtUtc));
        Assert.DoesNotContain(result.Meta.Sources, source => source.Source == "Slack");
    }

    [Fact]
    public async Task EdgeCustomersKeepZeroMissingAndUnavailableDistinct()
    {
        var harbor = await client.GetFromJsonAsync<DashboardResponse<SlaData>>("/api/v1/customers/customer-002/slas?from=2026-09-01&to=2026-10-01");
        Assert.NotNull(harbor);
        Assert.Equal(0m, harbor.Data.Compliance.Value);
        Assert.Contains("sla-event-harbor", harbor.Data.Objectives.Single().BreachEventIds);
        var usage = await client.GetFromJsonAsync<DashboardResponse<UsageData>>("/api/v1/customers/customer-002/usage?from=2026-09-01&to=2026-10-01");
        Assert.Equal("Zero", usage!.Data.Points.Single(x => x.FeatureId == "feature-harbor").Utilization.State);
        var work = await client.GetFromJsonAsync<DashboardResponse<WorkData>>("/api/v1/customers/customer-002/work-items?from=2026-09-01&to=2026-10-01");
        Assert.Equal("Missing", work!.Data.DeliverySpend.State);
        Assert.True(work.Meta.IsPartial);
        var paper = await client.GetFromJsonAsync<DashboardResponse<MarketData>>("/api/v1/customers/customer-003/market-data?from=2026-09-01&to=2026-10-01");
        Assert.Equal("Missing", paper!.Data.Indicators.Single().Benchmark.State);
        Assert.Equal("NotApplicable", paper.Data.Indicators.Single().CustomerComparison.State);
        var growth = await client.GetFromJsonAsync<DashboardResponse<MetricsData>>("/api/v1/customers/customer-003/metrics?from=2026-09-01&to=2026-10-01");
        Assert.Equal("NotApplicable", growth!.Data.Metrics.Single(x => x.Id == "revenue-growth").Actual.State);
    }

    [Fact]
    public async Task NewsCollapsesDuplicateUrlsAndSourcesMarkSlackUnavailable()
    {
        var news = await client.GetFromJsonAsync<DashboardResponse<NewsData>>("/api/v1/customers/customer-001/news?from=2026-09-01&to=2026-10-01");
        Assert.NotNull(news);
        Assert.Equal(2, news.Data.Total);
        Assert.Equal(1, news.Data.DuplicatesRemoved);
        Assert.DoesNotContain(news.Data.Items, item => item.Id == "news-002");
        Assert.All(news.Data.Items, item => Assert.True(item.IsSimulated));
        Assert.Equal("Stale", news.Meta.Sources.Single(x => x.Source == "CustomerNews").Freshness);
        var sources = await client.GetFromJsonAsync<DashboardResponse<SourceListData>>("/api/v1/customers/customer-001/sources");
        Assert.Equal("Unavailable", sources!.Data.Sources.Single(x => x.Source == "Slack").Status);
        var requests = await client.GetFromJsonAsync<DashboardResponse<RequestData>>("/api/v1/customers/customer-001/requests?from=2026-09-01&to=2026-10-01");
        Assert.True(requests!.Meta.IsPartial);
        Assert.Equal(0, requests.Data.Items.Single().RelatedMessages);
    }

    [Theory]
    [InlineData("/api/v1/customers/customer-001/overview?from=2026-10-01&to=2026-09-01", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/customers/customer-001/overview", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/customers/unknown/overview?from=2026-09-01&to=2026-10-01", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/customers/unknown/sources", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/customers?pageSize=101", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/customers/customer-001/work-items?from=2026-09-01&to=2026-10-01&sort=nope", HttpStatusCode.BadRequest)]
    public async Task RejectsInvalidRequests(string path, HttpStatusCode expected) =>
        Assert.Equal(expected, (await client.GetAsync(path)).StatusCode);

    [Fact]
    public async Task CustomersCanBeSearchedAndEmptyResultsAreExplicit()
    {
        var result = await client.GetFromJsonAsync<DashboardResponse<CustomerListData>>("/api/v1/customers?search=missing");
        Assert.NotNull(result);
        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.Total);
    }

    [Fact]
    public async Task OpenApiAndReadinessAreAvailable()
    {
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        var schema = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/v1/customers/{id}/overview", schema);
        Assert.Contains("/api/v1/customers/{id}/metrics", schema);
    }

    [Fact]
    public void MissingFixturesFailBeforeServingData() =>
        Assert.Throws<InvalidDataException>(() => new JsonFixtureStore(Path.Combine(AppContext.BaseDirectory, "missing-fixtures")));
}
