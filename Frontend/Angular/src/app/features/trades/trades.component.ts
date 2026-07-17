import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TradesService } from './services/trades.service';
import { Trade } from '../../shared/models/trade.model';
import { CurrencyInrPipe } from '../../shared/pipes/currency-inr.pipe';
import { PnlColorPipe } from '../../shared/pipes/pnl-color.pipe';

@Component({
  selector: 'app-trades',
  standalone: true,
  imports: [CommonModule, CurrencyInrPipe, PnlColorPipe],
  templateUrl: './trades.component.html',
  styleUrl: './trades.component.scss'
})
export class TradesComponent implements OnInit {
  private tradesService = inject(TradesService);

  readonly trades = signal<Trade[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.tradesService.getTrades().subscribe({
      next: (t) => {
        this.trades.set(t);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load trades.');
        this.loading.set(false);
      }
    });
  }
}
