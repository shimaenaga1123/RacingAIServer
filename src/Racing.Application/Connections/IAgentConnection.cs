using System;
using System.Threading;
using System.Threading.Tasks;
using Racing.Contracts.V1;
using Racing.Domain.Cars;
using Action = Racing.Contracts.V1.Action;

namespace Racing.Application.Connections;

public interface IAgentConnection
{
    Guid ParticipantId { get; }
    string AgentName { get; }
    bool IsConnected { get; }

    ValueTask SendAsync(ServerMessage message, CancellationToken ct = default);
    Task<DriverAction?> WaitForActionAsync(ulong tick, TimeSpan timeout, CancellationToken ct = default);
    void SubmitAction(Action action);
    void Close();
}
