using System;
using Racing.Domain.Geometry;

namespace Racing.Simulation.Tracks;

public sealed class TrackCoordinateConverter
{
    private readonly TrackRuntime _track;

    public TrackCoordinateConverter(TrackRuntime track)
    {
        _track = track;
    }

    public int FindNearestSampleIndex(Vector2 worldPos, int hintIndex = -1, int searchRadius = 30)
    {
        int count = _track.SampleCount;

        // 힌트가 유효하면 힌트 주변 반경을 먼저 탐색 (고속 경로)
        if (hintIndex >= 0 && hintIndex < count)
        {
            float bestDistSq = float.MaxValue;
            int bestIdx = hintIndex;

            for (int offset = -searchRadius; offset <= searchRadius; offset++)
            {
                int idx = (hintIndex + offset + count) % count;
                float distSq = Vector2.DistanceSquared(_track.Centerline[idx], worldPos);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIdx = idx;
                }
            }

            // 만약 탐색 범위 끝자락이 아니라면 국소 최솟값이므로 바로 반환
            return bestIdx;
        }

        // 전체 선형 탐색 (초기화 또는 텔레포트 시)
        float minSq = float.MaxValue;
        int nearest = 0;
        for (int i = 0; i < count; i++)
        {
            float dSq = Vector2.DistanceSquared(_track.Centerline[i], worldPos);
            if (dSq < minSq)
            {
                minSq = dSq;
                nearest = i;
            }
        }

        return nearest;
    }

    public TrackPosition Convert(Vector2 worldPos, float worldHeading, int hintIndex = -1)
    {
        int count = _track.SampleCount;
        int nearestIdx = FindNearestSampleIndex(worldPos, hintIndex);

        // 가장 가까운 점과 인접한 두 점 사이의 세그먼트 중 더 가까운 세그먼트에 투영
        int prevIdx = (nearestIdx - 1 + count) % count;
        int nextIdx = (nearestIdx + 1) % count;

        var segPrev = new Segment2D(_track.Centerline[prevIdx], _track.Centerline[nearestIdx]);
        var segNext = new Segment2D(_track.Centerline[nearestIdx], _track.Centerline[nextIdx]);

        Vector2 projPrev = segPrev.ClosestPoint(worldPos);
        Vector2 projNext = segNext.ClosestPoint(worldPos);

        float distPrev = Vector2.Distance(worldPos, projPrev);
        float distNext = Vector2.Distance(worldPos, projNext);

        Vector2 projected;
        int baseIdx;
        float segmentT;

        if (distPrev <= distNext)
        {
            projected = projPrev;
            baseIdx = prevIdx;
            segmentT = segPrev.Length > 1e-4f ? Vector2.Distance(_track.Centerline[prevIdx], projected) / segPrev.Length : 0f;
        }
        else
        {
            projected = projNext;
            baseIdx = nearestIdx;
            segmentT = segNext.Length > 1e-4f ? Vector2.Distance(_track.Centerline[nearestIdx], projected) / segNext.Length : 0f;
        }

        // 1. DistanceAlongTrack (S)
        float baseS = _track.DistanceAlongTrack[baseIdx];
        int nextBaseIdx = (baseIdx + 1) % count;
        float segLength = nextBaseIdx == 0 
            ? _track.TotalLength - baseS 
            : _track.DistanceAlongTrack[nextBaseIdx] - baseS;

        float distanceAlongTrack = baseS + segLength * segmentT;
        if (distanceAlongTrack >= _track.TotalLength)
        {
            distanceAlongTrack -= _track.TotalLength;
        }

        // 2. LateralOffset (L)
        // 트랙 탄젠트와 노멀 기준
        Vector2 tangent = _track.Tangents[nearestIdx];
        Vector2 normal = _track.Normals[nearestIdx]; // 우측이 +normal

        Vector2 offsetVector = worldPos - projected;
        // normal과의 Dot이 양수이면 오른쪽, 음수이면 왼쪽
        float lateralOffset = Vector2.Dot(offsetVector, normal);

        // 3. HeadingError (헤딩 차이)
        float trackAngle = MathF.Atan2(tangent.Y, tangent.X);
        float headingError = NormalizeAngle(worldHeading - trackAngle);

        // 4. Track Width & OnTrack 여부
        float trackWidth = _track.Widths[nearestIdx];
        bool isOnTrack = MathF.Abs(lateralOffset) <= (trackWidth * 0.5f);

        return new TrackPosition(
            DistanceAlongTrack: distanceAlongTrack,
            LateralOffset: lateralOffset,
            HeadingError: headingError,
            TrackWidth: trackWidth,
            IsOnTrack: isOnTrack
        );
    }

    public float[] GetAheadCurvatures(float currentDistance, float[] lookaheadDistances)
    {
        var result = new float[lookaheadDistances.Length];
        int count = _track.SampleCount;

        for (int k = 0; k < lookaheadDistances.Length; k++)
        {
            float targetDist = (currentDistance + lookaheadDistances[k]) % _track.TotalLength;
            int idx = (int)MathF.Round((targetDist / _track.TotalLength) * count) % count;

            int prev = (idx - 1 + count) % count;
            int next = (idx + 1) % count;

            Vector2 t1 = _track.Tangents[prev];
            Vector2 t2 = _track.Tangents[next];

            float angleDiff = NormalizeAngle(MathF.Atan2(t2.Y, t2.X) - MathF.Atan2(t1.Y, t1.X));
            float ds = MathF.Max(0.1f, _track.DistanceAlongTrack[next] - _track.DistanceAlongTrack[prev]);
            if (ds < 0) ds += _track.TotalLength;

            result[k] = angleDiff / ds; // 곡률 = dtheta / ds
        }

        return result;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;
        while (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }
}
