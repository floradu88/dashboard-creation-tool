using CustomerDashboard.Domain;

namespace CustomerDashboard.UnitTests;

public sealed class MeasurementTests
{
    private static readonly ReportingPeriod September = new(new(2026, 9, 1), new(2026, 10, 1));

    [Fact]
    public void SlaWithNoEligibleEventsIsNotApplicable() =>
        Assert.Equal("NotApplicable", DashboardMetrics.SlaCompliance([], new Dictionary<string, int>()).State);

    [Fact]
    public void SlaComplianceIgnoresIneligibleEvents()
    {
        SlaEvent[] events = [
            new("met", "c", "objective", DateTimeOffset.Parse("2026-09-02T00:00:00Z"), 10, true, null),
            new("missed", "c", "objective", DateTimeOffset.Parse("2026-09-03T00:00:00Z"), 90, true, null),
            new("excluded", "c", "objective", DateTimeOffset.Parse("2026-09-04T00:00:00Z"), 1, false, "maintenance")];
        Assert.Equal(50m, DashboardMetrics.SlaCompliance(events, new Dictionary<string, int> { ["objective"] = 60 }).Value);
    }

    [Fact]
    public void SharedLaborIsAllocatedByFractionAndMissingRatesStayUnpriced()
    {
        Worklog[] logs = [
            new("shared", "issue", "worker-001", DateTimeOffset.Parse("2026-09-21T09:00:00Z"), 4, true,
                [new("customer-a", "feature", .25m), new("customer-b", "feature", .75m)]),
            new("missing", "issue", "worker-002", DateTimeOffset.Parse("2026-09-21T09:00:00Z"), 2, true,
                [new("customer-a", "feature", 1m)]),
            new("draft", "issue", "worker-001", DateTimeOffset.Parse("2026-09-21T09:00:00Z"), 9, false,
                [new("customer-a", "feature", 1m)])];
        LaborRate[] rates = [new("rate", "worker-001", new(2026, 1, 1), new(2027, 1, 1), new(100m, "EUR"))];
        FeatureRecord[] features = [new("feature", "customer-a", "Feature"), new("feature", "customer-b", "Feature")];
        var first = DashboardMetrics.DeliveryCost("customer-a", "EUR", "UTC", September, logs, features, rates, []);
        var second = DashboardMetrics.DeliveryCost("customer-b", "EUR", "UTC", September, logs, features, rates, []);
        Assert.Equal(100m, first.Lines.Single().Labor);
        Assert.Equal(2m, first.UncoveredHours);
        Assert.Equal(300m, second.Lines.Single().Labor);
        Assert.Equal(0m, second.UncoveredHours);
    }

    [Fact]
    public void GrowthAndCoverageRefuseZeroBaselines()
    {
        Assert.Equal("NotApplicable", DashboardMetrics.Growth(10m, 0m).State);
        Assert.Equal("NotApplicable", DashboardMetrics.Growth(10m, null).State);
        var target = new MetricTarget("t", "weighted-pipeline", 1, September.From, September.To, 50m, "EUR", "owner");
        Assert.Equal(0.36m, DashboardMetrics.PipelineCoverage(18m, target, "EUR", September).Value);
        Assert.Equal("NotApplicable", DashboardMetrics.PipelineCoverage(18m, target with { Value = 0 }, "EUR", September).State);
    }

    [Fact]
    public void NewsKeepsTheLowestIdForACanonicalUrl()
    {
        NewsArticle[] articles = [
            Article("news-002", "https://news.example/same"),
            Article("news-001", "https://news.example/same")];
        Assert.Equal("news-001", DashboardMetrics.DeduplicateNews(articles).Single().Id);
    }

    private static NewsArticle Article(string id, string url) => new(id, "customer-001", "Headline", "Summary", "Publisher", url,
        DateTimeOffset.Parse("2026-09-24T08:00:00Z"), null, ["ProductLaunch"], new("northstar.example", "ApprovedDomain", true), "Relevant", true);
}
