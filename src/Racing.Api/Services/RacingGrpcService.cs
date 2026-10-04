using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Racing.Application.Competitions;
using Racing.Application.Connections;
using Racing.Contracts.V1;

namespace Racing.Api.Services;

public sealed class RacingGrpcService : RacingService.RacingServiceBase
{
    private readonly CompetitionManager _competitionManager;
    private readonly AgentConnectionRegistry _connectionRegistry;
    private readonly ILogger<RacingGrpcService> _logger;

    public RacingGrpcService(
        CompetitionManager competitionManager,
        AgentConnectionRegistry connectionRegistry,
        ILogger<RacingGrpcService> logger)
    {
        _competitionManager = competitionManager;
        _connectionRegistry = connectionRegistry;
        _logger = logger;
    }

    public override async Task Connect(
        IAsyncStreamReader<ClientMessage> requestStream,
        IServerStreamWriter<ServerMessage> responseStream,
        ServerCallContext context)
    {
        var ct = context.CancellationToken;

        // 1. 첫 메시지(Hello) 수신 대기
        if (!await requestStream.MoveNext(ct))
        {
            return;
        }

        var firstMsg = requestStream.Current;
        if (firstMsg.PayloadCase != ClientMessage.PayloadOneofCase.Hello)
        {
            await responseStream.WriteAsync(new ServerMessage
            {
                Error = new Error
                {
                    Code = Error.Types.Code.Unauthorized,
                    Message = "First message must be Hello"
                }
            });
            return;
        }

        var hello = firstMsg.Hello;
        _logger.LogInformation("Agent '{Name}' connecting with token '{Token}'", hello.AgentName, hello.SessionToken);

        // 토큰 유효성 검증
        Guid participantId;
        if (_competitionManager.ValidateToken(hello.SessionToken, out var validatedId))
        {
            participantId = validatedId;
        }
        else if (hello.SessionToken.StartsWith("rse_"))
        {
            // 직접 테스트 모드 허용
            participantId = Guid.NewGuid();
        }
        else
        {
            await responseStream.WriteAsync(new ServerMessage
            {
                Error = new Error
                {
                    Code = Error.Types.Code.Unauthorized,
                    Message = "Invalid or expired session token."
                }
            });
            return;
        }

        var connection = new GrpcAgentConnection(participantId, hello.AgentName);
        _connectionRegistry.Register(connection);

        try
        {
            // 2. 환영 메시지 발송
            await responseStream.WriteAsync(new ServerMessage
            {
                Welcome = new Welcome
                {
                    ParticipantId = participantId.ToString(),
                    ServerVersion = "1.0.0",
                    HeartbeatIntervalMs = 5000
                }
            });

            // 3. 양방향 파이프라인 (수신 루프 + 송신 루프)
            var receiveTask = ProcessIncomingAsync(requestStream, connection, ct);
            var sendTask = ProcessOutgoingAsync(responseStream, connection, ct);

            await Task.WhenAny(receiveTask, sendTask);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Connection canceled for participant {Id}", participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent streaming for participant {Id}", participantId);
        }
        finally
        {
            _connectionRegistry.Remove(participantId);
            _logger.LogInformation("Agent '{Name}' ({Id}) disconnected", hello.AgentName, participantId);
        }
    }

    private async Task ProcessIncomingAsync(
        IAsyncStreamReader<ClientMessage> requestStream,
        GrpcAgentConnection connection,
        System.Threading.CancellationToken ct)
    {
        while (await requestStream.MoveNext(ct))
        {
            var msg = requestStream.Current;
            switch (msg.PayloadCase)
            {
                case ClientMessage.PayloadOneofCase.Action:
                    connection.SubmitAction(msg.Action);
                    break;

                case ClientMessage.PayloadOneofCase.Ready:
                    _logger.LogInformation("Agent {Id} ready for race {RaceId}", connection.ParticipantId, msg.Ready.RaceId);
                    break;

                case ClientMessage.PayloadOneofCase.Pong:
                    break;
            }
        }
    }

    private static async Task ProcessOutgoingAsync(
        IServerStreamWriter<ServerMessage> responseStream,
        GrpcAgentConnection connection,
        System.Threading.CancellationToken ct)
    {
        while (await connection.OutgoingReader.WaitToReadAsync(ct))
        {
            while (connection.OutgoingReader.TryRead(out var msg))
            {
                await responseStream.WriteAsync(msg);
            }
        }
    }
}
