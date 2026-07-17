// Mirrors AlgoTrading.Business.DTOs.TradeDto one-to-one (CLAUDE.md §5).
export interface Trade {
  id: number;
  symbol: string;
  side: string;
  entryTime: string;
  entryPrice: number;
  exitTime: string | null;
  exitPrice: number | null;
  qty: number;
  pnl: number | null;
  setupName: string | null;
  htfTrend: string | null;
  ltfTrigger: string | null;
  isOpen: boolean;
}
