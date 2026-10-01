import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, RequestData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-requests-tab',
  imports: [ResultPanel],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <section class="panel chart-panel"><div class="table-wrap"><table><thead><tr><th>Request</th><th>Type</th><th>Status</th><th>Received</th><th>Due</th><th>Documents</th><th>Work</th><th>Messages</th></tr></thead><tbody>
          @for (row of response.data.items; track row.id) {
            <tr><td><a [href]="row.sourceUrl">{{ row.title }}</a></td><td>{{ row.type }}</td><td>{{ row.status }}</td><td>{{ row.receivedAtUtc }}</td><td>{{ row.dueAtUtc ?? 'None' }}</td>
              <td>@for (link of row.documents; track link.id) { <a [href]="link.url">{{ link.title }}</a> }</td>
              <td>@for (link of row.workItems; track link.id) { <a [href]="link.url">{{ link.title }}</a> }</td>
              <td>{{ row.relatedMessages }}</td></tr>
          }
          @if (!response.data.items.length) { <tr><td colspan="8">No requests were received in this period.</td></tr> }
        </tbody></table></div></section>
      }
    </app-result>`,
})
export class RequestsTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<RequestData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.requests(this.ctx.customerId(), this.ctx.from(), this.ctx.to())); }
}
