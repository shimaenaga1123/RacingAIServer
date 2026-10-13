import * as signalR from "@microsoft/signalr";
import { SIGNALR_URL } from "./api";

export interface CarSnapshot {
  participantId: string;
  name: string;
  x: number;
  y: number;
  rotation: number;
  speed: number;
  steering: number;
  lap: number;
  nextCheckpoint: number;
  distanceAlongTrack: number;
  lateralOffset: number;
  isOnTrack: boolean;
}

export interface RaceSnapshot {
  raceId: string;
  tick: number;
  timestampMs: number;
  cars: CarSnapshot[];
}

export interface SpectatorCallbacks {
  onSnapshot: (snapshot: RaceSnapshot) => void;
  onRaceStarted?: (raceId: string) => void;
  onRaceFinished?: (raceId: string) => void;
  onConnectionStateChanged?: (
    state: signalR.HubConnectionState,
  ) => void;
}

export function createSpectatorConnection(
  callbacks: SpectatorCallbacks,
): signalR.HubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(SIGNALR_URL)
    .withAutomaticReconnect([0, 1000, 3000, 5000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  let activeRaceId: string | null = null;

  connection.on("ReceiveSnapshot", (data: RaceSnapshot) => {
    callbacks.onSnapshot(data);
  });

  connection.on("RaceStarted", async (raceId: string) => {
    activeRaceId = raceId;

    try {
      await connection.invoke("JoinRace", raceId);
    } catch (error) {
      console.error("레이스 그룹 참가 실패:", error);
    }

    callbacks.onRaceStarted?.(raceId);
  });

  connection.on("RaceFinished", (raceId: string) => {
    callbacks.onRaceFinished?.(raceId);
  });

  connection.onreconnecting(() => {
    callbacks.onConnectionStateChanged?.(
      signalR.HubConnectionState.Reconnecting,
    );
  });

  connection.onreconnected(async () => {
    callbacks.onConnectionStateChanged?.(
      signalR.HubConnectionState.Connected,
    );

    if (activeRaceId) {
      try {
        await connection.invoke("JoinRace", activeRaceId);
      } catch (error) {
        console.error("레이스 재참가 실패:", error);
      }
    }
  });

  connection.onclose(() => {
    callbacks.onConnectionStateChanged?.(
      signalR.HubConnectionState.Disconnected,
    );
  });

  return connection;
}

export async function startSpectatorConnection(
  connection: signalR.HubConnection,
): Promise<void> {
  if (
    connection.state === signalR.HubConnectionState.Connected ||
    connection.state === signalR.HubConnectionState.Connecting
  ) {
    return;
  }

  await connection.start();
}

export async function stopSpectatorConnection(
  connection: signalR.HubConnection | null,
): Promise<void> {
  if (!connection) {
    return;
  }

  if (
    connection.state !== signalR.HubConnectionState.Disconnected
  ) {
    await connection.stop();
  }
}
