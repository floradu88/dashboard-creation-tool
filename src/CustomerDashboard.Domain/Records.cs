namespace CustomerDashboard.Domain;

public sealed record SourceMappings(string? SalesforceAccountId, string? AlexisCustomerKey, string? JiraCustomerKey, string[] SlackChannelIds, string[] ConfluenceSpaceKeys);
public sealed record Customer(string Id, string Name, string Domain, string[] Aliases, string Industry, string CountryCode, string ReportingCurrency, string ReportingTimeZone, SourceMappings SourceMappings);
public sealed record PersonRef(string Id, string DisplayName);
public sealed record TeamMember(string Id, string DisplayName, string Role);
public sealed record Stakeholder(string Id, string DisplayName, string Role, string? Email);
public sealed record Renewal(DateOnly Date, Money Value, string Status);
public sealed record AccountAction(string Id, string Title, string OwnerId, DateOnly DueDate, string Status);
public sealed record AccountRisk(string Id, string Description, string Severity, string OwnerId, string Status);
public sealed record MetricTarget(string Id, string MetricId, int DefinitionVersion, DateOnly PeriodStart, DateOnly PeriodEndExclusive, decimal Value, string Unit, string OwnerId);
public sealed record SalesforceAccount(string Id, string CustomerId, PersonRef Owner, TeamMember[] Team, Stakeholder[] Stakeholders, Renewal? Renewal, DateOnly? NextQbrDate, DateTimeOffset? LastMeaningfulInteractionAtUtc, string? SuccessPlanUrl, AccountAction[] Actions, AccountRisk[] Risks, MetricTarget[] Targets);
public sealed record SalesforceRequest(string Id, string CanonicalRequestId, string CustomerId, string Type, string Title, string Status, DateTimeOffset ReceivedAtUtc, DateTimeOffset? DueAtUtc, DateTimeOffset? ResolvedAtUtc, string OwnerId, string? OpportunityId, string[] JiraIssueIds, string[] ConfluencePageIds, string SourceUrl);
public sealed record UsageRecord(string Id, string CustomerId, DateOnly PeriodStart, DateOnly PeriodEndExclusive, string FeatureId, string Unit, decimal Quantity, decimal? Entitlement, string[] ActiveUserIds);
public sealed record SlaObjective(string Id, string CustomerId, string Name, string Kind, int ThresholdMinutes, decimal TargetCompliancePercent);
public sealed record SlaEvent(string Id, string CustomerId, string ObjectiveId, DateTimeOffset OccurredAtUtc, int ActualMinutes, bool Eligible, string? ExclusionReason);
public sealed record LaborRate(string Id, string WorkerId, DateOnly ValidFrom, DateOnly ValidToExclusive, Money HourlyRate);
public sealed record ExternalCost(string Id, string CustomerId, string FeatureId, DateOnly IncurredDate, Money Amount, string Description);
public sealed record FeatureRecord(string Id, string CustomerId, string Name);
public sealed record WorkAllocation(string CustomerId, string FeatureId, decimal Fraction);
public sealed record Worklog(string Id, string IssueId, string WorkerId, DateTimeOffset StartedAtUtc, decimal Hours, bool Approved, WorkAllocation[] Allocations);
public sealed record WorkIssue(string Id, string Key, string CustomerId, string? FeatureId, string? CanonicalRequestId, string Type, string Title, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, DateTimeOffset? ResolvedAtUtc, string SourceUrl);
public sealed record DocumentPage(string Id, string CustomerId, string SpaceKey, string Title, string Kind, int Version, DateTimeOffset UpdatedAtUtc, string[] CanonicalRequestIds, string SourceUrl, string Summary);
public sealed record SlackChannel(string Id, string CustomerId, string Name);
public sealed record SlackMessage(string Id, string CustomerId, string ChannelId, DateTimeOffset SentAtUtc, string? ThreadRootId, string AuthorDisplayName, string Text, string[] CanonicalRequestIds, string SourceUrl);
public sealed record NewsMatch(string Domain, string Method, bool Reviewed);
public sealed record NewsArticle(string Id, string CustomerId, string Headline, string Summary, string Publisher, string SourceUrl, DateTimeOffset PublishedAtUtc, DateOnly? EventDate, string[] Topics, NewsMatch Match, string Relevance, bool IsSimulated);
public sealed record MarketIndicator(string Id, string CustomerId, string Name, string Kind, string Provider, string Sector, string Geography, string MetricId, int DefinitionVersion, DateOnly PeriodStart, DateOnly PeriodEndExclusive, DateTimeOffset ObservedAtUtc, decimal? Value, string Unit, string? Currency, string Methodology, int? SampleSize, string SourceUrl, bool IsSimulated);

public static class TimeZones
{
    public static DateOnly LocalDate(DateTimeOffset instant, string timeZoneId) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).DateTime);
}
