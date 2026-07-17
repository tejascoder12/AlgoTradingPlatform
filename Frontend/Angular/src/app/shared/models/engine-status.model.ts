// Mirrors AlgoTrading.Business.DTOs.EngineStatusDto.
export interface EngineStatus {
  isOnline: boolean;
  lastHeartbeat: string | null;
  secondsSinceHeartbeat: number | null;
  source: string | null;
}
