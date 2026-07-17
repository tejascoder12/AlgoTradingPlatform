import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpoints } from '../../../core/config/api-endpoints';
import { Trade } from '../../../shared/models/trade.model';

// Calls only the trade endpoints.
@Injectable({ providedIn: 'root' })
export class TradesService {
  private http = inject(HttpClient);

  /** Trades for a given ISO date (yyyy-MM-dd); omit for today. */
  getTrades(date?: string): Observable<Trade[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<Trade[]>(ApiEndpoints.trades.list, { params });
  }

  getTrade(id: number): Observable<Trade> {
    return this.http.get<Trade>(ApiEndpoints.trades.byId(id));
  }
}
