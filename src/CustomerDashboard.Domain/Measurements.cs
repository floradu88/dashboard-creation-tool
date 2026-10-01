namespace CustomerDashboard.Domain;

public sealed record Measured(decimal? Value, string State, string? Unit)
{
    public static Measured Number(decimal value, string? unit) => new(value, value == 0 ? "Zero" : "Value", unit);
    public static Measured Missing(string? unit) => new(null, "Missing", unit);
    public static Measured NotApplicable(string? unit) => new(null, "NotApplicable", unit);
    public static Measured Unavailable(string? unit) => new(null, "Unavailable", unit);
}

public sealed record FeatureCostLine(string FeatureId, string FeatureName, decimal Labor, decimal External, decimal UncoveredHours);
public sealed record CostComputation(Measured Total, Measured Coverage, decimal UncoveredHours, IReadOnlyList<FeatureCostLine> Lines);
public sealed record UsagePoint(string FeatureId, string Unit, DateOnly PeriodStart, DateOnly PeriodEndExclusive, decimal Quantity, decimal? Entitlement, Measured Utilization, int ActiveUsers, bool Aligned);
public sealed record UsageComputation(Measured ActiveUsers, bool Partial, IReadOnlyList<UsagePoint> Points, Measured Adoption);
public sealed record SlaObjectiveResult(string ObjectiveId, string Name, string Kind, int ThresholdMinutes, decimal TargetCompliancePercent, int EligibleEvents, int MetEvents, Measured Compliance, IReadOnlyList<string> BreachEventIds);
public sealed record MetricDefinition(string Id, int Version, string Name, string Unit, string Formula, string Direction, string MissingBehavior, string[] Sources);

public static class DashboardMetrics
{
    public static readonly string[] OpenWorkStatuses = ["ToDo", "InProgress", "Blocked"];
    public static readonly string[] OpenRequestStatuses = ["Open", "InProgress"];
    public const string ProjectedRevenueReason = "No approved revenue schedule is recorded. Projected revenue is not replaced with weighted pipeline.";
    public const string RetentionReason = "NRR and GRR need recurring expansion, contraction, and churn cohorts. Those inputs are not in the normalized contract.";

    public static readonly MetricDefinition[] Catalog =
    [
        new("revenue-growth", 1, "Revenue growth", "Percent", "Change versus the comparison window divided by that baseline.", "Higher", "Not applicable when the baseline is missing or zero.", ["Alexis"]),
        new("pipeline-coverage", 1, "Pipeline coverage", "Ratio", "Weighted open pipeline divided by the positive target for the same period and currency.", "Higher", "Not applicable without a matching positive target.", ["Salesforce"]),
        new("expansion-pipeline", 1, "Expansion pipeline", "Currency", "Weighted pipeline for open upsell opportunities expected to close in the period.", "Higher", "Zero when no qualifying upsell exists.", ["Salesforce"]),
        new("usage-adoption", 1, "Usage adoption", "Percent", "Mean entitlement utilization for aligned buckets with a positive entitlement.", "Higher", "Not applicable when no positive entitlement is recorded.", ["Alexis"]),
        new("sla-compliance", 1, "SLA compliance", "Percent", "Eligible events within threshold divided by all eligible events.", "Higher", "Not applicable when there are no eligible events.", ["Alexis"]),
        new("open-requests", 1, "Open requests", "Count", "Requests currently Open or InProgress.", "Lower", "Zero when none are open.", ["Salesforce"]),
        new("overdue-requests", 1, "Overdue requests", "Count", "Open requests whose due date is before the clock date.", "Lower", "Zero when none are overdue. A missing due date is not overdue.", ["Salesforce"]),
        new("backlog-age", 1, "Backlog age", "Day", "Average age in days of issues in ToDo, InProgress, or Blocked.", "Lower", "Not applicable when no issue is in the open-status set.", ["Jira"]),
        new("renewal-horizon", 1, "Renewal horizon", "Day", "Days from the clock date until the renewal date.", "Higher", "Not applicable when the renewal date is absent.", ["Salesforce"]),
        new("delivery-cost", 1, "Delivery cost", "Currency", "Approved allocated labor at the historical rate plus already attributed external cost.", "Lower", "Missing when approved hours exist and none have a rate. Incomplete rates are partial.", ["Jira", "Alexis"])
    ];

    public static Measured Growth(decimal? current, decimal? baseline)
    {
        if (current is null || baseline is null || baseline == 0) return Measured.NotApplicable("Percent");
        return Measured.Number(Round((current.Value - baseline.Value) / baseline.Value * 100m, 2), "Percent");
    }

    public static Measured PipelineCoverage(decimal weightedPipeline, MetricTarget? target, string currency, ReportingPeriod period)
    {
        if (target is null || target.PeriodStart != period.From || target.PeriodEndExclusive != period.To || target.Unit != currency)
            return Measured.NotApplicable("Ratio");
        if (target.Value <= 0) return Measured.NotApplicable("Ratio");
        return Measured.Number(Round(weightedPipeline / target.Value, 4), "Ratio");
    }

    public static Measured SlaCompliance(IEnumerable<SlaEvent> events, IReadOnlyDictionary<string, int> thresholdMinutes)
    {
        var eligible = events.Where(x => x.Eligible).ToArray();
        if (eligible.Length == 0) return Measured.NotApplicable("Percent");
        var met = eligible.Count(x => x.ActualMinutes <= thresholdMinutes[x.ObjectiveId]);
        return Measured.Number(Round(met * 100m / eligible.Length, 2), "Percent");
    }

    public static IReadOnlyList<SlaObjectiveResult> SlaResults(IEnumerable<SlaObjective> objectives, IEnumerable<SlaEvent> events, ReportingPeriod period)
    {
        var inPeriod = events.Where(x => period.Contains(DateOnly.FromDateTime(x.OccurredAtUtc.UtcDateTime))).ToArray();
        return objectives.Select(objective =>
        {
            var rows = inPeriod.Where(x => x.ObjectiveId == objective.Id).ToArray();
            var compliance = SlaCompliance(rows, new Dictionary<string, int> { [objective.Id] = objective.ThresholdMinutes });
            var breaches = rows.Where(x => x.Eligible && x.ActualMinutes > objective.ThresholdMinutes).Select(x => x.Id).ToArray();
            return new SlaObjectiveResult(objective.Id, objective.Name, objective.Kind, objective.ThresholdMinutes, objective.TargetCompliancePercent,
                rows.Count(x => x.Eligible), rows.Count(x => x.Eligible && x.ActualMinutes <= objective.ThresholdMinutes), compliance, breaches);
        }).ToArray();
    }

    public static UsageComputation Usage(IEnumerable<UsageRecord> records, ReportingPeriod period)
    {
        var overlapping = records.Where(x => period.Overlaps(x.PeriodStart, x.PeriodEndExclusive)).ToArray();
        var points = overlapping.Select(x =>
        {
            var aligned = period.Covers(x.PeriodStart, x.PeriodEndExclusive);
            var utilization = x.Entitlement is > 0 ? Measured.Number(Round(x.Quantity / x.Entitlement.Value * 100m, 2), "Percent") : Measured.NotApplicable("Percent");
            return new UsagePoint(x.FeatureId, x.Unit, x.PeriodStart, x.PeriodEndExclusive, x.Quantity, x.Entitlement, utilization, x.ActiveUserIds.Distinct().Count(), aligned);
        }).ToArray();
        var adoptionSource = points.Where(x => x.Aligned && x.Utilization.State is "Value" or "Zero").ToArray();
        var adoption = adoptionSource.Length == 0 ? Measured.NotApplicable("Percent")
            : Measured.Number(Round(adoptionSource.Average(x => x.Utilization.Value!.Value), 2), "Percent");
        return new(Measured.Number(overlapping.SelectMany(x => x.ActiveUserIds).Distinct().Count(), "Count"), points.Any(x => !x.Aligned), points, adoption);
    }

    public static CostComputation DeliveryCost(string customerId, string currency, string timeZone, ReportingPeriod period, IEnumerable<Worklog> worklogs, IEnumerable<FeatureRecord> features, IEnumerable<LaborRate> rates, IEnumerable<ExternalCost> externalCosts)
    {
        var lines = new Dictionary<string, (string Name, decimal Labor, decimal External, decimal Uncovered)>();
        decimal approved = 0, covered = 0;
        foreach (var work in worklogs.Where(x => x.Approved))
        {
            var date = TimeZones.LocalDate(work.StartedAtUtc, timeZone);
            if (!period.Contains(date)) continue;
            var rate = rates.SingleOrDefault(x => x.WorkerId == work.WorkerId && date >= x.ValidFrom && date < x.ValidToExclusive);
            foreach (var allocation in work.Allocations.Where(x => x.CustomerId == customerId))
            {
                var hours = work.Hours * allocation.Fraction;
                approved += hours;
                var name = features.SingleOrDefault(x => x.Id == allocation.FeatureId && x.CustomerId == customerId)?.Name ?? allocation.FeatureId;
                if (!lines.TryGetValue(allocation.FeatureId, out var line)) line = (name, 0, 0, 0);
                if (rate is null || rate.HourlyRate.Currency != currency) line.Uncovered += hours;
                else { covered += hours; line.Labor += hours * rate.HourlyRate.Amount; }
                lines[allocation.FeatureId] = line;
            }
        }
        foreach (var cost in externalCosts.Where(x => x.CustomerId == customerId && period.Contains(x.IncurredDate)))
        {
            if (cost.Amount.Currency != currency) throw new ArgumentException("Currency conversion is not configured.");
            if (!lines.TryGetValue(cost.FeatureId, out var line))
                line = (features.SingleOrDefault(x => x.Id == cost.FeatureId)?.Name ?? cost.FeatureId, 0, 0, 0);
            line.External += cost.Amount.Amount;
            lines[cost.FeatureId] = line;
        }
        var result = lines.Select(x => new FeatureCostLine(x.Key, x.Value.Name, Round(x.Value.Labor, 2), Round(x.Value.External, 2), x.Value.Uncovered)).ToArray();
        var known = result.Sum(x => x.Labor + x.External);
        var uncovered = result.Sum(x => x.UncoveredHours);
        var total = approved > 0 && covered == 0 && known == 0 ? Measured.Missing(currency) : Measured.Number(Round(known, 2), currency);
        var coverage = approved == 0 ? Measured.NotApplicable("Percent") : Measured.Number(Round(covered / approved * 100m, 2), "Percent");
        return new(total, coverage, uncovered, result);
    }

    public static Measured BacklogAge(IEnumerable<WorkIssue> issues, DateOnly today)
    {
        var open = issues.Where(x => OpenWorkStatuses.Contains(x.Status)).ToArray();
        if (open.Length == 0) return Measured.NotApplicable("Day");
        var average = open.Average(x => (decimal)(today.DayNumber - DateOnly.FromDateTime(x.CreatedAtUtc.UtcDateTime).DayNumber));
        return Measured.Number(Round(average, 2), "Day");
    }

    public static Measured RenewalHorizon(DateOnly? renewalDate, DateOnly today) =>
        renewalDate is null ? Measured.NotApplicable("Day") : Measured.Number(renewalDate.Value.DayNumber - today.DayNumber, "Day");

    public static int OverdueRequests(IEnumerable<SalesforceRequest> requests, DateOnly today) =>
        requests.Count(x => OpenRequestStatuses.Contains(x.Status) && x.DueAtUtc is DateTimeOffset due && DateOnly.FromDateTime(due.UtcDateTime) < today);

    public static IReadOnlyList<NewsArticle> DeduplicateNews(IEnumerable<NewsArticle> articles) =>
        articles.GroupBy(x => x.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.OrderBy(article => article.Id, StringComparer.Ordinal).First())
            .OrderByDescending(x => x.PublishedAtUtc).ToArray();

    public static decimal Round(decimal amount, int digits) => Math.Round(amount, digits, MidpointRounding.AwayFromZero);
}
