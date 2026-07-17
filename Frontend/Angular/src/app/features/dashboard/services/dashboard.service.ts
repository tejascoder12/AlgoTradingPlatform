import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpoints } from '../../../core/config/api-endpoints';
import { PnlSummary } from '../../../shared/models/pnl-summary.model';
import { TodaySummary } from '../../../shared/models/today-summary.model';
import { EngineStatus } from '../../../shared/models/engine-status.model';

// Calls only the dashboard endpoints (one service per feature — CLAUDE.md §5).
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  getPnlSummary(): Observable<PnlSummary> {
    return this.http.get<PnlSummary>(ApiEndpoints.dashboard.pnlSummary);
  }

  getTodaySummary(): Observable<TodaySummary> {
    return this.http.get<TodaySummary>(ApiEndpoints.dashboard.todaySummary);
  }

  getEngineStatus(): Observable<EngineStatus> {
    return this.http.get<EngineStatus>(ApiEndpoints.dashboard.engineStatus);
  }
}
