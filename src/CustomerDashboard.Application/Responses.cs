using CustomerDashboard.Domain;

namespace CustomerDashboard.Application;

public sealed record SourceStatus(string Source, string Mode, string Status, DateTimeOffset? LastSuccessfulExtractionAtUtc, DateTimeOffset? DataThroughUtc, string Freshness);
public sealed record ResponseMeta(int SchemaVersion, DateTimeOffset GeneratedAtUtc, string? ReportingCurrency, bool IsPartial, IReadOnlyList<SourceStatus> Sources, IReadOnlyList<string> Warnings);
public sealed record DashboardResponse<T>(string? CustomerId, T Data, ResponseMeta Meta);
public sealed record CustomerSummary(string Id, string Name, string Industry, string ReportingCurrency);
public sealed record CustomerListData(IReadOnlyList<CustomerSummary> Items, int Total, int Page, int PageSize);
public sealed record MonthValue(string Month, decimal Revenue, decimal WeightedPipeline);
public sealed record AttentionItem(string Code, string Message);
public sealed record OverviewData(Measured Revenue, Measured WeightedPipeline, Measured ProjectedRevenue, Measured ActiveUsers, Measured SlaCompliance, Measured OpenRequests, Measured DeliverySpend, Measured DeliveryCoverage, IReadOnlyList<MonthValue> Trend, IReadOnlyList<AttentionItem> Attention);
public sealed record StageTotal(string Stage, int Count, decimal Amount, decimal Weighted);
public sealed record OpportunityRow(string Id, string Name, string Kind, string Stage, decimal Amount, string Currency, decimal Probability, decimal Weighted, DateOnly ExpectedCloseDate, string SourceUrl);
public sealed record CommercialData(Measured Revenue, Measured WeightedPipeline, Measured ProjectedRevenue, IReadOnlyList<StageTotal> Stages, IReadOnlyList<MonthValue> Trend, IReadOnlyList<OpportunityRow> Opportunities);
public sealed record UsageData(Measured ActiveUsers, Measured Adoption, IReadOnlyList<UsagePoint> Points);
public sealed record SlaData(Measured Compliance, IReadOnlyList<SlaObjectiveResult> Objectives);
public sealed record StatusCount(string Status, int Count);
public sealed record WorkRow(string Id, string Key, string? FeatureId, string Type, string Title, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, DateTimeOffset? ResolvedAtUtc, string SourceUrl);
public sealed record WorkData(Measured DeliverySpend, Measured DeliveryCoverage, IReadOnlyList<StatusCount> Statuses, IReadOnlyList<FeatureCostLine> Costs, IReadOnlyList<WorkRow> Items, int Total, int Page, int PageSize);
public sealed record LinkedRecord(string Id, string Title, string Url);
public sealed record RequestRow(string Id, string CanonicalRequestId, string Type, string Title, string Status, DateTimeOffset ReceivedAtUtc, DateTimeOffset? DueAtUtc, DateTimeOffset? ResolvedAtUtc, string? OpportunityId, IReadOnlyList<LinkedRecord> Documents, IReadOnlyList<LinkedRecord> WorkItems, int RelatedMessages, string SourceUrl);
public sealed record RequestData(IReadOnlyList<RequestRow> Items, int Total, int Page, int PageSize);
public sealed record NewsRow(string Id, string Headline, string Summary, string Publisher, string SourceUrl, DateTimeOffset PublishedAtUtc, DateOnly? EventDate, IReadOnlyList<string> Topics, string MatchMethod, string Relevance, bool IsSimulated);
public sealed record NewsData(IReadOnlyList<NewsRow> Items, int Total, int Page, int PageSize, int DuplicatesRemoved);
public sealed record MarketRow(string Id, string Name, string Kind, string Provider, string Sector, string Geography, string MetricId, int DefinitionVersion, DateOnly PeriodStart, DateOnly PeriodEndExclusive, DateTimeOffset ObservedAtUtc, Measured Benchmark, string Methodology, int? SampleSize, string SourceUrl, bool IsSimulated, Measured CustomerComparison);
public sealed record MarketData(IReadOnlyList<MarketRow> Indicators);
public sealed record StakeholderView(string Id, string DisplayName, string Role);
public sealed record TeamView(string Id, string DisplayName, string Role);
public sealed record ActionView(string Id, string Title, string OwnerId, DateOnly DueDate, string Status, bool Overdue);
public sealed record RiskView(string Id, string Description, string Severity, string OwnerId, string Status);
public sealed record RenewalView(DateOnly Date, decimal Amount, string Currency, string Status);
public sealed record AccountData(string AccountId, string OwnerName, IReadOnlyList<TeamView> Team, IReadOnlyList<StakeholderView> Stakeholders, RenewalView? Renewal, Measured RenewalHorizon, DateOnly? NextQbrDate, DateTimeOffset? LastMeaningfulInteractionAtUtc, string? SuccessPlanUrl, IReadOnlyList<ActionView> Actions, IReadOnlyList<RiskView> Risks, DateTimeOffset? AsOfUtc);
public sealed record MetricRow(string Id, int Version, string Name, string Unit, string Formula, string Direction, string MissingBehavior, IReadOnlyList<string> Sources, Measured Actual, decimal? Target, Measured Comparison);
public sealed record MetricsData(DateOnly ComparisonFrom, DateOnly ComparisonTo, IReadOnlyList<MetricRow> Metrics);
public sealed record SourceListData(IReadOnlyList<SourceStatus> Sources);

public static class PageSlice
{
    public static (T[] Items, int Total) Take<T>(IEnumerable<T> source, int page, int pageSize)
    {
        var all = source.ToArray();
        return (all.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), all.Length);
    }
}
