import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'companies' },
  { path: 'companies', loadComponent: () => import('./features/customers/company-list').then(m => m.CompanyList) },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard-page').then(m => m.DashboardPage),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      { path: 'overview', loadComponent: () => import('./features/dashboard/overview/overview-tab').then(m => m.OverviewTab) },
      { path: 'commercial', loadComponent: () => import('./features/dashboard/commercial/commercial-tab').then(m => m.CommercialTab) },
      { path: 'usage', loadComponent: () => import('./features/dashboard/usage/usage-tab').then(m => m.UsageTab) },
      { path: 'slas', loadComponent: () => import('./features/dashboard/slas/slas-tab').then(m => m.SlasTab) },
      { path: 'delivery', loadComponent: () => import('./features/dashboard/delivery/delivery-tab').then(m => m.DeliveryTab) },
      { path: 'requests', loadComponent: () => import('./features/dashboard/requests/requests-tab').then(m => m.RequestsTab) },
      { path: 'news', loadComponent: () => import('./features/dashboard/news/news-tab').then(m => m.NewsTab) },
      { path: 'market', loadComponent: () => import('./features/dashboard/market/market-tab').then(m => m.MarketTab) },
      { path: 'account', loadComponent: () => import('./features/dashboard/account/account-tab').then(m => m.AccountTab) },
      { path: 'metrics', loadComponent: () => import('./features/dashboard/metrics/metrics-tab').then(m => m.MetricsTab) },
      { path: 'sources', loadComponent: () => import('./features/dashboard/sources/sources-tab').then(m => m.SourcesTab) },
    ],
  },
  { path: '**', redirectTo: 'companies' },
];
