using System;
using System.Collections.Generic;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;

namespace Racing.Simulation.Tracks;

public sealed class SplineTrackBuilder
{
    private readonly float _targetSampleStep;
    private readonly int _checkpointCount;

    public SplineTrackBuilder(float targetSampleStep = 0.5f, int checkpointCount = 12)
    {
        _targetSampleStep = MathF.Max(0.1f, targetSampleStep);
        _checkpointCount = Math.Max(4, checkpointCount);
    }

    public TrackRuntime Build(TrackDefinition definition)
    {
        var rawPoints = definition.Points;
        if (rawPoints.Count < 3)
        {
            throw new ArgumentException("Track must have at least 3 control points.");
        }

        int count = rawPoints.Count;

        // 1단계: 세그먼트별 고밀도 사전 샘플링 (적응형 호 길이 측정)
        const int samplesPerSegment = 50;
        var densePoints = new List<Vector2>();
        var denseWidths = new List<float>();

        int segmentCount = definition.IsLoop ? count : count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            int i0 = (i - 1 + count) % count;
            int i1 = i;
            int i2 = (i + 1) % count;
            int i3 = (i + 2) % count;

            var p0 = rawPoints[i0].Position;
            var p1 = rawPoints[i1].Position;
            var p2 = rawPoints[i2].Position;
            var p3 = rawPoints[i3].Position;

            float w0 = rawPoints[i0].Width;
            float w1 = rawPoints[i1].Width;
            float w2 = rawPoints[i2].Width;
            float w3 = rawPoints[i3].Width;

            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = (float)s / samplesPerSegment;
                densePoints.Add(CatmullRomSpline.Evaluate(p0, p1, p2, p3, t));
                denseWidths.Add(CatmullRomSpline.EvaluateScalar(w0, w1, w2, w3, t));
            }
        }

        // 2단계: 누적 호 길이 계산
        int totalDense = densePoints.Count;
        var denseDistances = new float[totalDense + 1];
        denseDistances[0] = 0f;

        for (int i = 1; i < totalDense; i++)
        {
            denseDistances[i] = denseDistances[i - 1] + Vector2.Distance(densePoints[i - 1], densePoints[i]);
        }

        float totalLength = denseDistances[totalDense - 1];
        if (definition.IsLoop)
        {
            totalLength += Vector2.Distance(densePoints[totalDense - 1], densePoints[0]);
        }
        denseDistances[totalDense] = totalLength;

        // 3단계: 균일한 간격(targetSampleStep)으로 재샘플링
        int sampleCount = Math.Max(10, (int)MathF.Ceiling(totalLength / _targetSampleStep));
        float actualStep = totalLength / sampleCount;

        var centerline = new Vector2[sampleCount];
        var widths = new float[sampleCount];
        var distanceAlongTrack = new float[sampleCount];
        var tangents = new Vector2[sampleCount];
        var normals = new Vector2[sampleCount];
        var leftBoundaries = new Vector2[sampleCount];
        var rightBoundaries = new Vector2[sampleCount];

        int denseCursor = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            float targetDist = i * actualStep;
            distanceAlongTrack[i] = targetDist;

            while (denseCursor < totalDense - 1 && denseDistances[denseCursor + 1] < targetDist)
            {
                denseCursor++;
            }

            float d0 = denseDistances[denseCursor];
            float d1 = denseDistances[denseCursor + 1];
            float span = MathF.Max(1e-5f, d1 - d0);
            float t = (targetDist - d0) / span;

            Vector2 ptA = densePoints[denseCursor];
            Vector2 ptB = denseCursor + 1 < totalDense ? densePoints[denseCursor + 1] : densePoints[0];
            centerline[i] = Vector2.Lerp(ptA, ptB, t);

            float wA = denseWidths[denseCursor];
            float wB = denseCursor + 1 < totalDense ? denseWidths[denseCursor + 1] : denseWidths[0];
            widths[i] = wA + (wB - wA) * t;
        }

        // 4단계: Tangent, Normal, Boundary 계산
        for (int i = 0; i < sampleCount; i++)
        {
            int prev = (i - 1 + sampleCount) % sampleCount;
            int next = (i + 1) % sampleCount;

            Vector2 tangent = (centerline[next] - centerline[prev]).Normalized();
            if (tangent == Vector2.Zero)
            {
                tangent = new Vector2(1f, 0f);
            }

            tangents[i] = tangent;

            // 좌측: -90도, 우측: +90도 (트랙 진행 방향 기준 우측이 +Normal)
            Vector2 normal = new Vector2(-tangent.Y, tangent.X);
            normals[i] = normal;

            float halfWidth = widths[i] * 0.5f;
            leftBoundaries[i] = centerline[i] - normal * halfWidth;
            rightBoundaries[i] = centerline[i] + normal * halfWidth;
        }

        // 5단계: Checkpoints 균일 배치
        var checkpoints = new List<CheckpointDefinition>();
        for (int cp = 0; cp < _checkpointCount; cp++)
        {
            float targetDist = (totalLength / _checkpointCount) * cp;
            int sampleIdx = (int)MathF.Round(targetDist / actualStep) % sampleCount;

            Vector2 center = centerline[sampleIdx];
            Vector2 normal = normals[sampleIdx];
            float halfWidth = widths[sampleIdx] * 0.5f;

            Vector2 left = center - normal * halfWidth;
            Vector2 right = center + normal * halfWidth;

            checkpoints.Add(new CheckpointDefinition(
                Index: cp,
                Center: center,
                Line: new Segment2D(left, right),
                DistanceAlongTrack: distanceAlongTrack[sampleIdx]
            ));
        }

        return new TrackRuntime
        {
            Id = definition.Id,
            Name = definition.Name,
            IsLoop = definition.IsLoop,
            TotalLength = totalLength,
            DefaultLaps = definition.DefaultLaps,
            Centerline = centerline,
            Tangents = tangents,
            Normals = normals,
            Widths = widths,
            DistanceAlongTrack = distanceAlongTrack,
            LeftBoundaries = leftBoundaries,
            RightBoundaries = rightBoundaries,
            Checkpoints = checkpoints
        };
    }
}
