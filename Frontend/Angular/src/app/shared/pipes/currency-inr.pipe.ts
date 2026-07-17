import { Pipe, PipeTransform } from '@angular/core';

// Formats a number as Indian Rupees (₹) with Indian digit grouping (CLAUDE.md §5).
@Pipe({ name: 'currencyInr', standalone: true })
export class CurrencyInrPipe implements PipeTransform {
  transform(value: number | null | undefined, digits = 2): string {
    if (value === null || value === undefined || Number.isNaN(value)) return '—';
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: digits,
      maximumFractionDigits: digits
    }).format(value);
  }
}
