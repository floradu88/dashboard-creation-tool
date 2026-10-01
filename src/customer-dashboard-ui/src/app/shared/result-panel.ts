import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ResponseMeta } from '../core/dashboard-api';

@Component({
  selector: 'app-result',
  imports: [DatePipe, MatButtonModule],
  template: `
    @if (loading() && !ready()) {
      <section aria-live="polite" aria-busy="true"><p class="muted">Loading customer insights…</p><div class="kpis">@for (n of [1,2,3]; track n) { <div class="skeleton"></div> }</div><div class="skeleton large"></div></section>
    } @else if (error() && !ready()) {
      <section class="panel notice" role="alert"><h2>Data unavailable</h2><p>{{ error() }}</p><button mat-stroked-button type="button" (click)="retry.emit()">Retry</button></section>
    } @else {
      @if (error()) { <section class="panel notice compact" role="alert"><p>{{ error() }}</p><button mat-stroked-button type="button" (click)="retry.emit()">Retry</button></section> }
      @if (meta()?.isPartial) { <p class="banner">Partial data. Some contributing sources or calculations are incomplete.</p> }
      @for (warning of meta()?.warnings ?? []; track warning) { <p class="banner quiet">{{ warning }}</p> }
      <ng-content />
      @if (meta(); as details) {
        <section class="panel sources"><div class="panel-heading"><div><h2>Sources</h2><p>Extraction timestamps stay fixed until the input files change. Response time {{ details.generatedAtUtc | date:'MMM d, y, HH:mm':'UTC' }} UTC.</p></div></div>
          <div class="table-wrap"><table><thead><tr><th>Source</th><th>Mode</th><th>Status</th><th>Last extracted</th><th>Freshness</th></tr></thead><tbody>
            @for (source of details.sources; track source.source) {
              <tr><td class="source-name">{{ source.source }}</td><td>{{ source.mode }}</td><td>{{ source.status }}</td><td>{{ source.lastSuccessfulExtractionAtUtc ? (source.lastSuccessfulExtractionAtUtc | date:'MMM d, y, HH:mm':'UTC') + ' UTC' : 'Unknown' }}</td><td><span class="freshness" [class.stale]="source.freshness === 'Stale'" [class.bad]="source.status === 'Unavailable'">{{ source.freshness }}</span></td></tr>
            }
          </tbody></table></div>
        </section>
      }
    }
  `,
})
export class ResultPanel {
  readonly loading = input(false);
  readonly ready = input(false);
  readonly error = input('');
  readonly meta = input<ResponseMeta | null>(null);
  readonly retry = output<void>();
}
