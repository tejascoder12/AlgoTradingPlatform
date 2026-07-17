// Mirrors AlgoTrading.Business.DTOs.PnlSummaryDto.
export interface PnlSummary {
  totalTrades: number;
  wins: number;
  losses: number;
  openPositions: number;
  grossPnl: number;
  realizedPnl: number;
  winRate: number;
  bestTradePnl: number;
  worstTradePnl: number;
}
