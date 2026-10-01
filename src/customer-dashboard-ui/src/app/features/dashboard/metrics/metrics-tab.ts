import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, MetricsData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-metrics-tab',
  imports: [ResultPanel],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <p class="banner quiet">Comparison window {{ response.data.comparisonFrom }} to {{ response.data.comparisonTo }}. There is no composite health score.</p>
        <section class="panel chart-panel"><div class="table-wrap"><table><thead><tr><th>Metric</th><th>Actual</th><th>Target</th><th>Comparison</th><th>Direction</th><th>Definition</th></tr></thead><tbody>
          @for (row of response.data.metrics; track row.id) { <tr><td>{{ row.name }}<div class="metric-note">v{{ row.version }} · {{ row.sources.join(', ') }}</div></td><td>{{ text(row.actual) }}</td><td>{{ row.target ?? 'None' }}</td><td>{{ text(row.comparison) }}</td><td>{{ row.direction }}</td><td>{{ row.formula }} {{ row.missingBehavior }}</td></tr> }
        </tbody></table></div></section>
      }
    </app-result>`,
})
export class MetricsTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<MetricsData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() {
    watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.metrics(this.ctx.customerId(), this.ctx.from(), this.ctx.to(), this.ctx.compareFrom(), this.ctx.compareTo()));
  }
  text = measuredText;
}
