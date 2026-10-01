import { Component, DestroyRef, inject, signal } from '@angular/core';
import { CommercialData, DashboardApi, DashboardResponse } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { BarChartPanel } from '../../../shared/bar-chart';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-commercial-tab',
  imports: [ResultPanel, BarChartPanel],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <section class="kpis"><article class="panel kpi"><span class="metric-label">Recognized revenue</span><strong>{{ text(response.data.revenue) }}</strong></article><article class="panel kpi"><span class="metric-label">Weighted pipeline</span><strong>{{ text(response.data.weightedPipeline) }}</strong></article><article class="panel kpi"><span class="metric-label">Projected revenue</span><strong>{{ text(response.data.projectedRevenue) }}</strong><span class="metric-note">Not a substitute for weighted pipeline</span></article></section>
        <section class="panel chart-panel"><h2>Pipeline by stage</h2><app-bar-chart [categories]="response.data.stages.map(row => row.stage)" [series]="[{ name: 'Amount', values: response.data.stages.map(row => row.amount) }, { name: 'Weighted open pipeline', values: response.data.stages.map(row => row.weighted) }]" [unit]="response.meta.reportingCurrency ?? ''" label="Opportunity amount and weighted open pipeline by stage." /></section>
        <section class="panel chart-panel"><div class="table-wrap"><table><thead><tr><th>Opportunity</th><th>Kind</th><th>Stage</th><th>Amount</th><th>Probability</th><th>Weighted</th><th>Expected close</th></tr></thead><tbody>
          @for (row of response.data.opportunities; track row.id) { <tr><td><a [href]="row.sourceUrl">{{ row.name }}</a></td><td>{{ row.kind }}</td><td>{{ row.stage }}</td><td>{{ row.amount }} {{ row.currency }}</td><td>{{ row.probability }}</td><td>{{ row.weighted }}</td><td>{{ row.expectedCloseDate }}</td></tr> }
          @if (!response.data.opportunities.length) { <tr><td colspan="7">No opportunities with an expected close in this period.</td></tr> }
        </tbody></table></div></section>
      }
    </app-result>`,
})
export class CommercialTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<CommercialData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.commercial(this.ctx.customerId(), this.ctx.from(), this.ctx.to())); }
  text = measuredText;
}
