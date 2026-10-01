import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, OverviewData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { BarChartPanel } from '../../../shared/bar-chart';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-overview-tab',
  imports: [ResultPanel, BarChartPanel],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <section class="kpis" aria-label="Overview metrics">
          @for (card of cards(response); track card.label) {
            <article class="panel kpi"><span class="metric-label">{{ card.label }}</span><strong>{{ card.value }}</strong><span class="metric-note">{{ card.note }}</span></article>
          }
        </section>
        <section class="panel chart-panel"><div class="panel-heading"><div><h2>Commercial outlook</h2><p>Recognized revenue and probability-weighted open opportunities. Projected revenue is not shown because no approved schedule exists.</p></div></div>
          <app-bar-chart [categories]="response.data.trend.map(row => row.month)" [series]="[{ name: 'Recognized revenue', values: response.data.trend.map(row => row.revenue) }, { name: 'Weighted pipeline', values: response.data.trend.map(row => row.weightedPipeline) }]" [unit]="response.meta.reportingCurrency ?? ''" label="Monthly recognized revenue and weighted pipeline. Exact figures are in the table below." />
          <div class="table-wrap"><table><thead><tr><th>Month</th><th>Revenue</th><th>Weighted pipeline</th></tr></thead><tbody>@for (row of response.data.trend; track row.month) { <tr><td>{{ row.month }}</td><td>{{ row.revenue }}</td><td>{{ row.weightedPipeline }}</td></tr> }</tbody></table></div>
        </section>
        <section class="panel chart-panel"><h2>Attention</h2>@if (!response.data.attention.length) { <p>Nothing in the attention list for this window.</p> } @else { <ul>@for (item of response.data.attention; track item.message) { <li>{{ item.message }}</li> }</ul> }</section>
      }
    </app-result>`,
})
export class OverviewTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<OverviewData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() {
    watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.overview(this.ctx.customerId(), this.ctx.from(), this.ctx.to()));
  }
  cards(response: DashboardResponse<OverviewData>) {
    const data = response.data;
    return [
      { label: 'Recognized revenue', value: measuredText(data.revenue), note: 'Alexis · recognition date in period' },
      { label: 'Weighted pipeline', value: measuredText(data.weightedPipeline), note: 'Salesforce · expected close in period' },
      { label: 'Projected revenue', value: measuredText(data.projectedRevenue), note: 'Unavailable without an approved schedule' },
      { label: 'Active users', value: measuredText(data.activeUsers), note: 'Distinct people in overlapping usage buckets' },
      { label: 'SLA compliance', value: measuredText(data.slaCompliance), note: 'Eligible events only' },
      { label: 'Open requests', value: measuredText(data.openRequests), note: 'Point-in-time Salesforce status' },
      { label: 'Delivery spend', value: measuredText(data.deliverySpend), note: `Coverage ${measuredText(data.deliveryCoverage)}` },
    ];
  }
}
