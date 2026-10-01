import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, SourceListData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-sources-tab',
  imports: [ResultPanel],
  template: `<app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()" />`,
})
export class SourcesTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<SourceListData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.sources(this.ctx.customerId())); }
}
