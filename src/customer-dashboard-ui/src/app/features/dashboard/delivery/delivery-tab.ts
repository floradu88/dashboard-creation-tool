import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, WorkData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-delivery-tab',
  imports: [ResultPanel],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <section class="kpis"><article class="panel kpi"><span class="metric-label">Known delivery cost</span><strong>{{ text(response.data.deliverySpend) }}</strong></article><article class="panel kpi"><span class="metric-label">Rate coverage</span><strong>{{ text(response.data.deliveryCoverage) }}</strong><span class="metric-note">Unapproved hours are excluded. Shared hours use the allocation fraction.</span></article></section>
        <section class="panel chart-panel"><h2>Cost by feature</h2><div class="table-wrap"><table><thead><tr><th>Feature</th><th>Labor</th><th>External</th><th>Uncovered hours</th></tr></thead><tbody>
          @for (row of response.data.costs; track row.featureId) { <tr><td>{{ row.featureName }}</td><td>{{ row.labor }}</td><td>{{ row.external }}</td><td>{{ row.uncoveredHours }}</td></tr> }
          @if (!response.data.costs.length) { <tr><td colspan="4">No attributed cost in this period.</td></tr> }
        </tbody></table></div></section>
        <section class="panel chart-panel"><h2>Work</h2><div class="table-wrap"><table><thead><tr><th>Key</th><th>Type</th><th>Title</th><th>Status</th><th>Updated</th></tr></thead><tbody>
          @for (row of response.data.items; track row.id) { <tr><td><a [href]="row.sourceUrl">{{ row.key }}</a></td><td>{{ row.type }}</td><td>{{ row.title }}</td><td>{{ row.status }}</td><td>{{ row.updatedAtUtc }}</td></tr> }
          @if (!response.data.items.length) { <tr><td colspan="5">No linked work in this period.</td></tr> }
        </tbody></table></div><p>{{ response.data.total }} work items</p></section>
      }
    </app-result>`,
})
export class DeliveryTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<WorkData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.work(this.ctx.customerId(), this.ctx.from(), this.ctx.to())); }
  text = measuredText;
}
