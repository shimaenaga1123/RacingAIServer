using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Racing.Contracts.V1;
using Racing.Domain.Cars;
using Action = Racing.Contracts.V1.Action;

namespace Racing.Application.Connections;

public sealed class GrpcAgentConnection : IAgentConnection
{
    private readonly Channel<ServerMessage> _outgoingChannel;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<DriverAction>> _pendingTicks = new();
    private readonly CancellationTokenSource _cts = new();

    public Guid ParticipantId { get; }
    public string AgentName { get; }
    public bool IsConnected => !_cts.IsCancellationRequested;

    public ChannelReader<ServerMessage> OutgoingReader => _outgoingChannel.Reader;

    public GrpcAgentConnection(Guid participantId, string agentName, int outgoingCapacity = 128)
    {
        ParticipantId = participantId;
        AgentName = agentName;
        _outgoingChannel = Channel.CreateBounded<ServerMessage>(new BoundedChannelOptions(outgoingCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async ValueTask SendAsync(ServerMessage message, CancellationToken ct = default)
    {
        if (!IsConnected) return;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        await _outgoingChannel.Writer.WriteAsync(message, linkedCts.Token);
    }

    public void SubmitAction(Action action)
    {
        ulong tick = action.Tick;
        if (_pendingTicks.TryRemove(tick, out var tcs))
        {
            var driverAction = new DriverAction(action.Steering, action.Throttle, action.Brake);
            tcs.TrySetResult(driverAction);
        }
    }

    public async Task<DriverAction?> WaitForActionAsync(ulong tick, TimeSpan timeout, CancellationToken ct = default)
    {
        if (!IsConnected) return null;

        var tcs = _pendingTicks.GetOrAdd(tick, _ => new TaskCompletionSource<DriverAction>(TaskCreationOptions.RunContinuationsAsynchronously));

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        var timeoutTask = Task.Delay(timeout, linkedCts.Token);

        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        _pendingTicks.TryRemove(tick, out _);

        if (completedTask == tcs.Task)
        {
            return await tcs.Task;
        }

        // 타임아웃 발생
        return null;
    }

    public void Close()
    {
        if (_cts.IsCancellationRequested) return;

        _cts.Cancel();
        _outgoingChannel.Writer.TryComplete();

        foreach (var kvp in _pendingTicks)
        {
            kvp.Value.TrySetCanceled();
        }
        _pendingTicks.Clear();
        _cts.Dispose();
    }
}
