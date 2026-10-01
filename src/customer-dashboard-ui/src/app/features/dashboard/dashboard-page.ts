import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DashboardApi } from '../../core/dashboard-api';
import { DashboardContext } from '../../core/dashboard-context';

@Component({
  selector: 'app-dashboard-page',
  imports: [FormsModule, RouterLink, RouterLinkActive, RouterOutlet, MatButtonModule],
  templateUrl: './dashboard-page.html',
})
export class DashboardPage {
  private readonly api = inject(DashboardApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly ctx = inject(DashboardContext);
  customerId = '';
  from = '2026-09-01';
  to = '2026-12-01';
  compareFrom = '';
  compareTo = '';
  readonly tabs = [
    ['overview', 'Overview'], ['commercial', 'Commercial'], ['usage', 'Usage'],
    ['slas', 'Service & SLAs'], ['delivery', 'Delivery & costs'], ['requests', 'Requests & history'],
    ['news', 'Customer news'], ['market', 'Market intelligence'], ['account', 'Account management'],
    ['metrics', 'Metrics'], ['sources', 'Data sources'],
  ];

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(query => {
      this.from = query.get('from') ?? '2026-09-01';
      this.to = query.get('to') ?? '2026-12-01';
      this.compareFrom = query.get('compareFrom') ?? '';
      this.compareTo = query.get('compareTo') ?? '';
      this.customerId = query.get('customer') ?? this.customerId;
      this.ctx.from.set(this.from);
      this.ctx.to.set(this.to);
      this.ctx.compareFrom.set(this.compareFrom);
      this.ctx.compareTo.set(this.compareTo);
      if (query.get('customer')) this.ctx.customerId.set(query.get('customer')!);
    });
    this.api.customers().pipe(takeUntilDestroyed()).subscribe({
      next: page => {
        this.ctx.customers.set(page.data.items);
        if (!this.customerId && page.data.items[0]) {
          this.customerId = page.data.items[0].id;
          this.apply();
        } else this.ctx.customerId.set(this.customerId);
      },
      error: () => this.ctx.customerId.set(''),
    });
  }

  title() { return this.tabs.find(tab => this.router.url.includes('/' + tab[0]))?.[1] ?? 'Dashboard'; }
  apply() {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { customer: this.customerId, from: this.from, to: this.to, compareFrom: this.compareFrom || null, compareTo: this.compareTo || null } });
  }
}
