import { environment } from '../../../environments/environment';

// Central list of endpoint URLs — never hardcode URLs in services (CLAUDE.md §5).
const base = environment.apiBaseUrl;

export const ApiEndpoints = {
  dashboard: {
    pnlSummary: `${base}/dashboard/pnl-summary`,
    todaySummary: `${base}/dashboard/today-summary`,
    engineStatus: `${base}/dashboard/engine-status`
  },
  trades: {
    list: `${base}/trades`,
    byId: (id: number) => `${base}/trades/${id}`
  },
  marketData: {
    candles: `${base}/marketdata/candles`
  }
} as const;
