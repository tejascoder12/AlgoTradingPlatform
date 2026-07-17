// Mirrors AlgoTrading.Business.DTOs.TodaySummaryDto.
export interface TodaySummary {
  sessionDate: string;
  totalTrades: number;
  wins: number;
  losses: number;
  openPositions: number;
  grossPnl: number;
  realizedPnl: number;
  winRate: number;
  bestTradePnl: number;
  worstTradePnl: number;
  bestTradeSymbol: string | null;
  worstTradeSymbol: string | null;
  hasTraded: boolean;
}
