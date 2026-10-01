using CustomerDashboard.Domain;
namespace CustomerDashboard.UnitTests;
public sealed class CommercialMetricsTests
{
    private static readonly ReportingPeriod September = new(new(2026, 9, 1), new(2026, 10, 1));
    [Fact]
    public void RevenueUsesInclusiveStartExclusiveEndAndRoundsAtTotal()
    {
        RevenueEntry[] entries = [
            new("a", "customer", new(2026, 9, 1), new(10.005m, "EUR")),
            new("b", "customer", new(2026, 9, 30), new(20.005m, "EUR")),
            new("c", "customer", new(2026, 10, 1), new(999m, "EUR"))];
        Assert.Equal(30.01m, CommercialMetrics.Revenue(entries, September, "EUR"));
    }
    [Fact]
    public void PipelineExcludesWonLostAndOutOfPeriod()
    {
        Opportunity[] records = [
            new("a", "c", "Proposal", new(100m, "EUR"), .6m, new(2026, 9, 10)),
            new("b", "c", "Won", new(500m, "EUR"), 1m, new(2026, 9, 10)),
            new("c", "c", "Lost", new(500m, "EUR"), .5m, new(2026, 9, 10)),
            new("d", "c", "Proposal", new(500m, "EUR"), .5m, new(2026, 10, 1))];
        Assert.Equal(60m, CommercialMetrics.WeightedPipeline(records, September, "EUR"));
    }
    [Fact]
    public void RefusesMixedCurrenciesRatherThanAddingThem() =>
        Assert.Throws<ArgumentException>(() => CommercialMetrics.Revenue(
            [new("a", "c", new(2026, 9, 1), new(10m, "USD"))], September, "EUR"));
    [Fact]
    public void RefusesReversedReportingPeriod() =>
        Assert.Throws<ArgumentException>(() => new ReportingPeriod(new(2026, 10, 1), new(2026, 9, 1)));
}

