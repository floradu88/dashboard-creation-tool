import { computed, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DashboardContext {
  readonly customers = signal<{ id: string; name: string; industry: string; reportingCurrency: string }[]>([]);
  readonly customerId = signal('');
  readonly from = signal('2026-09-01');
  readonly to = signal('2026-12-01');
  readonly compareFrom = signal('');
  readonly compareTo = signal('');
  readonly attempt = signal(0);
  readonly revision = computed(() => [this.customerId(), this.from(), this.to(), this.compareFrom(), this.compareTo(), this.attempt()].join('|'));
  retry() { this.attempt.update(value => value + 1); }
}
