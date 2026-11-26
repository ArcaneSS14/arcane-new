using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
// Arcane-Start
using Content.Arcane.Common.Administration;
using Content.Arcane.Common.CCVar;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Network;
using System.Net;
using System.Text.Json;
// Arcane-End

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class QueryTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        Connected = true
    };

    [Test]
    public async Task QuerySingleLog()
    {
        var pair = Pair;
        var server = pair.Server;

        var sSystems = server.ResolveDependency<IEntitySystemManager>();
        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<ServerGameTicker>();

        var date = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        ICommonSession player = default;

        await server.WaitPost(() =>
        {
            player = sPlayers.Sessions.First();

            sAdminLogSystem.Add(LogType.Unknown, $"{player.AttachedEntity:Entity} test log: {guid}");
        });

        var filter = new LogFilter
        {
            Round = sGamerTicker.RoundId,
            Search = guid.ToString(),
            Types = new HashSet<LogType> { LogType.Unknown },
            After = date,
            AnyPlayers = new[] { player.UserId.UserId }
        };

        await PoolManager.WaitUntil(server, async () =>
        {
            foreach (var _ in await sAdminLogSystem.All(filter))
            {
                return true;
            }

            return false;
        });
    }
    // Arcane-Start
    private static PoolSettings HistoryPair => new()
    {
        Connected = true,
        DummyTicker = false,
        AdminLogsEnabled = true,
        Fresh = true,
        Dirty = true,
        Destructive = true,
    };

    [Test]
    [PairConfig(nameof(HistoryPair))]
    [EnsureCVar(Side.Server, typeof(ArcaneCVars), nameof(ArcaneCVars.AhelpHistoryRateLimitCount), 20)]
    public async Task AhelpHistoryChecksTheCurrentPermissionAndChannel()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var admins = Server.ResolveDependency<IAdminManager>();
        var player = ServerSession!;
        var foreignUser = new NetUserId(Guid.NewGuid());
        var marker = Guid.NewGuid().ToString();
        await db.UpdatePlayerRecordAsync(player.UserId, player.Name, IPAddress.Loopback, null);
        await db.UpdatePlayerRecordAsync(foreignUser, $"History-{marker}", IPAddress.Loopback, null);
        var dbServer = await db.AddOrGetServer($"history-permissions-{marker}");
        var round = await db.AddNewRound(dbServer, player.UserId.UserId, foreignUser.UserId);
        using var json = JsonDocument.Parse("{}");
        var logs = new List<AdminLog>();
        foreach (var user in new[] { player.UserId, foreignUser })
        {
            foreach (var adminOnly in new[] { false, true })
            {
                var id = logs.Count + 1;
                logs.Add(new AdminLog
                {
                    RoundId = round,
                    Id = id,
                    Date = DateTime.UtcNow,
                    Type = adminOnly ? LogType.AhelpAdminOnly : LogType.Ahelp,
                    Impact = LogImpact.Low,
                    Message = $"{marker}-{user}-{adminOnly}",
                    Json = json,
                    Players = new()
                    {
                        new AdminLogPlayer { RoundId = round, LogId = id, PlayerUserId = user.UserId },
                    },
                });
            }
        }
        await db.AddAdminLogs(logs);

        var responses = new List<BwoinkHistoryResponse>();
        void OnResponse(object sender, object message)
        {
            if (message is BwoinkHistoryResponse response)
                responses.Add(response);
        }
        await Client.WaitPost(() => CEntMan.EntityNetManager.ReceivedSystemMessage += OnResponse);
        try
        {
            await Server.WaitAssertion(() =>
            {
                admins.PromoteHost(player);
                Assert.That(admins.GetAdminData(player)?.HasFlag(AdminFlags.Adminhelp), Is.True);
            });
            await Client.WaitPost(() => CEntMan.EntityNetManager.SendSystemNetworkMessage(new BwoinkHistoryRequest(foreignUser)));
            await Pair.RunTicksSync(10);
            await Client.WaitAssertion(() =>
            {
                Assert.That(responses, Has.Count.EqualTo(1));
                Assert.That(responses[0].Channel, Is.EqualTo(foreignUser));
                Assert.That(responses[0].Messages.Count(message => message.Text.StartsWith(marker)), Is.EqualTo(2));
                Assert.That(responses[0].Messages.Any(message => message.AdminOnly), Is.True);
            });

            await Server.WaitAssertion(() =>
            {
                admins.DeAdmin(player);
                Assert.That(admins.GetAdminData(player), Is.Null);
            });
            await Client.WaitPost(() => CEntMan.EntityNetManager.SendSystemNetworkMessage(new BwoinkHistoryRequest(player.UserId)));
            await Pair.RunTicksSync(10);
            await Client.WaitAssertion(() =>
            {
                Assert.That(responses, Has.Count.EqualTo(2));
                Assert.That(responses[1].Messages.Count(message => message.Text.StartsWith(marker)), Is.EqualTo(1));
                Assert.That(responses[1].Messages.Any(message => message.AdminOnly), Is.False);
            });

            await Client.WaitPost(() => CEntMan.EntityNetManager.SendSystemNetworkMessage(new BwoinkHistoryRequest(foreignUser)));
            await Pair.RunTicksSync(10);
            await Client.WaitAssertion(() => Assert.That(responses, Has.Count.EqualTo(2)));

            await Server.WaitAssertion(() =>
            {
                admins.ReAdmin(player);
                Assert.That(admins.GetAdminData(player)?.HasFlag(AdminFlags.Adminhelp), Is.True);
            });
            await Client.WaitPost(() => CEntMan.EntityNetManager.SendSystemNetworkMessage(new BwoinkHistoryRequest(player.UserId)));
            await Pair.RunTicksSync(10);
            await Client.WaitAssertion(() =>
            {
                Assert.That(responses, Has.Count.EqualTo(3));
                Assert.That(responses[2].Messages.Count(message => message.Text.StartsWith(marker)), Is.EqualTo(2));
            });

            await Server.WaitPost(() => admins.DeAdmin(player));
            await Client.WaitPost(() => CEntMan.EntityNetManager.SendSystemNetworkMessage(new BwoinkHistoryRequest(foreignUser)));
            await Pair.RunTicksSync(10);
            await Client.WaitAssertion(() => Assert.That(responses, Has.Count.EqualTo(3)));
        }
        finally
        {
            await Client.WaitPost(() => CEntMan.EntityNetManager.ReceivedSystemMessage -= OnResponse);
        }
    }
    // Arcane-End
}
