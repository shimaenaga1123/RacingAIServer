using System;
using System.Collections.Generic;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Simulation.Laps;
using Racing.Simulation.Physics;
using Racing.Simulation.Races;
using Racing.Simulation.Tracks;

namespace Racing.Simulation;

public sealed class RaceSimulator(
    TrackRuntime track,
    IPhysicsEngine? physicsEngine = null,
    float fixedDeltaTime = RaceSimulator.DefaultFixedDeltaTime,
    int totalLaps = 3)
{
    private readonly IPhysicsEngine _physicsEngine = physicsEngine ?? new ArcadePhysicsEngine();
    private readonly TrackCoordinateConverter _coordinateConverter = new TrackCoordinateConverter(track);
    private readonly LapTracker _lapTracker = new LapTracker(track, totalLaps);
    private readonly float _dt = fixedDeltaTime;

    public const float DefaultFixedDeltaTime = 1f / 60f;

    public void Step(Race race, IReadOnlyDictionary<Guid, DriverAction> actions)
    {
        if (race.Status != RaceStatus.Running)
        {
            return;
        }

        // 1. Action 적용
        foreach (var driver in race.Drivers)
        {
            if (driver.Progress.IsFinished || driver.Progress.IsDisqualified)
            {
                driver.Car.ApplyAction(DriverAction.None);
                continue;
            }

            if (actions.TryGetValue(driver.ParticipantId, out var action))
            {
                driver.Car.ApplyAction(action);
            }
            else
            {
                // 액션 누락 시 무동작(또는 이전 상태 유지 등)
                driver.Car.ApplyAction(DriverAction.None);
            }
        }

        // 2. 물리 시뮬레이션
        foreach (var driver in race.Drivers)
        {
            if (driver.Progress.IsFinished || driver.Progress.IsDisqualified)
            {
                continue;
            }

            driver.PreviousPosition = driver.Car.Position;
            _physicsEngine.Step(driver.Car, _dt, driver.TrackPosition.IsOnTrack);
        }

        // 3. 차량 간 충돌 판정 및 해결 (Circle-Circle)
        ResolveVehicleCollisions(race.Drivers);

        // 4. 트랙 좌표 변환 및 체크포인트/랩 업데이트
        bool allFinished = true;
        foreach (var driver in race.Drivers)
        {
            driver.TrackPosition = _coordinateConverter.Convert(driver.Car.Position, driver.Car.Rotation);

            _lapTracker.UpdateProgress(
                driver.Progress,
                driver.PreviousPosition,
                driver.Car.Position,
                driver.TrackPosition,
                _dt
            );

            if (!driver.Progress.IsFinished && !driver.Progress.IsDisqualified)
            {
                allFinished = false;
            }
        }

        // 5. 모든 차량 완주 시 레이스 종료
        if (allFinished && race.Drivers.Count > 0)
        {
            race.Finish();
        }

        // 6. 틱 증가
        race.AdvanceTick();
    }

    private static void ResolveVehicleCollisions(IReadOnlyList<Driver> drivers)
    {
        int count = drivers.Count;
        for (int i = 0; i < count; i++)
        {
            var carA = drivers[i].Car;
            for (int j = i + 1; j < count; j++)
            {
                var carB = drivers[j].Car;

                Vector2 delta = carB.Position - carA.Position;
                float distSq = delta.LengthSquared();
                float minDist = carA.Config.CollisionRadius + carB.Config.CollisionRadius;

                if (distSq < minDist * minDist && distSq > 1e-6f)
                {
                    float dist = MathF.Sqrt(distSq);
                    Vector2 normal = delta / dist;
                    float overlap = minDist - dist;

                    // 1) 위치 분리 (각각 겹침의 절반씩 반대 방향으로 이동)
                    carA.Position -= normal * (overlap * 0.5f);
                    carB.Position += normal * (overlap * 0.5f);

                    // 2) 탄성 반발 충격량 계산 (간이 반발 계수 e = 0.5)
                    Vector2 relativeVelocity = carB.Velocity - carA.Velocity;
                    float velAlongNormal = Vector2.Dot(relativeVelocity, normal);

                    // 서로 다가오는 방향일 때만 반발
                    if (velAlongNormal < 0f)
                    {
                        const float restitution = 0.5f;
                        float impulse = -(1f + restitution) * velAlongNormal / 2f;
                        carA.Velocity -= normal * impulse;
                        carB.Velocity += normal * impulse;
                    }
                }
            }
        }
    }
}
