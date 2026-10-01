# Data dictionary

Status: definitions used by the mock dashboard handlers. Formulas live in `CustomerDashboard.Domain` and are the calculation authority. Fixture samples are fictional.

## Response envelope

Customer-scoped and list responses use:

| Field | Meaning |
| --- | --- |
| customerId | Canonical customer id, or null for the customer list |
| data | Endpoint payload |
| meta.schemaVersion | `1` |
| meta.generatedAtUtc | Time the response was created. Not an extraction time |
| meta.reportingCurrency | Customer reporting currency, or null for the list |
| meta.isPartial | True when a contributing source is unavailable or a calculation is incomplete |
| meta.sources | Contributing sources only. Each has `source`, `mode`, `status` (`Available` or `Unavailable`), `lastSuccessfulExtractionAtUtc`, `dataThroughUtc`, and `freshness` (`Fresh` or `Stale`) |
| meta.warnings | Readable gaps. No stack traces or file paths |

`lastSuccessfulExtractionAtUtc` comes from the fixture envelope and does not change on reload. A failed or unavailable source does not erase that timestamp. Freshness compares the testable clock with that timestamp and the source threshold (`DataSources:{Source}:FreshnessMinutes`, otherwise `MockData:FreshnessMinutes`).

Date query parameters `from` and `to` are inclusive start and exclusive end in the customer reporting calendar, spanning 1–366 days. Timestamps are stored in UTC. Worklog rate lookup uses the worklog instant converted to the customer IANA timezone.

## Measured values

| State | Meaning |
| --- | --- |
| Value | Known non-zero number |
| Zero | Known zero. This is not missing data |
| Missing | The source is available but the required input was not recorded |
| NotApplicable | The formula has no defined result, including a zero or missing comparison baseline |
| Unavailable | A contributing source is marked unavailable. Records from that source are omitted and are not replaced with mock substitutes |

Money uses decimal amounts and ISO currency codes. Totals round half away from zero to two decimal places. No exchange rates are configured; a mismatched currency fails the read.

## Commercial

| Measure | Definition |
| --- | --- |
| Recognized revenue | Sum of Alexis revenue amounts with `recognitionDate` in the period. Negative amounts are allowed only for `Adjustment`. This is not bookings and not ARR/MRR |
| Weighted pipeline | Sum of `amount × probability` for Salesforce opportunities in stage Qualification, Proposal, or Negotiation whose `expectedCloseDate` is in the period. Won and Lost are excluded. Probability is 0–1 |
| Projected revenue | Not calculated. An approved revenue schedule is not in the fixture contract, so the state is NotApplicable. Weighted pipeline is not a substitute |
| Expansion pipeline | Weighted pipeline restricted to `kind = Upsell` |
| Pipeline coverage | Weighted pipeline divided by the Salesforce target for metric `weighted-pipeline` when the target period equals the query period, the unit equals the reporting currency, and the target is positive. Otherwise NotApplicable |

## Usage, service, and delivery

| Measure | Definition |
| --- | --- |
| Active users | Distinct `activeUserIds` on usage buckets that overlap the period. Counts are not summed across buckets |
| Usage quantity | Summed only for the same unit. A bucket that is not fully inside the period does not get prorated; the response is partial |
| Entitlement utilization | `quantity / entitlement` when entitlement is positive. A zero quantity with a positive entitlement is Zero. A null entitlement is NotApplicable |
| SLA compliance | `met eligible events / all eligible events × 100`. An eligible event is met when `actualMinutes <= thresholdMinutes`. Zero eligible events is NotApplicable. Ineligible events require an exclusion reason and are outside the denominator. Availability is not inferred from response-time events |
| Feature labor cost | Sum of `approved hours × allocation fraction × historical hourly rate` for the worklog's local date. Unapproved hours are excluded. Fractions on a worklog are greater than 0 and sum to 1; each customer receives only its fraction. Hours with no covering rate stay uncovered and are not priced as zero |
| External cost | Sum of already-attributed Alexis external costs in the period. They are not allocated again |
| Delivery cost | Known labor plus external cost. Coverage is priced approved hours / all approved allocated hours. Incomplete coverage sets `isPartial` |
| Backlog age | Average whole UTC days from issue creation to the clock date for statuses ToDo, InProgress, and Blocked. No open issues is NotApplicable |
| Open requests | Salesforce requests in Open or InProgress. This is a point-in-time count |
| Overdue requests | Open requests whose `dueAtUtc` calendar date is before the clock date. A null due date is not overdue |

## Account, news, market, and retention

Account fields are a Salesforce snapshot as of that source's extraction time. Renewal value is not a period total. Renewal horizon is days from the clock date to `renewal.date`. A null renewal is NotApplicable.

News keeps publication time, event date, and extraction time separate. Articles join on canonical customer id plus an approved domain. `ApprovedDomain` and reviewed `ReviewedAlias` are the only match methods. Items are deduplicated by canonical URL; the lowest id is kept. Fixture articles are simulated. The API returns text and links, not article HTML. Stakeholder email is stored in fixtures and omitted from API responses until customer authorization exists.

Market observations keep their unit, currency, geography, sector, methodology, and `observedAtUtc`. A null benchmark value is Missing, not zero. A customer comparison is NotApplicable when the customer's baseline is missing, zero, or the metric definition/period/unit does not match.

Revenue growth is `(current recognized revenue − baseline) / baseline`. The baseline is the previous window of equal length unless `compareFrom` and `compareTo` are supplied. A zero or missing baseline is NotApplicable.

NRR and GRR are not shown. Recurring, expansion, contraction, and churn cohorts are not separate inputs in the normalized contract, so a recurring total is not labeled NRR, GRR, ARR, or MRR. No composite health score is calculated.

## Metric catalog

Version 1. Direction `Higher` means a larger actual is better. `Lower` means a smaller actual is better.

| Id | Unit | Formula | Missing behavior | Sources |
| --- | --- | --- | --- | --- |
| revenue-growth | Percent | Revenue growth above | NotApplicable when baseline is missing or zero | Alexis |
| pipeline-coverage | Ratio | Pipeline coverage above | NotApplicable when the matching positive target is absent | Salesforce |
| expansion-pipeline | Currency | Expansion pipeline above | Zero when no open upsell is in period | Salesforce |
| usage-adoption | Percent | Mean utilization of buckets with a positive entitlement | NotApplicable when none qualify | Alexis |
| sla-compliance | Percent | SLA compliance above | NotApplicable when no eligible events | Alexis |
| open-requests | Count | Open requests above | Zero when none are open | Salesforce |
| overdue-requests | Count | Overdue requests above | Zero when none are overdue | Salesforce |
| backlog-age | Day | Backlog age above | NotApplicable when nothing is open | Jira |
| renewal-horizon | Day | Renewal horizon above | NotApplicable when renewal date is absent | Salesforce |
| delivery-cost | Currency | Delivery cost above | Missing when approved hours exist and none have a rate | Jira, Alexis |
