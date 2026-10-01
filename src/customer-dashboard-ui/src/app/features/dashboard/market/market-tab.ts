import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, MarketData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-market-tab',
  imports: [ResultPanel, DatePipe],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <p class="banner quiet">Benchmarks are observations for their own period. They are not period totals for the account.</p>
        <section class="panel chart-panel"><div class="table-wrap"><table><thead><tr><th>Indicator</th><th>Provider</th><th>Scope</th><th>Period</th><th>Observed</th><th>Benchmark</th><th>Customer comparison</th><th>Method</th></tr></thead><tbody>
          @for (row of response.data.indicators; track row.id) {
            <tr><td><a [href]="row.sourceUrl">{{ row.name }}</a> @if (row.isSimulated) { <span class="small-badge">Simulated</span> }</td><td>{{ row.provider }}</td><td>{{ row.sector }} · {{ row.geography }}</td><td>{{ row.periodStart }} – {{ row.periodEndExclusive }}</td><td>{{ row.observedAtUtc | date:'MMM d, y':'UTC' }}</td><td>{{ text(row.benchmark) }}</td><td>{{ text(row.customerComparison) }}</td><td>{{ row.methodology }}</td></tr>
          }
          @if (!response.data.indicators.length) { <tr><td colspan="8">No market observations overlap this period.</td></tr> }
        </tbody></table></div></section>
      }
    </app-result>`,
})
export class MarketTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<MarketData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.market(this.ctx.customerId(), this.ctx.from(), this.ctx.to())); }
  text = measuredText;
}
