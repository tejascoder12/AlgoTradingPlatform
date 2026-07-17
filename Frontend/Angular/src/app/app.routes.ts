import { Routes } from '@angular/router';

// Lazy-loaded routes, one per feature (CLAUDE.md §5).
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
  },
  {
    path: 'trades',
    loadComponent: () =>
      import('./features/trades/trades.component').then((m) => m.TradesComponent)
  },
  {
    path: 'charts',
    loadComponent: () =>
      import('./features/charts/charts.component').then((m) => m.ChartsComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
