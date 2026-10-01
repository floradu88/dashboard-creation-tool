import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap } from 'rxjs';
import { CustomerListData, DashboardApi, DashboardResponse } from '../../core/dashboard-api';

@Component({
  selector: 'app-company-list',
  imports: [FormsModule, RouterLink, MatButtonModule],
  templateUrl: './company-list.html',
})
export class CompanyList {
  private readonly api = inject(DashboardApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly pageSize = 4;
  readonly from = '2026-09-01';
  readonly to = '2026-12-01';
  search = '';
  page = 1;
  readonly result = signal<DashboardResponse<CustomerListData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor() {
    this.route.queryParamMap.pipe(
      switchMap(query => {
        this.search = query.get('search') ?? '';
        const requested = Number(query.get('page') ?? '1');
        this.page = Number.isInteger(requested) && requested > 0 ? requested : 1;
        if (this.result() === null) this.loading.set(true);
        this.error.set('');
        return this.api.customers(this.search, this.page, this.pageSize).pipe(
          catchError(() => {
            this.error.set('We could not load companies. Check the API, then try again.');
            return of(null);
          }),
        );
      }),
      takeUntilDestroyed(),
    ).subscribe(page => {
      this.loading.set(false);
      if (page) this.result.set(page);
    });
  }

  submit() {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { search: this.search.trim() || null, page: null } });
  }

  go(page: number) {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { search: this.search.trim() || null, page: page > 1 ? page : null } });
  }

  range(data: CustomerListData) {
    if (!data.total) return '0 companies';
    const start = (data.page - 1) * data.pageSize + 1;
    const end = Math.min(data.page * data.pageSize, data.total);
    return `${start}–${end} of ${data.total}`;
  }

  canPrevious(data: CustomerListData) { return data.page > 1; }
  canNext(data: CustomerListData) { return data.page * data.pageSize < data.total; }
}
