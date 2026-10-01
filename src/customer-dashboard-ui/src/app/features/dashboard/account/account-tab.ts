import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { AccountData, DashboardApi, DashboardResponse } from '../../../core/dashboard-api';
import { DashboardContext } from '../../../core/dashboard-context';
import { measuredText } from '../../../core/format';
import { watchTab } from '../../../core/watch-tab';
import { ResultPanel } from '../../../shared/result-panel';

@Component({
  selector: 'app-account-tab',
  imports: [ResultPanel, DatePipe],
  template: `
    <app-result [loading]="loading()" [ready]="!!data()" [error]="error()" [meta]="data()?.meta ?? null" (retry)="ctx.retry()">
      @if (data(); as response) {
        <p class="banner quiet">Account fields are a snapshot as of {{ response.data.asOfUtc ? (response.data.asOfUtc | date:'MMM d, y, HH:mm':'UTC') + ' UTC' : 'the source extraction' }}. The date filter does not turn them into period totals.</p>
        @if (!response.data.accountId) { <section class="panel notice"><h2>No account snapshot</h2><p>Salesforce did not provide an account for this customer.</p></section> }
        @else {
          <section class="kpis"><article class="panel kpi"><span class="metric-label">Owner</span><strong class="period">{{ response.data.ownerName }}</strong></article><article class="panel kpi"><span class="metric-label">Renewal</span><strong class="period">{{ response.data.renewal ? (response.data.renewal.amount + ' ' + response.data.renewal.currency) : 'Missing' }}</strong><span class="metric-note">{{ response.data.renewal?.date || 'No date' }} · {{ response.data.renewal?.status || 'Unknown' }}</span></article><article class="panel kpi"><span class="metric-label">Renewal horizon</span><strong>{{ text(response.data.renewalHorizon) }}</strong><span class="metric-note">Next QBR {{ response.data.nextQbrDate || 'Missing' }}</span></article></section>
          <section class="panel chart-panel"><h2>Team and stakeholders</h2><div class="table-wrap"><table><thead><tr><th>Name</th><th>Role</th></tr></thead><tbody>@for (person of response.data.team; track person.id) { <tr><td>{{ person.displayName }}</td><td>{{ person.role }}</td></tr> }@for (person of response.data.stakeholders; track person.id) { <tr><td>{{ person.displayName }}</td><td>{{ person.role }}</td></tr> }</tbody></table></div>@if (response.data.successPlanUrl) { <a [href]="response.data.successPlanUrl">Success plan</a> }</section>
          <section class="panel chart-panel"><h2>Actions</h2><div class="table-wrap"><table><thead><tr><th>Action</th><th>Owner</th><th>Due</th><th>Status</th></tr></thead><tbody>@for (row of response.data.actions; track row.id) { <tr><td>{{ row.title }}</td><td>{{ row.ownerId }}</td><td>{{ row.dueDate }}</td><td>{{ row.overdue ? 'Overdue' : row.status }}</td></tr> }@if (!response.data.actions.length) { <tr><td colspan="4">No actions.</td></tr> }</tbody></table></div></section>
          <section class="panel chart-panel"><h2>Risks</h2><div class="table-wrap"><table><thead><tr><th>Risk</th><th>Severity</th><th>Status</th></tr></thead><tbody>@for (row of response.data.risks; track row.id) { <tr><td>{{ row.description }}</td><td>{{ row.severity }}</td><td>{{ row.status }}</td></tr> }@if (!response.data.risks.length) { <tr><td colspan="3">No open risks.</td></tr> }</tbody></table></div></section>
        }
      }
    </app-result>`,
})
export class AccountTab {
  private readonly api = inject(DashboardApi);
  readonly ctx = inject(DashboardContext);
  readonly data = signal<DashboardResponse<AccountData> | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  constructor() { watchTab(this.ctx.revision, inject(DestroyRef), this, () => this.api.account(this.ctx.customerId())); }
  text = measuredText;
}
