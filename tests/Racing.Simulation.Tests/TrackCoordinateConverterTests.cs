using System;
using System.Collections.Generic;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Tracks;
using Xunit;

namespace Racing.Simulation.Tests;

public class TrackCoordinateConverterTests
{
    private readonly TrackRuntime _track;
    private readonly TrackCoordinateConverter _converter;

    public TrackCoordinateConverterTests()
    {
        // 간단한 100x100 사각 루프 트랙 생성
        var trackDef = new TrackDefinition
        {
            Id = "loop",
            Name = "Loop",
            IsLoop = true,
            Points = new List<TrackPoint>
            {
                new(new Vector2(0f, 0f), 10f),
                new(new Vector2(100f, 0f), 10f),
                new(new Vector2(100f, 100f), 10f),
                new(new Vector2(0f, 100f), 10f)
            }
        };

        _track = new SplineTrackBuilder(targetSampleStep: 1f).Build(trackDef);
        _converter = new TrackCoordinateConverter(_track);
    }

    [Fact]
    public void Convert_CenterPoint_ShouldHaveNearZeroLateralOffset()
    {
        // 트랙의 10번째 샘플 지점 중심
        var samplePos = _track.Centerline[10];
        var tangent = _track.Tangents[10];
        float heading = MathF.Atan2(tangent.Y, tangent.X);

        var trackPos = _converter.Convert(samplePos, heading);

        Assert.True(MathF.Abs(trackPos.LateralOffset) < 0.1f);
        Assert.True(MathF.Abs(trackPos.HeadingError) < 0.1f);
        Assert.True(trackPos.IsOnTrack);
    }

    [Fact]
    public void Convert_RightShiftedPoint_ShouldHavePositiveLateralOffset()
    {
        var samplePos = _track.Centerline[10];
        var normal = _track.Normals[10]; // 오른쪽 방향
        var shiftedPos = samplePos + normal * 3f;

        var tangent = _track.Tangents[10];
        float heading = MathF.Atan2(tangent.Y, tangent.X);

        var trackPos = _converter.Convert(shiftedPos, heading);

        Assert.True(trackPos.LateralOffset > 2.5f && trackPos.LateralOffset < 3.5f);
        Assert.True(trackPos.IsOnTrack);
    }

    [Fact]
    public void Convert_OffTrackPoint_ShouldSetIsOnTrackFalse()
    {
        var samplePos = _track.Centerline[10];
        var normal = _track.Normals[10];
        // 폭이 10m이므로 중심에서 10m 나가면 명백히 트랙 밖임
        var shiftedPos = samplePos + normal * 10f;

        var trackPos = _converter.Convert(shiftedPos, 0f);

        Assert.False(trackPos.IsOnTrack);
    }
}
