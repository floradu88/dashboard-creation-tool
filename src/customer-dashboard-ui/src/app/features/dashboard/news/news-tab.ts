import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DashboardApi, DashboardResponse, NewsData } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-news-tab',
  imports: [ResultPanel, DatePipe],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <div class="cards">
          @for (item of response.data.items; track item.id) {
            <article class="panel news-card"><p class="eyebrow">{{ item.isSimulated ? 'SIMULATED' : 'LIVE' }} · {{ item.topics.join(', ') }}</p><h2>{{ item.headline }}</h2><p>{{ item.summary }}</p><p>{{ item.publisher }} · published {{ item.publishedAtUtc | date:'MMM d, y':'UTC' }} UTC @if (item.eventDate) { · event {{ item.eventDate }} }</p><p>{{ item.relevance }} Match: {{ item.matchMethod }}.</p><a [href]="item.sourceUrl">Original source</a></article>
          }
          @if (!response.data.items.length) { <section class="panel notice"><h2>No news in this period</h2><p>No simulated articles were published in the selected window.</p></section> }
        </div>
      }
    </app-result>`,
})
export class NewsTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<NewsData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.news(this.ctx.customerId(), this.ctx.from(), this.ctx.to())); }
}
