using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Racing.Application.Connections;

public sealed class AgentConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, IAgentConnection> _connections = new();

    public void Register(IAgentConnection connection)
    {
        _connections.AddOrUpdate(connection.ParticipantId, connection, (_, old) =>
        {
            old.Close();
            return connection;
        });
    }

    public bool TryGet(Guid participantId, out IAgentConnection? connection)
    {
        return _connections.TryGetValue(participantId, out connection);
    }

    public bool Remove(Guid participantId)
    {
        if (_connections.TryRemove(participantId, out var conn))
        {
            conn.Close();
            return true;
        }
        return false;
    }

    public ICollection<IAgentConnection> GetAll() => _connections.Values;
}
