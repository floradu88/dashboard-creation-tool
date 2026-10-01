using CustomerDashboard.Domain;

namespace CustomerDashboard.Application;

public sealed class QuerySupport(ICustomerReader customers, ISourceStatusReader sources, TimeProvider clock)
{
    public Customer? Find(string customerId) => customers.Read().SingleOrDefault(x => x.Id == customerId);
    public DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
    public bool Available(string customerId, string source) => sources.Read(customerId).Single(x => x.Source == source).Status == "Available";

    public DashboardResponse<T> Respond<T>(Customer customer, T data, IReadOnlyList<string> sourceNames, bool partial = false, IEnumerable<string>? warnings = null)
    {
        var selected = sources.Read(customer.Id).Where(x => sourceNames.Contains(x.Source)).ToArray();
        var messages = new List<string>(warnings ?? []);
        if (selected.Any(x => x.Status != "Available")) partial = true;
        foreach (var source in selected.Where(x => x.Status != "Available"))
            messages.Add($"{source.Source} is unavailable. Its records are omitted.");
        foreach (var source in selected.Where(x => x.Status == "Available" && x.Freshness == "Stale"))
            messages.Add($"{source.Source} is stale. Values are from the last successful extraction.");
        return new(customer.Id, data, new(1, clock.GetUtcNow(), customer.ReportingCurrency, partial, selected, messages));
    }

    public static IReadOnlyList<MonthValue> Trend(Customer customer, ReportingPeriod period, IReadOnlyList<RevenueEntry> revenue, IReadOnlyList<Opportunity> opportunities)
    {
        var trend = new List<MonthValue>();
        for (var month = new DateOnly(period.From.Year, period.From.Month, 1); month < period.To; month = month.AddMonths(1))
        {
            var start = month < period.From ? period.From : month;
            var end = month.AddMonths(1) > period.To ? period.To : month.AddMonths(1);
            if (end <= start) continue;
            var slice = new ReportingPeriod(start, end);
            trend.Add(new(month.ToString("yyyy-MM"), CommercialMetrics.Revenue(revenue, slice, customer.ReportingCurrency),
                CommercialMetrics.WeightedPipeline(opportunities, slice, customer.ReportingCurrency)));
        }
        return trend;
    }

    public static Measured Money(decimal amount, string currency, bool available) =>
        available ? Measured.Number(amount, currency) : Measured.Unavailable(currency);
}

public sealed class GetCustomersHandler(ICustomerReader customers, TimeProvider clock)
{
    public DashboardResponse<CustomerListData> Handle(string? search, int page, int pageSize)
    {
        var filtered = customers.Read().Where(x => string.IsNullOrWhiteSpace(search) || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name).Select(x => new CustomerSummary(x.Id, x.Name, x.Industry, x.ReportingCurrency)).ToArray();
        var slice = PageSlice.Take(filtered, page, pageSize);
        var data = new CustomerListData(slice.Items, slice.Total, page, pageSize);
        return new(null, data, new(1, clock.GetUtcNow(), null, false, [], []));
    }
}

public sealed class GetOverviewHandler(QuerySupport support, ISalesforceReader salesforce, IAlexisReader alexis, IJiraReader jira)
{
    public DashboardResponse<OverviewData>? Handle(string customerId, ReportingPeriod period)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var alexisOn = support.Available(customerId, SourceNames.Alexis);
        var salesforceOn = support.Available(customerId, SourceNames.Salesforce);
        var jiraOn = support.Available(customerId, SourceNames.Jira);
        var revenueRows = alexisOn ? alexis.Revenue(customerId) : [];
        var opportunities = salesforceOn ? salesforce.Opportunities(customerId) : [];
        var requests = salesforceOn ? salesforce.Requests(customerId) : [];
        var usage = alexisOn ? DashboardMetrics.Usage(alexis.Usage(customerId), period) : null;
        var objectives = alexisOn ? alexis.SlaObjectives(customerId) : [];
        var events = alexisOn ? alexis.SlaEvents(customerId).Where(x => period.Contains(DateOnly.FromDateTime(x.OccurredAtUtc.UtcDateTime))).ToArray() : [];
        var thresholds = objectives.ToDictionary(x => x.Id, x => x.ThresholdMinutes);
        var sla = alexisOn ? DashboardMetrics.SlaCompliance(events, thresholds) : Measured.Unavailable("Percent");
        var cost = jiraOn && alexisOn
            ? DashboardMetrics.DeliveryCost(customerId, customer.ReportingCurrency, customer.ReportingTimeZone, period, jira.Worklogs(), jira.Features(customerId), alexis.LaborRates(), alexis.ExternalCosts(customerId))
            : null;
        var today = support.Today();
        var account = salesforceOn ? salesforce.Accounts().SingleOrDefault(x => x.CustomerId == customerId) : null;
        var attention = new List<AttentionItem>();
        if (alexisOn)
            attention.AddRange(DashboardMetrics.SlaResults(objectives, alexis.SlaEvents(customerId), period).SelectMany(x => x.BreachEventIds.Select(id => new AttentionItem("SlaBreach", $"{x.Name} breach {id}"))));
        if (account is not null)
            attention.AddRange(account.Actions.Where(x => x.Status is "Open" or "InProgress" && x.DueDate < today).Select(x => new AttentionItem("OverdueAction", x.Title)));
        var overdue = salesforceOn ? DashboardMetrics.OverdueRequests(requests, today) : 0;
        if (overdue > 0) attention.Add(new("OverdueRequest", $"{overdue} overdue request(s)"));
        if (cost?.UncoveredHours > 0) attention.Add(new("MissingRate", "Some approved delivery hours have no labor rate."));
        var warnings = new List<string> { DashboardMetrics.ProjectedRevenueReason };
        if (usage?.Partial == true) warnings.Add("Some usage buckets extend outside the reporting period and were not prorated.");
        if (cost?.UncoveredHours > 0) warnings.Add("Delivery cost excludes approved hours that have no rate.");
        return support.Respond(customer, new OverviewData(
            QuerySupport.Money(CommercialMetrics.Revenue(revenueRows, period, customer.ReportingCurrency), customer.ReportingCurrency, alexisOn),
            QuerySupport.Money(CommercialMetrics.WeightedPipeline(opportunities, period, customer.ReportingCurrency), customer.ReportingCurrency, salesforceOn),
            Measured.NotApplicable(customer.ReportingCurrency),
            usage?.ActiveUsers ?? Measured.Unavailable("Count"),
            sla,
            salesforceOn ? Measured.Number(requests.Count(x => DashboardMetrics.OpenRequestStatuses.Contains(x.Status)), "Count") : Measured.Unavailable("Count"),
            cost?.Total ?? Measured.Unavailable(customer.ReportingCurrency),
            cost?.Coverage ?? Measured.Unavailable("Percent"),
            QuerySupport.Trend(customer, period, revenueRows, opportunities),
            attention), [SourceNames.Alexis, SourceNames.Salesforce, SourceNames.Jira], usage?.Partial == true || cost?.UncoveredHours > 0, warnings);
    }
}

public sealed class GetCommercialHandler(QuerySupport support, ISalesforceReader salesforce, IAlexisReader alexis)
{
    public DashboardResponse<CommercialData>? Handle(string customerId, ReportingPeriod period)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var salesforceOn = support.Available(customerId, SourceNames.Salesforce);
        var alexisOn = support.Available(customerId, SourceNames.Alexis);
        var opportunities = salesforceOn ? salesforce.Opportunities(customerId).Where(x => period.Contains(x.ExpectedCloseDate)).ToArray() : [];
        var revenue = alexisOn ? alexis.Revenue(customerId) : [];
        var stages = opportunities.GroupBy(x => x.Stage).Select(x => new StageTotal(x.Key, x.Count(), x.Sum(row => row.Amount.Amount),
            x.Where(row => row.Stage is not ("Won" or "Lost")).Sum(row => row.Amount.Amount * row.Probability))).ToArray();
        var rows = opportunities.Select(x => new OpportunityRow(x.Id, x.Name, x.Kind, x.Stage, x.Amount.Amount, x.Amount.Currency, x.Probability,
            x.Stage is "Won" or "Lost" ? 0 : x.Amount.Amount * x.Probability, x.ExpectedCloseDate, x.SourceUrl)).ToArray();
        return support.Respond(customer, new CommercialData(
            QuerySupport.Money(CommercialMetrics.Revenue(revenue, period, customer.ReportingCurrency), customer.ReportingCurrency, alexisOn),
            QuerySupport.Money(CommercialMetrics.WeightedPipeline(salesforceOn ? salesforce.Opportunities(customerId) : [], period, customer.ReportingCurrency), customer.ReportingCurrency, salesforceOn),
            Measured.NotApplicable(customer.ReportingCurrency), stages, QuerySupport.Trend(customer, period, revenue, salesforceOn ? salesforce.Opportunities(customerId) : []), rows),
            [SourceNames.Salesforce, SourceNames.Alexis], warnings: [DashboardMetrics.ProjectedRevenueReason]);
    }
}

public sealed class GetUsageHandler(QuerySupport support, IAlexisReader alexis)
{
    public DashboardResponse<UsageData>? Handle(string customerId, ReportingPeriod period)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var available = support.Available(customerId, SourceNames.Alexis);
        var usage = available ? DashboardMetrics.Usage(alexis.Usage(customerId), period) : new UsageComputation(Measured.Unavailable("Count"), false, [], Measured.Unavailable("Percent"));
        var warnings = usage.Partial ? new[] { "Some usage buckets extend outside the reporting period and were not prorated." } : [];
        return support.Respond(customer, new UsageData(usage.ActiveUsers, usage.Adoption, usage.Points), [SourceNames.Alexis], usage.Partial, warnings);
    }
}

public sealed class GetSlaHandler(QuerySupport support, IAlexisReader alexis)
{
    public DashboardResponse<SlaData>? Handle(string customerId, ReportingPeriod period)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var available = support.Available(customerId, SourceNames.Alexis);
        if (!available) return support.Respond(customer, new SlaData(Measured.Unavailable("Percent"), []), [SourceNames.Alexis]);
        var objectives = alexis.SlaObjectives(customerId);
        var results = DashboardMetrics.SlaResults(objectives, alexis.SlaEvents(customerId), period);
        var thresholds = objectives.ToDictionary(x => x.Id, x => x.ThresholdMinutes);
        var events = alexis.SlaEvents(customerId).Where(x => period.Contains(DateOnly.FromDateTime(x.OccurredAtUtc.UtcDateTime)));
        return support.Respond(customer, new SlaData(DashboardMetrics.SlaCompliance(events, thresholds), results), [SourceNames.Alexis]);
    }
}

public sealed class GetWorkHandler(QuerySupport support, IJiraReader jira, IAlexisReader alexis)
{
    public static readonly string[] Sorts = ["updatedDesc", "keyAsc"];
    public DashboardResponse<WorkData>? Handle(string customerId, ReportingPeriod period, int page, int pageSize, string sort)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var jiraOn = support.Available(customerId, SourceNames.Jira);
        var alexisOn = support.Available(customerId, SourceNames.Alexis);
        var issues = jiraOn ? jira.Issues(customerId).Where(x => period.Contains(DateOnly.FromDateTime(x.UpdatedAtUtc.UtcDateTime)) || DashboardMetrics.OpenWorkStatuses.Contains(x.Status) && DateOnly.FromDateTime(x.CreatedAtUtc.UtcDateTime) < period.To).ToArray() : [];
        issues = sort == "keyAsc" ? issues.OrderBy(x => x.Key).ToArray() : issues.OrderByDescending(x => x.UpdatedAtUtc).ToArray();
        var slice = PageSlice.Take(issues, page, pageSize);
        var cost = jiraOn && alexisOn
            ? DashboardMetrics.DeliveryCost(customerId, customer.ReportingCurrency, customer.ReportingTimeZone, period, jira.Worklogs(), jira.Features(customerId), alexis.LaborRates(), alexis.ExternalCosts(customerId))
            : null;
        var rows = slice.Items.Select(x => new WorkRow(x.Id, x.Key, x.FeatureId, x.Type, x.Title, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, x.ResolvedAtUtc, x.SourceUrl)).ToArray();
        var statuses = issues.GroupBy(x => x.Status).Select(x => new StatusCount(x.Key, x.Count())).ToArray();
        var warnings = cost?.UncoveredHours > 0 ? new[] { "Delivery cost excludes approved hours that have no rate." } : [];
        return support.Respond(customer, new WorkData(cost?.Total ?? Measured.Unavailable(customer.ReportingCurrency), cost?.Coverage ?? Measured.Unavailable("Percent"), statuses, cost?.Lines ?? [], rows, slice.Total, page, pageSize),
            [SourceNames.Jira, SourceNames.Alexis], cost?.UncoveredHours > 0, warnings);
    }
}

public sealed class GetRequestsHandler(QuerySupport support, ISalesforceReader salesforce, IJiraReader jira, IConfluenceReader confluence, ISlackReader slack)
{
    public static readonly string[] Sorts = ["receivedDesc", "titleAsc"];
    public DashboardResponse<RequestData>? Handle(string customerId, ReportingPeriod period, int page, int pageSize, string sort)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var salesforceOn = support.Available(customerId, SourceNames.Salesforce);
        var requests = salesforceOn ? salesforce.Requests(customerId).Where(x => period.Contains(DateOnly.FromDateTime(x.ReceivedAtUtc.UtcDateTime))).ToArray() : [];
        requests = sort == "titleAsc" ? requests.OrderBy(x => x.Title).ToArray() : requests.OrderByDescending(x => x.ReceivedAtUtc).ToArray();
        var seen = new HashSet<string>();
        var unique = requests.Where(x => seen.Add(x.CanonicalRequestId)).ToArray();
        var slice = PageSlice.Take(unique, page, pageSize);
        var jiraOn = support.Available(customerId, SourceNames.Jira);
        var confluenceOn = support.Available(customerId, SourceNames.Confluence);
        var slackOn = support.Available(customerId, SourceNames.Slack);
        var issues = jiraOn ? jira.Issues(customerId) : [];
        var pages = confluenceOn ? confluence.Pages(customerId) : [];
        var messages = slackOn ? slack.Messages(customerId) : [];
        var rows = slice.Items.Select(x => new RequestRow(x.Id, x.CanonicalRequestId, x.Type, x.Title, x.Status, x.ReceivedAtUtc, x.DueAtUtc, x.ResolvedAtUtc, x.OpportunityId,
            pages.Where(pageRow => x.ConfluencePageIds.Contains(pageRow.Id)).Select(pageRow => new LinkedRecord(pageRow.Id, pageRow.Title, pageRow.SourceUrl)).ToArray(),
            issues.Where(issue => x.JiraIssueIds.Contains(issue.Id)).Select(issue => new LinkedRecord(issue.Id, issue.Title, issue.SourceUrl)).ToArray(),
            messages.Count(message => message.CanonicalRequestIds.Contains(x.CanonicalRequestId)), x.SourceUrl)).ToArray();
        return support.Respond(customer, new RequestData(rows, slice.Total, page, pageSize), [SourceNames.Salesforce, SourceNames.Jira, SourceNames.Confluence, SourceNames.Slack]);
    }
}

public sealed class GetNewsHandler(QuerySupport support, INewsReader news)
{
    public static readonly string[] Sorts = ["publishedDesc", "headlineAsc"];
    public DashboardResponse<NewsData>? Handle(string customerId, ReportingPeriod period, int page, int pageSize, string sort, string? topic)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var available = support.Available(customerId, SourceNames.CustomerNews);
        var articles = available ? news.Articles(customerId).Where(x => x.Match.Domain == customer.Domain && period.Contains(DateOnly.FromDateTime(x.PublishedAtUtc.UtcDateTime))).ToArray() : [];
        var deduped = DashboardMetrics.DeduplicateNews(articles);
        if (!string.IsNullOrWhiteSpace(topic)) deduped = deduped.Where(x => x.Topics.Contains(topic)).ToArray();
        deduped = sort == "headlineAsc" ? deduped.OrderBy(x => x.Headline).ToArray() : deduped.OrderByDescending(x => x.PublishedAtUtc).ToArray();
        var slice = PageSlice.Take(deduped, page, pageSize);
        var rows = slice.Items.Select(x => new NewsRow(x.Id, x.Headline, x.Summary, x.Publisher, x.SourceUrl, x.PublishedAtUtc, x.EventDate, x.Topics, x.Match.Method, x.Relevance, x.IsSimulated)).ToArray();
        var removed = articles.Length - DashboardMetrics.DeduplicateNews(articles).Count;
        var warnings = removed > 0 ? new[] { $"{removed} duplicate article(s) were collapsed by canonical URL." } : [];
        return support.Respond(customer, new NewsData(rows, slice.Total, page, pageSize, removed), [SourceNames.CustomerNews], warnings: warnings);
    }
}

public sealed class GetMarketHandler(QuerySupport support, IMarketReader market, IAlexisReader alexis)
{
    public DashboardResponse<MarketData>? Handle(string customerId, ReportingPeriod period)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var available = support.Available(customerId, SourceNames.MarketData);
        var usage = support.Available(customerId, SourceNames.Alexis) ? alexis.Usage(customerId) : [];
        var rows = available ? market.Indicators(customerId).Where(x => period.Overlaps(x.PeriodStart, x.PeriodEndExclusive)).Select(x => new MarketRow(
            x.Id, x.Name, x.Kind, x.Provider, x.Sector, x.Geography, x.MetricId, x.DefinitionVersion, x.PeriodStart, x.PeriodEndExclusive, x.ObservedAtUtc,
            x.Value is null ? Measured.Missing(x.Unit) : Measured.Number(x.Value.Value, x.Unit), x.Methodology, x.SampleSize, x.SourceUrl, x.IsSimulated,
            CustomerComparison(usage, x))).ToArray() : [];
        return support.Respond(customer, new MarketData(rows), [SourceNames.MarketData, SourceNames.Alexis]);
    }

    private static Measured CustomerComparison(IReadOnlyList<UsageRecord> usage, MarketIndicator indicator)
    {
        if (indicator.MetricId != "usage-growth" || indicator.Unit != "Percent") return Measured.NotApplicable("Percent");
        if (indicator.PeriodEndExclusive <= indicator.PeriodStart || indicator.PeriodEndExclusive.DayNumber - indicator.PeriodStart.DayNumber > 366)
            return Measured.NotApplicable("Percent");
        var window = new ReportingPeriod(indicator.PeriodStart, indicator.PeriodEndExclusive);
        var current = usage.Where(x => x.PeriodStart == window.From && x.PeriodEndExclusive == window.To).ToArray();
        if (current.Select(x => x.Unit).Distinct().Count() != 1) return Measured.NotApplicable("Percent");
        var baselineWindow = window.Previous();
        var baseline = usage.Where(x => x.Unit == current[0].Unit && x.PeriodStart == baselineWindow.From && x.PeriodEndExclusive == baselineWindow.To).Sum(x => (decimal?)x.Quantity);
        if (usage.All(x => x.PeriodStart != baselineWindow.From || x.PeriodEndExclusive != baselineWindow.To)) baseline = null;
        return DashboardMetrics.Growth(current.Sum(x => x.Quantity), baseline);
    }
}

public sealed class GetAccountHandler(QuerySupport support, ISalesforceReader salesforce)
{
    public DashboardResponse<AccountData>? Handle(string customerId)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var available = support.Available(customerId, SourceNames.Salesforce);
        var account = available ? salesforce.Accounts().SingleOrDefault(x => x.CustomerId == customerId) : null;
        if (!available || account is null)
            return support.Respond(customer, new AccountData("", "", [], [], null, Measured.Unavailable("Day"), null, null, null, [], [], null), [SourceNames.Salesforce]);
        var today = support.Today();
        var renewal = account.Renewal is null ? null : new RenewalView(account.Renewal.Date, account.Renewal.Value.Amount, account.Renewal.Value.Currency, account.Renewal.Status);
        var data = new AccountData(account.Id, account.Owner.DisplayName,
            account.Team.Select(x => new TeamView(x.Id, x.DisplayName, x.Role)).ToArray(),
            account.Stakeholders.Select(x => new StakeholderView(x.Id, x.DisplayName, x.Role)).ToArray(),
            renewal, DashboardMetrics.RenewalHorizon(account.Renewal?.Date, today), account.NextQbrDate, account.LastMeaningfulInteractionAtUtc, account.SuccessPlanUrl,
            account.Actions.Select(x => new ActionView(x.Id, x.Title, x.OwnerId, x.DueDate, x.Status, x.Status is "Open" or "InProgress" && x.DueDate < today)).ToArray(),
            account.Risks.Select(x => new RiskView(x.Id, x.Description, x.Severity, x.OwnerId, x.Status)).ToArray(), null);
        var response = support.Respond(customer, data, [SourceNames.Salesforce], warnings: ["Account fields are a point-in-time snapshot, not period totals. Contact emails are omitted until authorization is enabled."]);
        return response with { Data = data with { AsOfUtc = response.Meta.Sources.Single().LastSuccessfulExtractionAtUtc } };
    }
}

public sealed class GetMetricsHandler(QuerySupport support, ISalesforceReader salesforce, IAlexisReader alexis, IJiraReader jira)
{
    public DashboardResponse<MetricsData>? Handle(string customerId, ReportingPeriod period, ReportingPeriod comparison)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var alexisOn = support.Available(customerId, SourceNames.Alexis);
        var salesforceOn = support.Available(customerId, SourceNames.Salesforce);
        var jiraOn = support.Available(customerId, SourceNames.Jira);
        var revenue = alexisOn ? alexis.Revenue(customerId) : [];
        var opportunities = salesforceOn ? salesforce.Opportunities(customerId) : [];
        var requests = salesforceOn ? salesforce.Requests(customerId) : [];
        var account = salesforceOn ? salesforce.Accounts().SingleOrDefault(x => x.CustomerId == customerId) : null;
        var targets = account?.Targets ?? [];
        var today = support.Today();
        var currency = customer.ReportingCurrency;
        var currentRevenue = alexisOn ? CommercialMetrics.Revenue(revenue, period, currency) : (decimal?)null;
        var baselineRevenue = alexisOn ? CommercialMetrics.Revenue(revenue, comparison, currency) : (decimal?)null;
        var weighted = salesforceOn ? CommercialMetrics.WeightedPipeline(opportunities, period, currency) : 0m;
        var expansion = salesforceOn ? CommercialMetrics.WeightedPipeline(opportunities.Where(x => x.Kind == "Upsell"), period, currency) : 0m;
        var usage = alexisOn ? DashboardMetrics.Usage(alexis.Usage(customerId), period) : null;
        var objectives = alexisOn ? alexis.SlaObjectives(customerId) : [];
        var events = alexisOn ? alexis.SlaEvents(customerId).Where(x => period.Contains(DateOnly.FromDateTime(x.OccurredAtUtc.UtcDateTime))) : [];
        var sla = alexisOn ? DashboardMetrics.SlaCompliance(events, objectives.ToDictionary(x => x.Id, x => x.ThresholdMinutes)) : Measured.Unavailable("Percent");
        var cost = jiraOn && alexisOn ? DashboardMetrics.DeliveryCost(customerId, currency, customer.ReportingTimeZone, period, jira.Worklogs(), jira.Features(customerId), alexis.LaborRates(), alexis.ExternalCosts(customerId)) : null;
        var issues = jiraOn ? jira.Issues(customerId) : [];
        MetricTarget? Target(string id) => targets.SingleOrDefault(x => x.MetricId == id && x.DefinitionVersion == 1 && x.PeriodStart == period.From && x.PeriodEndExclusive == period.To);
        Measured Compare(string id, Measured actual)
        {
            var target = Target(id);
            if (target is null) return Measured.NotApplicable(actual.Unit);
            if (actual.Value is null) return Measured.NotApplicable(actual.Unit);
            return DashboardMetrics.Growth(actual.Value, target.Value);
        }
        var rows = new MetricRow[]
        {
            Row("revenue-growth", alexisOn ? DashboardMetrics.Growth(currentRevenue, baselineRevenue) : Measured.Unavailable("Percent"), null, Measured.NotApplicable("Percent")),
            Row("pipeline-coverage", salesforceOn ? DashboardMetrics.PipelineCoverage(weighted, Target("weighted-pipeline"), currency, period) : Measured.Unavailable("Ratio"), Target("weighted-pipeline")?.Value, Measured.NotApplicable("Ratio")),
            Row("expansion-pipeline", salesforceOn ? Measured.Number(expansion, currency) : Measured.Unavailable(currency), Target("expansion-pipeline")?.Value, Compare("expansion-pipeline", salesforceOn ? Measured.Number(expansion, currency) : Measured.Unavailable(currency))),
            Row("usage-adoption", usage?.Adoption ?? Measured.Unavailable("Percent"), Target("usage-adoption")?.Value, Measured.NotApplicable("Percent")),
            Row("sla-compliance", sla, Target("sla-compliance")?.Value, Measured.NotApplicable("Percent")),
            Row("open-requests", salesforceOn ? Measured.Number(requests.Count(x => DashboardMetrics.OpenRequestStatuses.Contains(x.Status)), "Count") : Measured.Unavailable("Count"), Target("open-requests")?.Value, Measured.NotApplicable("Count")),
            Row("overdue-requests", salesforceOn ? Measured.Number(DashboardMetrics.OverdueRequests(requests, today), "Count") : Measured.Unavailable("Count"), Target("overdue-requests")?.Value, Measured.NotApplicable("Count")),
            Row("backlog-age", jiraOn ? DashboardMetrics.BacklogAge(issues, today) : Measured.Unavailable("Day"), Target("backlog-age")?.Value, Measured.NotApplicable("Day")),
            Row("renewal-horizon", salesforceOn ? DashboardMetrics.RenewalHorizon(account?.Renewal?.Date, today) : Measured.Unavailable("Day"), Target("renewal-horizon")?.Value, Measured.NotApplicable("Day")),
            Row("delivery-cost", cost?.Total ?? Measured.Unavailable(currency), Target("delivery-cost")?.Value, cost?.Coverage ?? Measured.Unavailable("Percent"))
        };
        var partial = usage?.Partial == true || cost?.UncoveredHours > 0;
        return support.Respond(customer, new MetricsData(comparison.From, comparison.To, rows), [SourceNames.Alexis, SourceNames.Salesforce, SourceNames.Jira], partial,
            [DashboardMetrics.RetentionReason, "Comparison uses the previous window unless compareFrom and compareTo are both supplied."]);
    }

    private static MetricRow Row(string id, Measured actual, decimal? target, Measured comparison)
    {
        var definition = DashboardMetrics.Catalog.Single(x => x.Id == id);
        return new(definition.Id, definition.Version, definition.Name, definition.Unit, definition.Formula, definition.Direction, definition.MissingBehavior, definition.Sources, actual, target, comparison);
    }
}

public sealed class GetSourcesHandler(QuerySupport support, ISourceStatusReader sources)
{
    public DashboardResponse<SourceListData>? Handle(string customerId)
    {
        var customer = support.Find(customerId);
        if (customer is null) return null;
        var rows = sources.Read(customerId);
        return support.Respond(customer, new SourceListData(rows), rows.Select(x => x.Source).ToArray());
    }
}

public sealed record CapabilitySection(string Id, string Title, string Tool, bool Implemented, string[] Arguments);
public sealed record CapabilityCatalog(string Mode, string DateRule, string CurrencyRule, string Endpoint, IReadOnlyList<CapabilitySection> Sections);

public sealed class GetCapabilitiesHandler
{
    public CapabilityCatalog Handle() => new("Mock", "from is inclusive and to is exclusive, spanning 1 to 366 days.",
        "Amounts stay in the customer reporting currency. Missing values are not zero.", "http://127.0.0.1:5080/mcp",
        [
            new("customers", "Customers", "list_customers", true, ["search", "page", "pageSize"]),
            new("overview", "Overview", "get_customer_overview", true, ["customerId", "from", "to"]),
            new("commercial", "Commercial", "get_commercial_summary", true, ["customerId", "from", "to"]),
            new("usage", "Usage", "get_usage_summary", true, ["customerId", "from", "to"]),
            new("slas", "Service and SLAs", "get_sla_summary", true, ["customerId", "from", "to"]),
            new("delivery", "Delivery and costs", "get_work_items", true, ["customerId", "from", "to", "page", "pageSize", "sort"]),
            new("requests", "Requests and history", "get_request_history", true, ["customerId", "from", "to", "page", "pageSize", "sort"]),
            new("news", "Customer news", "get_customer_news", true, ["customerId", "from", "to", "page", "pageSize", "sort", "topic"]),
            new("market", "Market intelligence", "get_market_data", true, ["customerId", "from", "to"]),
            new("account", "Account management", "get_account_summary", true, ["customerId"]),
            new("metrics", "Metrics", "get_customer_metrics", true, ["customerId", "from", "to", "compareFrom", "compareTo"]),
            new("sources", "Data sources", "get_customer_sources", true, ["customerId"])
        ]);
}
