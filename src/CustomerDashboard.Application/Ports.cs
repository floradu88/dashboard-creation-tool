using CustomerDashboard.Domain;

namespace CustomerDashboard.Application;

public interface ICustomerReader { IReadOnlyList<Customer> Read(); }
public interface ISalesforceReader
{
    IReadOnlyList<SalesforceAccount> Accounts();
    IReadOnlyList<Opportunity> Opportunities(string customerId);
    IReadOnlyList<SalesforceRequest> Requests(string customerId);
}
public interface IAlexisReader
{
    IReadOnlyList<RevenueEntry> Revenue(string customerId);
    IReadOnlyList<UsageRecord> Usage(string customerId);
    IReadOnlyList<SlaObjective> SlaObjectives(string customerId);
    IReadOnlyList<SlaEvent> SlaEvents(string customerId);
    IReadOnlyList<LaborRate> LaborRates();
    IReadOnlyList<ExternalCost> ExternalCosts(string customerId);
}
public interface IJiraReader
{
    IReadOnlyList<FeatureRecord> Features(string customerId);
    IReadOnlyList<WorkIssue> Issues(string customerId);
    IReadOnlyList<Worklog> Worklogs();
}
public interface IConfluenceReader { IReadOnlyList<DocumentPage> Pages(string customerId); }
public interface ISlackReader { IReadOnlyList<SlackMessage> Messages(string customerId); }
public interface INewsReader { IReadOnlyList<NewsArticle> Articles(string customerId); }
public interface IMarketReader { IReadOnlyList<MarketIndicator> Indicators(string customerId); }
public interface ISourceStatusReader { IReadOnlyList<SourceStatus> Read(string customerId); }

public static class SourceNames
{
    public const string Salesforce = "Salesforce";
    public const string Alexis = "Alexis";
    public const string Jira = "Jira";
    public const string Confluence = "Confluence";
    public const string Slack = "Slack";
    public const string CustomerNews = "CustomerNews";
    public const string MarketData = "MarketData";
}
