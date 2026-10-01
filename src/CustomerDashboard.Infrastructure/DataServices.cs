using CustomerDashboard.Application;
using CustomerDashboard.Domain;

namespace CustomerDashboard.Infrastructure;

public sealed class CustomerDataService(JsonFixtureStore store) : ICustomerReader
{
    public IReadOnlyList<Customer> Read() => store.Collection<Customer>("CustomerCatalog", "customers");
}

public sealed class SalesforceDataService(JsonFixtureStore store) : ISalesforceReader
{
    public IReadOnlyList<SalesforceAccount> Accounts() => store.Collection<SalesforceAccount>("Salesforce", "accounts");
    public IReadOnlyList<Opportunity> Opportunities(string customerId) => store.Collection<Opportunity>("Salesforce", "opportunities").Where(x => x.CustomerId == customerId).ToArray();
    public IReadOnlyList<SalesforceRequest> Requests(string customerId) => store.Collection<SalesforceRequest>("Salesforce", "requests").Where(x => x.CustomerId == customerId).ToArray();
}

public sealed class AlexisDataService(JsonFixtureStore store) : IAlexisReader
{
    public IReadOnlyList<RevenueEntry> Revenue(string customerId) => store.Collection<RevenueEntry>("Alexis", "revenueEntries").Where(x => x.CustomerId == customerId).ToArray();
    public IReadOnlyList<UsageRecord> Usage(string customerId) => store.Collection<UsageRecord>("Alexis", "usageRecords").Where(x => x.CustomerId == customerId).ToArray();
    public IReadOnlyList<SlaObjective> SlaObjectives(string customerId) => store.Collection<SlaObjective>("Alexis", "slaObjectives").Where(x => x.CustomerId == customerId).ToArray();
    public IReadOnlyList<SlaEvent> SlaEvents(string customerId) => store.Collection<SlaEvent>("Alexis", "slaEvents").Where(x => x.CustomerId == customerId).ToArray();
    public IReadOnlyList<LaborRate> LaborRates() => store.Collection<LaborRate>("Alexis", "laborRates");
    public IReadOnlyList<ExternalCost> ExternalCosts(string customerId) => store.Collection<ExternalCost>("Alexis", "externalCosts").Where(x => x.CustomerId == customerId).ToArray();
}

public sealed class JiraDataService(JsonFixtureStore store) : IJiraReader
{
    public IReadOnlyList<FeatureRecord> Features(string customerId) => store.Collection<FeatureRecord>("Jira", "features").Where(x => x.CustomerId == customerId).ToArray();
    public IReadOnlyList<WorkIssue> Issues(string customerId) => store.Collection<WorkIssue>("Jira", "issues").Where(x => x.CustomerId == customerId).ToArray();
    public IReadOnlyList<Worklog> Worklogs() => store.Collection<Worklog>("Jira", "worklogs");
}

public sealed class ConfluenceDataService(JsonFixtureStore store) : IConfluenceReader
{
    public IReadOnlyList<DocumentPage> Pages(string customerId) => store.Collection<DocumentPage>("Confluence", "pages").Where(x => x.CustomerId == customerId).ToArray();
}

public sealed class SlackDataService(JsonFixtureStore store) : ISlackReader
{
    public IReadOnlyList<SlackMessage> Messages(string customerId) => store.Collection<SlackMessage>("Slack", "messages").Where(x => x.CustomerId == customerId).ToArray();
}

public sealed class NewsDataService(JsonFixtureStore store) : INewsReader
{
    public IReadOnlyList<NewsArticle> Articles(string customerId) => store.Collection<NewsArticle>("CustomerNews", "articles").Where(x => x.CustomerId == customerId).ToArray();
}

public sealed class MarketDataService(JsonFixtureStore store) : IMarketReader
{
    public IReadOnlyList<MarketIndicator> Indicators(string customerId) => store.Collection<MarketIndicator>("MarketData", "indicators").Where(x => x.CustomerId == customerId).ToArray();
}

public sealed class SourceStatusService(JsonFixtureStore store, TimeProvider clock, int defaultFreshnessMinutes, IReadOnlyDictionary<string, string> availability, IReadOnlyDictionary<string, int> freshnessMinutes) : ISourceStatusReader
{
    public IReadOnlyList<SourceStatus> Read(string customerId) => store.Statuses(clock, defaultFreshnessMinutes, availability, freshnessMinutes);
}
