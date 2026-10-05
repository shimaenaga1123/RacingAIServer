using System;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Laps;

public sealed class LapTracker(TrackRuntime track, int totalLaps = 3)
{
    private readonly TrackRuntime _track = track;
    private readonly int _totalLaps = totalLaps;

    public void UpdateProgress(
        DriverProgress progress,
        Vector2 prevPos,
        Vector2 currentPos,
        TrackPosition trackPos,
        float dt)
    {
        if (progress.IsFinished || progress.IsDisqualified)
        {
            return;
        }

        progress.CurrentLapTimeSeconds += dt;
        progress.TotalRaceTimeSeconds += dt;

        int totalCp = _track.Checkpoints.Count;
        if (totalCp == 0) return;

        var moveSegment = new Segment2D(prevPos, currentPos);
        int nextCpIdx = progress.NextCheckpointIndex;
        var nextCp = _track.Checkpoints[nextCpIdx];

        // 1. 체크포인트 통과 여부 검사 (물리적 이동 선분과 체크포인트 라인 교차)
        bool crossed = moveSegment.Intersects(nextCp.Line, out _);

        // 빠른 고속 이동 시 선분 교차 외에도 S 거리 순방향 통과 보조 검사
        if (!crossed)
        {
            float cpDist = nextCp.DistanceAlongTrack;
            float prevDist = progress.LastRecordedDistance;
            float curDist = trackPos.DistanceAlongTrack;

            // 순방향으로 통과한 경우 (트랙 랩 래핑 고려)
            if (nextCpIdx == 0)
            {
                // 랩 통과 지점: 직전 거리가 트랙 끝 근처이고 현재 거리가 트랙 시작 근처
                if (prevDist > _track.TotalLength * 0.8f && curDist < _track.TotalLength * 0.2f)
                {
                    crossed = true;
                }
            }
            else if (prevDist <= cpDist && curDist >= cpDist && (curDist - prevDist) < _track.TotalLength * 0.2f)
            {
                crossed = true;
            }
        }

        if (crossed)
        {
            // 체크포인트 통과 성공!
            if (nextCpIdx == 0)
            {
                // 결승선(0번 체크포인트) 통과 -> 랩 완료!
                float lapTime = progress.CurrentLapTimeSeconds;
                progress.LapTimes.Add(lapTime);
                progress.LastLapTimeSeconds = lapTime;
                if (lapTime < progress.BestLapTimeSeconds)
                {
                    progress.BestLapTimeSeconds = lapTime;
                }

                if (progress.CurrentLap >= _totalLaps)
                {
                    progress.IsFinished = true;
                }
                else
                {
                    progress.CurrentLap++;
                    progress.CurrentLapTimeSeconds = 0f;
                    progress.NextCheckpointIndex = 1;
                }
            }
            else if (nextCpIdx == totalCp - 1)
            {
                // 마지막 체크포인트 통과 -> 다음 목표는 결승선 (0번)
                progress.NextCheckpointIndex = 0;
            }
            else
            {
                // 일반 체크포인트 전진
                progress.NextCheckpointIndex++;
            }
        }

        // 역주행 검사: 트랙 진행 각도와 헤딩 오차가 135도(2.35rad) 이상이면 역주행 경고
        if (MathF.Abs(trackPos.HeadingError) > 2.35f)
        {
            progress.ConsecutiveWrongWayTicks++;
        }
        else
        {
            progress.ConsecutiveWrongWayTicks = 0;
        }

        progress.LastRecordedDistance = trackPos.DistanceAlongTrack;
    }
}
