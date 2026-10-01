import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, SlaData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-slas-tab',
  imports: [ResultPanel],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <article class="panel kpi"><span class="metric-label">Eligible-event compliance</span><strong>{{ text(response.data.compliance) }}</strong><span class="metric-note">Zero eligible events is N/A, not 0%.</span></article>
        <section class="panel chart-panel"><div class="table-wrap"><table><thead><tr><th>Objective</th><th>Kind</th><th>Target</th><th>Eligible</th><th>Met</th><th>Compliance</th><th>Breaches</th></tr></thead><tbody>
          @for (row of response.data.objectives; track row.objectiveId) { <tr><td>{{ row.name }}</td><td>{{ row.kind }}</td><td>{{ row.targetCompliancePercent }}% / {{ row.thresholdMinutes }} min</td><td>{{ row.eligibleEvents }}</td><td>{{ row.metEvents }}</td><td>{{ text(row.compliance) }}</td><td>{{ row.breachEventIds.join(', ') || 'None' }}</td></tr> }
          @if (!response.data.objectives.length) { <tr><td colspan="7">No service objectives are recorded.</td></tr> }
        </tbody></table></div></section>
      }
    </app-result>`,
})
export class SlasTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<SlaData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.slas(this.ctx.customerId(), this.ctx.from(), this.ctx.to())); }
  text = measuredText;
}
