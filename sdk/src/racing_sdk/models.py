from __future__ import annotations
from dataclasses import dataclass
from typing import List


@dataclass(frozen=True)
class Vector2:
    x: float
    y: float


@dataclass(frozen=True)
class Action:
    steering: float = 0.0  # -1.0 ~ 1.0
    throttle: float = 1.0  #  0.0 ~ 1.0
    brake: float = 0.0  #  0.0 ~ 1.0


@dataclass(frozen=True)
class CarTelemetry:
    position: Vector2
    velocity: Vector2
    speed: float
    rotation: float
    angular_velocity: float
    current_steering: float
    current_throttle: float
    current_brake: float


@dataclass(frozen=True)
class TrackTelemetry:
    distance_along_track: float
    lateral_offset: float
    heading_error: float
    track_width: float
    future_curvatures: List[float]


@dataclass(frozen=True)
class LapTelemetry:
    current_lap: int
    total_laps: int
    current_lap_time: float
    last_lap_time: float
    best_lap_time: float
    next_checkpoint_idx: int
    total_checkpoints: int


@dataclass(frozen=True)
class OpponentTelemetry:
    participant_id: str
    name: str
    position: Vector2
    velocity: Vector2
    speed: float
    rotation: float
    distance_along_track: float
    lateral_offset: float


@dataclass(frozen=True)
class Observation:
    race_id: str
    tick: int
    timestamp_ms: int
    player: CarTelemetry
    track: TrackTelemetry
    lap: LapTelemetry
    opponents: List[OpponentTelemetry]


@dataclass(frozen=True)
class RaceRules:
    tick_rate: int
    action_timeout_ms: int
    max_consecutive_timeouts: int
    total_laps: int


@dataclass(frozen=True)
class RaceInfo:
    race_id: str
    track_id: str
    rules: RaceRules


@dataclass(frozen=True)
class RaceResult:
    rank: int
    total_time_seconds: float
    fastest_lap_seconds: float
    is_dnf: bool
    reason: str
