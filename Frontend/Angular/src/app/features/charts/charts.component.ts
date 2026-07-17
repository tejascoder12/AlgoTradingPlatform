import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CandlestickSeries,
  createChart,
  IChartApi,
  Time
} from 'lightweight-charts';
import { ChartDataService } from './services/chart-data.service';

@Component({
  selector: 'app-charts',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './charts.component.html',
  styleUrl: './charts.component.scss'
})
export class ChartsComponent implements AfterViewInit, OnDestroy {
  private chartData = inject(ChartDataService);

  @ViewChild('chartContainer', { static: true })
  private chartContainer!: ElementRef<HTMLDivElement>;

  private chart?: IChartApi;
  readonly symbol = signal('NIFTY');
  readonly error = signal<string | null>(null);

  ngAfterViewInit(): void {
    this.chart = createChart(this.chartContainer.nativeElement, {
      autoSize: true,
      layout: { background: { color: '#12141a' }, textColor: '#e5e7eb' },
      grid: { vertLines: { color: '#1f232b' }, horzLines: { color: '#1f232b' } }
    });

    const series = this.chart.addSeries(CandlestickSeries);

    this.chartData.getCandles(this.symbol(), '1m', 200).subscribe({
      next: (candles) => {
        series.setData(
          candles.map((c) => ({
            time: (Date.parse(c.time) / 1000) as Time,
            open: c.open,
            high: c.high,
            low: c.low,
            close: c.close
          }))
        );
      },
      error: () => this.error.set('Could not load candles (no data yet is normal in Phase 1).')
    });
  }

  ngOnDestroy(): void {
    this.chart?.remove();
  }
}
