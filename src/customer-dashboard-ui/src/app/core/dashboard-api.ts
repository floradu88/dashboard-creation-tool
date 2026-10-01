import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface Measured { value: number | null; state: string; unit: string | null }
export interface SourceStatus { source: string; mode: string; status: string; lastSuccessfulExtractionAtUtc: string | null; dataThroughUtc: string | null; freshness: string }
export interface ResponseMeta { schemaVersion: number; generatedAtUtc: string; reportingCurrency: string | null; isPartial: boolean; sources: SourceStatus[]; warnings: string[] }
export interface DashboardResponse<T> { customerId: string | null; data: T; meta: ResponseMeta }
export interface CustomerSummary { id: string; name: string; industry: string; reportingCurrency: string }
export interface CustomerListData { items: CustomerSummary[]; total: number; page: number; pageSize: number }
export interface MonthValue { month: string; revenue: number; weightedPipeline: number }
export interface AttentionItem { code: string; message: string }
export interface OverviewData { revenue: Measured; weightedPipeline: Measured; projectedRevenue: Measured; activeUsers: Measured; slaCompliance: Measured; openRequests: Measured; deliverySpend: Measured; deliveryCoverage: Measured; trend: MonthValue[]; attention: AttentionItem[] }
export interface StageTotal { stage: string; count: number; amount: number; weighted: number }
export interface OpportunityRow { id: string; name: string; kind: string; stage: string; amount: number; currency: string; probability: number; weighted: number; expectedCloseDate: string; sourceUrl: string }
export interface CommercialData { revenue: Measured; weightedPipeline: Measured; projectedRevenue: Measured; stages: StageTotal[]; trend: MonthValue[]; opportunities: OpportunityRow[] }
export interface UsagePoint { featureId: string; unit: string; periodStart: string; periodEndExclusive: string; quantity: number; entitlement: number | null; utilization: Measured; activeUsers: number; aligned: boolean }
export interface UsageData { activeUsers: Measured; adoption: Measured; points: UsagePoint[] }
export interface SlaObjective { objectiveId: string; name: string; kind: string; thresholdMinutes: number; targetCompliancePercent: number; eligibleEvents: number; metEvents: number; compliance: Measured; breachEventIds: string[] }
export interface SlaData { compliance: Measured; objectives: SlaObjective[] }
export interface StatusCount { status: string; count: number }
export interface FeatureCost { featureId: string; featureName: string; labor: number; external: number; uncoveredHours: number }
export interface WorkRow { id: string; key: string; featureId: string | null; type: string; title: string; status: string; createdAtUtc: string; updatedAtUtc: string; resolvedAtUtc: string | null; sourceUrl: string }
export interface WorkData { deliverySpend: Measured; deliveryCoverage: Measured; statuses: StatusCount[]; costs: FeatureCost[]; items: WorkRow[]; total: number; page: number; pageSize: number }
export interface LinkedRecord { id: string; title: string; url: string }
export interface RequestRow { id: string; canonicalRequestId: string; type: string; title: string; status: string; receivedAtUtc: string; dueAtUtc: string | null; resolvedAtUtc: string | null; opportunityId: string | null; documents: LinkedRecord[]; workItems: LinkedRecord[]; relatedMessages: number; sourceUrl: string }
export interface RequestData { items: RequestRow[]; total: number; page: number; pageSize: number }
export interface NewsRow { id: string; headline: string; summary: string; publisher: string; sourceUrl: string; publishedAtUtc: string; eventDate: string | null; topics: string[]; matchMethod: string; relevance: string; isSimulated: boolean }
export interface NewsData { items: NewsRow[]; total: number; page: number; pageSize: number; duplicatesRemoved: number }
export interface MarketRow { id: string; name: string; kind: string; provider: string; sector: string; geography: string; metricId: string; definitionVersion: number; periodStart: string; periodEndExclusive: string; observedAtUtc: string; benchmark: Measured; methodology: string; sampleSize: number | null; sourceUrl: string; isSimulated: boolean; customerComparison: Measured }
export interface MarketData { indicators: MarketRow[] }
export interface AccountData { accountId: string; ownerName: string; team: { id: string; displayName: string; role: string }[]; stakeholders: { id: string; displayName: string; role: string }[]; renewal: { date: string; amount: number; currency: string; status: string } | null; renewalHorizon: Measured; nextQbrDate: string | null; lastMeaningfulInteractionAtUtc: string | null; successPlanUrl: string | null; actions: { id: string; title: string; ownerId: string; dueDate: string; status: string; overdue: boolean }[]; risks: { id: string; description: string; severity: string; ownerId: string; status: string }[]; asOfUtc: string | null }
export interface MetricRow { id: string; version: number; name: string; unit: string; formula: string; direction: string; missingBehavior: string; sources: string[]; actual: Measured; target: number | null; comparison: Measured }
export interface MetricsData { comparisonFrom: string; comparisonTo: string; metrics: MetricRow[] }
export interface SourceListData { sources: SourceStatus[] }

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);
  customers(search = '', page = 1, pageSize = 25) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<DashboardResponse<CustomerListData>>('/api/v1/customers', { params });
  }
  overview(customerId: string, from: string, to: string) { return this.period<OverviewData>(customerId, 'overview', from, to); }
  commercial(customerId: string, from: string, to: string) { return this.period<CommercialData>(customerId, 'commercial', from, to); }
  usage(customerId: string, from: string, to: string) { return this.period<UsageData>(customerId, 'usage', from, to); }
  slas(customerId: string, from: string, to: string) { return this.period<SlaData>(customerId, 'slas', from, to); }
  work(customerId: string, from: string, to: string) { return this.period<WorkData>(customerId, 'work-items', from, to); }
  requests(customerId: string, from: string, to: string) { return this.period<RequestData>(customerId, 'requests', from, to); }
  news(customerId: string, from: string, to: string) { return this.period<NewsData>(customerId, 'news', from, to); }
  market(customerId: string, from: string, to: string) { return this.period<MarketData>(customerId, 'market-data', from, to); }
  account(customerId: string) { return this.http.get<DashboardResponse<AccountData>>(`/api/v1/customers/${encodeURIComponent(customerId)}/account`); }
  metrics(customerId: string, from: string, to: string, compareFrom: string, compareTo: string) {
    let params = new HttpParams().set('from', from).set('to', to);
    if (compareFrom && compareTo) params = params.set('compareFrom', compareFrom).set('compareTo', compareTo);
    return this.http.get<DashboardResponse<MetricsData>>(`/api/v1/customers/${encodeURIComponent(customerId)}/metrics`, { params });
  }
  sources(customerId: string) { return this.http.get<DashboardResponse<SourceListData>>(`/api/v1/customers/${encodeURIComponent(customerId)}/sources`); }
  private period<T>(customerId: string, path: string, from: string, to: string) {
    return this.http.get<DashboardResponse<T>>(`/api/v1/customers/${encodeURIComponent(customerId)}/${path}`, { params: new HttpParams().set('from', from).set('to', to) });
  }
}
