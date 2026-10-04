using System.Collections.Generic;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Tracks;
using Xunit;

namespace Racing.Simulation.Tests;

public class SplineTrackBuilderTests
{
    [Fact]
    public void Build_ShouldGenerateValidTrackRuntime()
    {
        var trackDef = new TrackDefinition
        {
            Id = "test-track",
            Name = "Test Track",
            IsLoop = true,
            DefaultLaps = 3,
            Points = new List<TrackPoint>
            {
                new(new Vector2(0f, 0f), 10f),
                new(new Vector2(100f, 0f), 10f),
                new(new Vector2(100f, 100f), 10f),
                new(new Vector2(0f, 100f), 10f)
            }
        };

        var builder = new SplineTrackBuilder(targetSampleStep: 1.0f, checkpointCount: 8);
        var runtime = builder.Build(trackDef);

        Assert.NotNull(runtime);
        Assert.True(runtime.TotalLength > 300f);
        Assert.True(runtime.SampleCount > 100);
        Assert.Equal(8, runtime.Checkpoints.Count);
        Assert.Equal(runtime.SampleCount, runtime.Centerline.Length);
        Assert.Equal(runtime.SampleCount, runtime.LeftBoundaries.Length);
        Assert.Equal(runtime.SampleCount, runtime.RightBoundaries.Length);
    }
}
