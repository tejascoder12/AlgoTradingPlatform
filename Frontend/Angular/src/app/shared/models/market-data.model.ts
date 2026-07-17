// Mirrors AlgoTrading.Business.DTOs.OhlcCandleDto.
export interface OhlcCandle {
  time: string;
  symbol: string;
  interval: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}
