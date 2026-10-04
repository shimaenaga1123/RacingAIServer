using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Racing.Application.Connections;
using Racing.Domain.Cars;
using Racing.Simulation.Races;

namespace Racing.Application.Orchestration;

public sealed class ActionCollector
{
    private readonly TimeSpan _timeout;
    private readonly int _maxConsecutiveTimeouts;
    private readonly ConcurrentDictionary<Guid, int> _consecutiveTimeouts = new();
    private readonly ConcurrentDictionary<Guid, DriverAction> _lastActions = new();

    public ActionCollector(TimeSpan? timeout = null, int maxConsecutiveTimeouts = 10)
    {
        _timeout = timeout ?? TimeSpan.FromMilliseconds(50);
        _maxConsecutiveTimeouts = maxConsecutiveTimeouts;
    }

    public async Task<IReadOnlyDictionary<Guid, DriverAction>> CollectActionsAsync(
        Race race,
        IReadOnlyDictionary<Guid, IAgentConnection> connections,
        CancellationToken ct = default)
    {
        var actionResults = new ConcurrentDictionary<Guid, DriverAction>();

        var tasks = race.Drivers
            .Where(d => !d.Progress.IsFinished && !d.Progress.IsDisqualified)
            .Select(async driver =>
            {
                var participantId = driver.ParticipantId;

                if (!connections.TryGetValue(participantId, out var conn) || !conn.IsConnected)
                {
                    HandleTimeout(driver, actionResults);
                    return;
                }

                try
                {
                    var action = await conn.WaitForActionAsync(race.CurrentTick, _timeout, ct);
                    if (action.HasValue)
                    {
                        // 정상 수신: 타임아웃 리셋, 최근 액션 갱신
                        _consecutiveTimeouts[participantId] = 0;
                        _lastActions[participantId] = action.Value;
                        actionResults[participantId] = action.Value;
                    }
                    else
                    {
                        // 타임아웃
                        HandleTimeout(driver, actionResults);
                    }
                }
                catch (Exception)
                {
                    HandleTimeout(driver, actionResults);
                }
            });

        await Task.WhenAll(tasks);

        return actionResults;
    }

    private void HandleTimeout(Driver driver, ConcurrentDictionary<Guid, DriverAction> actionResults)
    {
        var participantId = driver.ParticipantId;
        int timeouts = _consecutiveTimeouts.AddOrUpdate(participantId, 1, (_, current) => current + 1);

        if (timeouts >= _maxConsecutiveTimeouts)
        {
            // 연속 타임아웃 초과 -> 실격 (Disqualified / DNF)
            driver.Progress.IsDisqualified = true;
            driver.Progress.DnfReason = $"Disqualified: Exceeded {_maxConsecutiveTimeouts} consecutive action timeouts";
            actionResults[participantId] = DriverAction.None;
        }
        else
        {
            // 이전 액션 유지 또는 기본 액션(None)
            var fallback = _lastActions.TryGetValue(participantId, out var last) ? last : DriverAction.None;
            actionResults[participantId] = fallback;
        }
    }
}
