import { Pipe, PipeTransform } from '@angular/core';

// Maps a P&L number to a CSS class: positive = green, negative = red, zero = neutral.
@Pipe({ name: 'pnlColor', standalone: true })
export class PnlColorPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined || value === 0) return 'pnl-neutral';
    return value > 0 ? 'pnl-positive' : 'pnl-negative';
  }
}
