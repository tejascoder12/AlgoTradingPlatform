import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EMPTY, Subscription, catchError, of, switchMap, timer } from 'rxjs';
import { DashboardService } from './services/dashboard.service';
import { PnlSummary } from '../../shared/models/pnl-summary.model';
import { TodaySummary } from '../../shared/models/today-summary.model';
import { EngineStatus } from '../../shared/models/engine-status.model';
import { CurrencyInrPipe } from '../../shared/pipes/currency-inr.pipe';
import { PnlColorPipe } from '../../shared/pipes/pnl-color.pipe';

// Must stay below the API's 15s heartbeat threshold so the badge reflects a
// dead engine promptly. Drop this poll once LiveFeedHub has a broadcaster and
// the status arrives over SignalR instead (CLAUDE.md §5).
const REFRESH_MS = 5000;

const ENGINE_OFFLINE: EngineStatus = {
  isOnline: false,
  lastHeartbeat: null,
  secondsSinceHeartbeat: null,
  source: null
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CurrencyInrPipe, PnlColorPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private dashboard = inject(DashboardService);
  private subs = new Subscription();

  readonly summary = signal<PnlSummary | null>(null);
  readonly today = signal<TodaySummary | null>(null);
  readonly engine = signal<EngineStatus | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    // All-time summary: loaded once — it only moves when a trade closes.
    this.dashboard.getPnlSummary().subscribe({
      next: (s) => {
        this.summary.set(s);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load P&L summary. Is the API running?');
        this.loading.set(false);
      }
    });

    // Today's session refreshes while the engine trades. A failed poll keeps the
    // last good value rather than tearing the stream down.
    this.subs.add(
      timer(0, REFRESH_MS)
        .pipe(switchMap(() => this.dashboard.getTodaySummary().pipe(catchError(() => EMPTY))))
        .subscribe((t) => this.today.set(t))
    );

    // A failed status poll means we cannot see the engine — report it as OFFLINE.
    this.subs.add(
      timer(0, REFRESH_MS)
        .pipe(switchMap(() => this.dashboard.getEngineStatus().pipe(catchError(() => of(ENGINE_OFFLINE)))))
        .subscribe((e) => this.engine.set(e))
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }
}
