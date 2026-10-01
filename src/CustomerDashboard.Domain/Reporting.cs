namespace CustomerDashboard.Domain;

public sealed record Money(decimal Amount, string Currency);
public sealed record RevenueEntry(string Id, string CustomerId, DateOnly RecognitionDate, Money Amount, string Category = "Recurring", string Description = "");
public sealed record Opportunity(string Id, string CustomerId, string Stage, Money Amount, decimal Probability, DateOnly ExpectedCloseDate, string Name = "", string Kind = "NewBusiness", string SourceUrl = "");

public readonly record struct ReportingPeriod
{
    public DateOnly From { get; }
    public DateOnly To { get; }

    public ReportingPeriod(DateOnly from, DateOnly to)
    {
        if (to <= from || to.DayNumber - from.DayNumber > 366)
            throw new ArgumentException("Choose an inclusive start and exclusive end spanning 1–366 days.");
        From = from;
        To = to;
    }

    public bool Contains(DateOnly date) => date >= From && date < To;
    public bool Overlaps(DateOnly start, DateOnly endExclusive) => start < To && endExclusive > From;
    public bool Covers(DateOnly start, DateOnly endExclusive) => start >= From && endExclusive <= To;
    public int LengthDays => To.DayNumber - From.DayNumber;
    public ReportingPeriod Previous() => new(From.AddDays(-LengthDays), From);
}

public static class CommercialMetrics
{
    public static decimal Revenue(IEnumerable<RevenueEntry> entries, ReportingPeriod period, string currency) =>
        Round(entries.Where(x => period.Contains(x.RecognitionDate)).Sum(x => InCurrency(x.Amount, currency)));

    public static decimal WeightedPipeline(IEnumerable<Opportunity> opportunities, ReportingPeriod period, string currency) =>
        Round(opportunities.Where(x => x.Stage is not ("Won" or "Lost") && period.Contains(x.ExpectedCloseDate))
            .Sum(x => x.Probability is >= 0 and <= 1
                ? InCurrency(x.Amount, currency) * x.Probability
                : throw new ArgumentException("Opportunity probability must be between zero and one.")));

    private static decimal InCurrency(Money money, string currency) =>
        money.Currency == currency ? money.Amount : throw new ArgumentException("Currency conversion is not configured.");
    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}

