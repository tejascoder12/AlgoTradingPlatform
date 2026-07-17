import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpoints } from '../../../core/config/api-endpoints';
import { OhlcCandle } from '../../../shared/models/market-data.model';

// Calls only the market-data / candle endpoints.
@Injectable({ providedIn: 'root' })
export class ChartDataService {
  private http = inject(HttpClient);

  getCandles(symbol: string, interval = '1m', limit = 200): Observable<OhlcCandle[]> {
    const params = new HttpParams()
      .set('symbol', symbol)
      .set('interval', interval)
      .set('limit', limit);
    return this.http.get<OhlcCandle[]>(ApiEndpoints.marketData.candles, { params });
  }
}
