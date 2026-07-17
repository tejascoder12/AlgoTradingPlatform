import { Injectable, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PnlSummary } from '../../shared/models/pnl-summary.model';
import { EngineStatus } from '../../shared/models/engine-status.model';

// Live feed connection. Exposes server pushes as observables/signals (CLAUDE.md §5).
// Hub method names must match LiveFeedHub constants on the API.
@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection?: HubConnection;

  // Connection state as a signal for easy template binding.
  readonly connected = signal(false);

  // Server-pushed streams.
  readonly pnl$ = new Subject<PnlSummary>();
  readonly engineStatus$ = new Subject<EngineStatus>();
  readonly signal$ = new Subject<unknown>();

  /** Opens the SignalR connection and wires the server->client handlers. */
  start(): void {
    if (this.connection) return;

    this.connection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('PnlUpdate', (data: PnlSummary) => this.pnl$.next(data));
    this.connection.on('EngineStatus', (data: EngineStatus) => this.engineStatus$.next(data));
    this.connection.on('SignalUpdate', (data: unknown) => this.signal$.next(data));

    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose(() => this.connected.set(false));

    this.connection
      .start()
      .then(() => this.connected.set(true))
      .catch((err) => console.error('[SignalR] start failed', err));
  }

  /** Closes the connection (e.g. on logout / app teardown). */
  async stop(): Promise<void> {
    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) {
      await this.connection.stop();
    }
    this.connection = undefined;
    this.connected.set(false);
  }
}
