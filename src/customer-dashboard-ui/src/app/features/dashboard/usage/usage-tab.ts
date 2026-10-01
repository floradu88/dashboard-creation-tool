import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, UsageData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-usage-tab',
  imports: [ResultPanel],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <section class="kpis"><article class="panel kpi"><span class="metric-label">Active users</span><strong>{{ text(response.data.activeUsers) }}</strong></article><article class="panel kpi"><span class="metric-label">Adoption</span><strong>{{ text(response.data.adoption) }}</strong><span class="metric-note">Mean utilization where entitlement is positive</span></article></section>
        <section class="panel chart-panel"><div class="table-wrap"><table><thead><tr><th>Feature</th><th>Period</th><th>Unit</th><th>Quantity</th><th>Entitlement</th><th>Utilization</th><th>Users</th><th>Aligned</th></tr></thead><tbody>
          @for (row of response.data.points; track row.featureId + row.periodStart) { <tr><td>{{ row.featureId }}</td><td>{{ row.periodStart }} – {{ row.periodEndExclusive }}</td><td>{{ row.unit }}</td><td>{{ row.quantity }}</td><td>{{ row.entitlement ?? 'Missing' }}</td><td>{{ text(row.utilization) }}</td><td>{{ row.activeUsers }}</td><td>{{ row.aligned ? 'Yes' : 'Partial' }}</td></tr> }
          @if (!response.data.points.length) { <tr><td colspan="8">No usage buckets overlap this period.</td></tr> }
        </tbody></table></div></section>
      }
    </app-result>`,
})
export class UsageTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<UsageData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.usage(this.ctx.customerId(), this.ctx.from(), this.ctx.to())); }
  text = measuredText;
}
