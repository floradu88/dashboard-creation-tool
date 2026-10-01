using System.Text.Json;
using CustomerDashboard.Application;
using CustomerDashboard.Domain;

namespace CustomerDashboard.Infrastructure;

// Immutable startup snapshot: reads never make upstream calls or mutate extraction times.
public sealed class JsonFixtureStore
{
    public static readonly IReadOnlyDictionary<string, string> Manifest = new Dictionary<string, string>
    {
        ["customers"] = "CustomerCatalog", ["salesforce"] = "Salesforce", ["alexis"] = "Alexis",
        ["jira"] = "Jira", ["confluence"] = "Confluence", ["slack"] = "Slack",
        ["customer-news"] = "CustomerNews", ["market-data"] = "MarketData"
    };
    private readonly Dictionary<string, JsonElement> snapshots = [];
    private readonly HashSet<string> customerIds;
    public int CustomerCount => customerIds.Count;
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JsonFixtureStore(string root)
    {
        foreach (var (file, source) in Manifest)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, file + ".json")));
                var envelope = document.RootElement;
                if (envelope.GetProperty("schemaVersion").GetInt32() != 1
                    || envelope.GetProperty("source").GetString() != source
                    || envelope.GetProperty("mode").GetString() != "Mock")
                    throw new InvalidDataException("Unsupported schema, source or mode.");
                var extracted = envelope.GetProperty("lastSuccessfulExtractionAtUtc").GetDateTimeOffset();
                var through = envelope.GetProperty("dataThroughUtc");
                if (through.ValueKind != JsonValueKind.Null && through.GetDateTimeOffset() > extracted)
                    throw new InvalidDataException("Coverage watermark exceeds extraction time.");
                if (envelope.GetProperty("data").ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException("data must be an object.");
                snapshots[source] = envelope.Clone();
            }
            catch (Exception error) when (error is JsonException or IOException or InvalidOperationException or KeyNotFoundException or FormatException)
            {
                throw new InvalidDataException($"Invalid fixture {file}.json: {error.Message}", error);
            }
        }
        var customers = Collection<Customer>("CustomerCatalog", "customers");
        customerIds = customers.Select(x => x.Id).ToHashSet();
        if (customerIds.Count != customers.Count || customers.Any(x => string.IsNullOrWhiteSpace(x.Id)
            || string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.ReportingCurrency)))
            throw new InvalidDataException("customers.json: customer IDs must be unique and customer names/currencies required.");
        foreach (var (source, snapshot) in snapshots) ValidateReferences(snapshot.GetProperty("data"), source);
        ValidateBundle(customers);
    }

    public IReadOnlyList<T> Collection<T>(string source, string name) =>
        snapshots[source].GetProperty("data").GetProperty(name).Deserialize<T[]>(JsonOptions)
        ?? throw new InvalidDataException($"{source}.{name}: expected an array.");

    public IReadOnlyList<SourceStatus> Statuses(TimeProvider clock, int defaultFreshnessMinutes, IReadOnlyDictionary<string, string> availability, IReadOnlyDictionary<string, int> freshnessMinutes) =>
        snapshots.Where(x => x.Key != "CustomerCatalog").Select(x =>
        {
            var time = x.Value.GetProperty("lastSuccessfulExtractionAtUtc").GetDateTimeOffset();
            var through = x.Value.GetProperty("dataThroughUtc");
            var minutes = freshnessMinutes.TryGetValue(x.Key, out var configured) ? configured : defaultFreshnessMinutes;
            var status = availability.TryGetValue(x.Key, out var configuredStatus) ? configuredStatus : "Available";
            return new SourceStatus(x.Key, "Mock", status, time, through.ValueKind == JsonValueKind.Null ? null : through.GetDateTimeOffset(),
                clock.GetUtcNow() - time > TimeSpan.FromMinutes(minutes) ? "Stale" : "Fresh");
        }).ToArray();

    private void ValidateReferences(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name == "customerId" && !customerIds.Contains(property.Value.GetString() ?? ""))
                    throw new InvalidDataException($"{path}.customerId: unknown customer.");
                ValidateReferences(property.Value, path + "." + property.Name);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var ids = new HashSet<string>();
            foreach (var child in element.EnumerateArray())
            {
                if (child.ValueKind == JsonValueKind.Object && child.TryGetProperty("id", out var id)
                    && (string.IsNullOrWhiteSpace(id.GetString()) || !ids.Add(id.GetString()!)))
                    throw new InvalidDataException($"{path}: missing or duplicate record ID.");
                ValidateReferences(child, path);
            }
        }
    }

    private void ValidateBundle(IReadOnlyList<Customer> customers)
    {
        foreach (var customer in customers)
        {
            if (customer.SourceMappings is null || string.IsNullOrWhiteSpace(customer.Domain) || customer.ReportingCurrency.Length != 3)
                throw new InvalidDataException($"customers.json: {customer.Id} is missing identity or currency.");
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(customer.ReportingTimeZone); }
            catch (TimeZoneNotFoundException) { throw new InvalidDataException($"customers.json: unknown timezone for {customer.Id}."); }
        }
        var byId = customers.ToDictionary(x => x.Id);
        var accounts = Collection<SalesforceAccount>("Salesforce", "accounts");
        var opportunities = Collection<Opportunity>("Salesforce", "opportunities");
        var requests = Collection<SalesforceRequest>("Salesforce", "requests");
        var features = Collection<FeatureRecord>("Jira", "features");
        var issues = Collection<WorkIssue>("Jira", "issues");
        var worklogs = Collection<Worklog>("Jira", "worklogs");
        var pages = Collection<DocumentPage>("Confluence", "pages");
        var channels = Collection<SlackChannel>("Slack", "channels");
        var messages = Collection<SlackMessage>("Slack", "messages");
        var articles = Collection<NewsArticle>("CustomerNews", "articles");
        var indicators = Collection<MarketIndicator>("MarketData", "indicators");
        var revenue = Collection<RevenueEntry>("Alexis", "revenueEntries");
        var usage = Collection<UsageRecord>("Alexis", "usageRecords");
        var objectives = Collection<SlaObjective>("Alexis", "slaObjectives");
        var events = Collection<SlaEvent>("Alexis", "slaEvents");
        var rates = Collection<LaborRate>("Alexis", "laborRates");
        var external = Collection<ExternalCost>("Alexis", "externalCosts");
        foreach (var account in accounts)
        {
            var customer = byId[account.CustomerId];
            if (customer.SourceMappings.SalesforceAccountId != account.Id) throw new InvalidDataException($"Salesforce account {account.Id} does not match the customer mapping.");
            var team = account.Team.Select(x => x.Id).ToHashSet();
            if (!team.Contains(account.Owner.Id) || account.Actions.Any(x => !team.Contains(x.OwnerId)) || account.Risks.Any(x => !team.Contains(x.OwnerId)))
                throw new InvalidDataException($"Salesforce account {account.Id} has an owner outside the team.");
            if (account.Renewal is not null && account.Renewal.Value.Currency != customer.ReportingCurrency)
                throw new InvalidDataException($"Salesforce account {account.Id} renewal currency does not match.");
        }
        foreach (var opportunity in opportunities)
        {
            if (opportunity.Amount.Currency != byId[opportunity.CustomerId].ReportingCurrency || opportunity.Probability is < 0 or > 1
                || opportunity.Stage is not ("Qualification" or "Proposal" or "Negotiation" or "Won" or "Lost")
                || opportunity.Kind is not ("NewBusiness" or "Upsell" or "Renewal"))
                throw new InvalidDataException($"Salesforce opportunity {opportunity.Id} is invalid.");
        }
        var opportunityIds = opportunities.Select(x => x.Id).ToHashSet();
        var issueIds = issues.Select(x => x.Id).ToHashSet();
        var pageIds = pages.Select(x => x.Id).ToHashSet();
        foreach (var request in requests)
        {
            if (request.Type is not ("RFP" or "RFI" or "FeatureRequest" or "Support") || request.Status is not ("Open" or "InProgress" or "Resolved" or "Cancelled"))
                throw new InvalidDataException($"Salesforce request {request.Id} has an unknown type or status.");
            if (request.OpportunityId is not null && !opportunityIds.Contains(request.OpportunityId)) throw new InvalidDataException($"Request {request.Id} opportunity is missing.");
            if (request.JiraIssueIds.Any(x => !issueIds.Contains(x)) || request.ConfluencePageIds.Any(x => !pageIds.Contains(x)))
                throw new InvalidDataException($"Request {request.Id} has a missing linked record.");
        }
        foreach (var entry in revenue)
        {
            if (entry.Amount.Currency != byId[entry.CustomerId].ReportingCurrency || entry.Category is not ("Recurring" or "OneOff" or "Adjustment"))
                throw new InvalidDataException($"Revenue {entry.Id} is invalid.");
            if (entry.Category != "Adjustment" && entry.Amount.Amount < 0) throw new InvalidDataException($"Revenue {entry.Id} cannot be negative.");
        }
        var featureKeys = features.Select(x => x.CustomerId + ":" + x.Id).ToHashSet();
        foreach (var record in usage)
        {
            if (record.Quantity < 0 || record.Entitlement < 0 || record.PeriodEndExclusive <= record.PeriodStart || !featureKeys.Contains(record.CustomerId + ":" + record.FeatureId))
                throw new InvalidDataException($"Usage {record.Id} is invalid.");
        }
        var objectiveIds = objectives.Select(x => x.Id).ToHashSet();
        foreach (var objective in objectives)
            if (objective.Kind is not ("ResponseTime" or "ResolutionTime") || objective.ThresholdMinutes < 0 || objective.TargetCompliancePercent is < 0 or > 100)
                throw new InvalidDataException($"SLA objective {objective.Id} is invalid.");
        foreach (var slaEvent in events)
        {
            if (!objectiveIds.Contains(slaEvent.ObjectiveId) || slaEvent.ActualMinutes < 0) throw new InvalidDataException($"SLA event {slaEvent.Id} is invalid.");
            if (slaEvent.Eligible != string.IsNullOrWhiteSpace(slaEvent.ExclusionReason)) throw new InvalidDataException($"SLA event {slaEvent.Id} exclusion reason does not match eligibility.");
        }
        foreach (var worker in rates.GroupBy(x => x.WorkerId))
        {
            var ordered = worker.OrderBy(x => x.ValidFrom).ToArray();
            for (var i = 1; i < ordered.Length; i++)
                if (ordered[i].ValidFrom < ordered[i - 1].ValidToExclusive) throw new InvalidDataException($"Labor rates overlap for {worker.Key}.");
        }
        foreach (var issue in issues)
        {
            if (issue.FeatureId is not null && !featureKeys.Contains(issue.CustomerId + ":" + issue.FeatureId)) throw new InvalidDataException($"Issue {issue.Id} feature is missing.");
            if (issue.ResolvedAtUtc is not null && issue.ResolvedAtUtc < issue.CreatedAtUtc) throw new InvalidDataException($"Issue {issue.Id} was resolved before it was created.");
        }
        foreach (var work in worklogs)
        {
            if (!issueIds.Contains(work.IssueId) || work.Hours < 0 || work.Allocations.Length == 0 || work.Allocations.Any(x => x.Fraction is <= 0 or > 1) || Math.Abs(work.Allocations.Sum(x => x.Fraction) - 1m) > 0.0000001m)
                throw new InvalidDataException($"Worklog {work.Id} allocations are invalid.");
            if (work.Allocations.Any(x => !featureKeys.Contains(x.CustomerId + ":" + x.FeatureId))) throw new InvalidDataException($"Worklog {work.Id} feature is missing.");
        }
        foreach (var page in pages)
            if (!byId[page.CustomerId].SourceMappings.ConfluenceSpaceKeys.Contains(page.SpaceKey) || page.Version < 1)
                throw new InvalidDataException($"Confluence page {page.Id} is outside the customer space.");
        foreach (var channel in channels)
            if (!byId[channel.CustomerId].SourceMappings.SlackChannelIds.Contains(channel.Id)) throw new InvalidDataException($"Slack channel {channel.Id} is not mapped.");
        var messageIds = messages.Select(x => x.Id).ToHashSet();
        foreach (var message in messages)
        {
            if (channels.All(x => x.Id != message.ChannelId || x.CustomerId != message.CustomerId)) throw new InvalidDataException($"Slack message {message.Id} channel is missing.");
            if (message.ThreadRootId is not null && !messageIds.Contains(message.ThreadRootId)) throw new InvalidDataException($"Slack message {message.Id} thread is missing.");
        }
        foreach (var article in articles)
        {
            var customer = byId[article.CustomerId];
            if (!article.IsSimulated || article.Match.Domain != customer.Domain || article.Match.Method is not ("ApprovedDomain" or "ReviewedAlias") || article.Match.Method == "ReviewedAlias" && !article.Match.Reviewed)
                throw new InvalidDataException($"News article {article.Id} is not an approved simulated match.");
        }
        foreach (var indicator in indicators)
        {
            if (!indicator.IsSimulated || indicator.Unit is not ("Percent" or "Ratio" or "Count" or "Index" or "Currency")) throw new InvalidDataException($"Market indicator {indicator.Id} is invalid.");
            if (indicator.Unit == "Currency" != (indicator.Currency is not null)) throw new InvalidDataException($"Market indicator {indicator.Id} currency does not match its unit.");
            if (indicator.Value is < 0 && indicator.Unit is not "Percent") throw new InvalidDataException($"Market indicator {indicator.Id} value is negative.");
        }
        foreach (var cost in external)
            if (cost.Amount.Amount < 0 || cost.Amount.Currency != byId[cost.CustomerId].ReportingCurrency || !featureKeys.Contains(cost.CustomerId + ":" + cost.FeatureId))
                throw new InvalidDataException($"External cost {cost.Id} is invalid.");
        var probe = new ReportingPeriod(new DateOnly(2025, 10, 1), new DateOnly(2026, 10, 1));
        foreach (var customer in customers)
            _ = CommercialMetrics.Revenue(revenue.Where(x => x.CustomerId == customer.Id), probe, customer.ReportingCurrency);
    }
}

