from __future__ import annotations
import queue
import threading
import sys
import ssl
from typing import Optional
import grpc

from ._grpc import racing_pb2, racing_pb2_grpc
from .agent import Agent
from .models import (
    Action,
    CarTelemetry,
    LapTelemetry,
    Observation,
    OpponentTelemetry,
    RaceInfo,
    RaceResult,
    RaceRules,
    TrackTelemetry,
    Vector2,
)


class RacingSession:
    def __init__(
        self,
        server_url: str,
        session_token: str,
        agent_name: str,
        insecure: bool = True,
        ssl_cert: Optional[bytes] = None,
    ):
        self._server_url = server_url.replace("http://", "").replace("https://", "")
        self._session_token = session_token
        self._agent_name = agent_name
        self._insecure = insecure
        self._ssl_cert = ssl_cert
        self._send_queue: queue.Queue[Optional[racing_pb2.ClientMessage]] = queue.Queue()
        self._stop_event = threading.Event()

    def run(self, agent: Agent) -> None:
        """Connect to the server and execute the agent loop."""
        if self._insecure:
            channel = grpc.insecure_channel(self._server_url)
        else:
            root_certs = self._ssl_cert
            if not root_certs and sys.platform == "win32" and ("localhost" in self._server_url or "127.0.0.1" in self._server_url):
                try:
                    pems = [
                        ssl.DER_cert_to_PEM_cert(c[0]).encode("utf-8")
                        for c in ssl.enum_certificates("Root")
                    ]
                    if pems:
                        root_certs = b"\n".join(pems)
                except Exception:
                    pass

            creds = grpc.ssl_channel_credentials(root_certificates=root_certs)
            channel = grpc.secure_channel(self._server_url, creds)

        stub = racing_pb2_grpc.RacingServiceStub(channel)

        def request_generator():
            # 1. First message: Hello
            hello_msg = racing_pb2.ClientMessage(
                hello=racing_pb2.Hello(
                    agent_name=self._agent_name,
                    session_token=self._session_token,
                    sdk_version="0.1.0",
                )
            )
            yield hello_msg

            # 2. Subsequent actions from queue
            while not self._stop_event.is_set():
                try:
                    msg = self._send_queue.get(timeout=0.1)
                    if msg is None:
                        break
                    yield msg
                except queue.Empty:
                    continue

        try:
            responses = stub.Connect(request_generator())
            for server_msg in responses:
                case = server_msg.WhichOneof("payload")

                if case == "welcome":
                    pass

                elif case == "race_assigned":
                    assigned = server_msg.race_assigned
                    rules = RaceRules(
                        tick_rate=assigned.rules.tick_rate,
                        action_timeout_ms=assigned.rules.action_timeout_ms,
                        max_consecutive_timeouts=assigned.rules.max_consecutive_timeouts,
                        total_laps=assigned.rules.total_laps,
                    )
                    info = RaceInfo(
                        race_id=assigned.race_id,
                        track_id=assigned.track_id,
                        rules=rules,
                    )
                    agent.on_race_start(info)

                    # Send Ready
                    ready_msg = racing_pb2.ClientMessage(
                        ready=racing_pb2.Ready(race_id=assigned.race_id)
                    )
                    self._send_queue.put(ready_msg)

                elif case == "race_started":
                    pass

                elif case == "observation":
                    raw_obs = server_msg.observation
                    obs = self._convert_observation(raw_obs)

                    # Agent decisions
                    action = agent.act(obs)

                    # Send Action back to server
                    action_msg = racing_pb2.ClientMessage(
                        action=racing_pb2.Action(
                            race_id=raw_obs.race_id,
                            tick=raw_obs.tick,
                            steering=float(action.steering),
                            throttle=float(action.throttle),
                            brake=float(action.brake),
                        )
                    )
                    self._send_queue.put(action_msg)

                elif case == "race_finished":
                    fin = server_msg.race_finished
                    res = RaceResult(
                        rank=fin.rank,
                        total_time_seconds=fin.total_time_seconds,
                        fastest_lap_seconds=fin.fastest_lap_seconds,
                        is_dnf=fin.is_dnf,
                        reason=fin.dnf_reason,
                    )
                    agent.on_race_finish(res)
                    break

                elif case == "error":
                    print(f"[Error from server] {server_msg.error.message}")
                    break

        except grpc.RpcError as e:
            print(f"[gRPC Connection Closed] {e.code()}: {e.details()}")
        finally:
            self._stop_event.set()
            self._send_queue.put(None)
            channel.close()

    def _convert_observation(self, raw: racing_pb2.Observation) -> Observation:
        player = CarTelemetry(
            position=Vector2(raw.player.position.x, raw.player.position.y),
            velocity=Vector2(raw.player.velocity.x, raw.player.velocity.y),
            speed=raw.player.speed,
            rotation=raw.player.rotation,
            angular_velocity=raw.player.angular_velocity,
            current_steering=raw.player.current_steering,
            current_throttle=raw.player.current_throttle,
            current_brake=raw.player.current_brake,
        )

        track = TrackTelemetry(
            distance_along_track=raw.track.distance_along_track,
            lateral_offset=raw.track.lateral_offset,
            heading_error=raw.track.heading_error,
            track_width=raw.track.track_width,
            future_curvatures=list(raw.track.future_curvatures),
        )

        lap = LapTelemetry(
            current_lap=raw.lap.current_lap,
            total_laps=raw.lap.total_laps,
            current_lap_time=raw.lap.current_lap_time,
            last_lap_time=raw.lap.last_lap_time,
            best_lap_time=raw.lap.best_lap_time,
            next_checkpoint_idx=raw.lap.next_checkpoint_idx,
            total_checkpoints=raw.lap.total_checkpoints,
        )

        opponents = [
            OpponentTelemetry(
                participant_id=o.participant_id,
                name=o.name,
                position=Vector2(o.position.x, o.position.y),
                velocity=Vector2(o.velocity.x, o.velocity.y),
                speed=o.speed,
                rotation=o.rotation,
                distance_along_track=o.distance_along_track,
                lateral_offset=o.lateral_offset,
            )
            for o in raw.opponents
        ]

        return Observation(
            race_id=raw.race_id,
            tick=raw.tick,
            timestamp_ms=raw.timestamp_ms,
            player=player,
            track=track,
            lap=lap,
            opponents=opponents,
        )
