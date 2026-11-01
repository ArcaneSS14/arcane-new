#nullable enable
using System.Linq;
using Content.Arcane.Client.JoinQueue;
using Content.Arcane.Shared.JoinQueue;
using Content.Goobstation.Common.CCVar;
using Content.IntegrationTests.Fixtures;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using ServerQueue = Content.Arcane.Server.JoinQueue.JoinQueueSystem;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class JoinQueueNetworkTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, DummyTicker = false, Dirty = true };

    public override async Task DoTeardown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != global::NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            // A Connected queue client receives no game states. The base cleanup's PVS sync cannot complete there.
            await Client.WaitPost(() =>
            {
                var net = Client.ResolveDependency<IClientNetManager>();
                if (net.ServerChannel is { IsConnected: true })
                    net.ClientDisconnect("Queue test cleanup");
            });
            await Pair.ReallyBeIdle(5);
            await Client.WaitAssertion(() => Assert.That(Client.Session, Is.Null));
        }
        await base.DoTeardown();
    }

    [Test]
    public async Task QueueCountsOnlyAdmittedPlayersAndScoresNeverGrantAdmission()
    {
        var (_, session) = await ConnectQueued();
        await Client.WaitAssertion(() =>
        {
            Assert.That(Client.ResolveDependency<IStateManager>().CurrentState, Is.TypeOf<QueueState>());
            var gui = Client.ResolveDependency<IUserInterfaceManager>().StateRoot.Children.OfType<QueueGui>().Single();
            Assert.That(gui.FindControl<Label>("QueuePosition").Text, Is.EqualTo("1"));
        });
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientSendMessage(
            new QueueMiniGameScoreMessage { Game = QueueMiniGameKind.Gyruss, Score = int.MaxValue }));
        await Pair.ReallyBeIdle(5);
        await Server.WaitAssertion(() =>
        {
            var queue = SEntMan.System<ServerQueue>();
            Assert.That(queue.PlayerInQueueCount, Is.EqualTo(1));
            Assert.That(queue.ActualPlayersCount, Is.Zero);
            Assert.That(SEntMan.System<ServerGameTicker>().GetAdmittedPlayerCount(), Is.Zero);
            Assert.That(queue.BuildScores().Single().Score, Is.EqualTo(5000));
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Connected));
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.SoftMaxPlayers, 1);
        });
        await WaitServer(() => session.Status == SessionStatus.InGame);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<ServerQueue>().PlayerInQueueCount, Is.Zero);
            Assert.That(SEntMan.System<ServerQueue>().ActualPlayersCount, Is.EqualTo(1));
        });
        await Pair.ReallyBeIdle(5);
        await Client.WaitAssertion(() => Assert.That(Client.ResolveDependency<IStateManager>().CurrentState, Is.Not.TypeOf<QueueState>()));
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientSendMessage(
            new QueueMiniGameScoreMessage { Game = QueueMiniGameKind.Gyruss, Score = 1234 }));
        await Pair.ReallyBeIdle(5);
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<ServerQueue>().BuildScores(), Is.Empty));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RoundRestartAndReadyCommandsCannotAdmitAWaitingConnection(bool lobby)
    {
        var (_, session) = await ConnectQueued();
        await Server.WaitPost(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.GameLobbyEnabled, lobby);
            SEntMan.System<ServerGameTicker>().RestartRound();
        });
        await Pair.ReallyBeIdle(20);
        await Server.WaitAssertion(() =>
        {
            var ticker = SEntMan.System<ServerGameTicker>();
            ticker.ToggleReady(session, true);
            Assert.That(ticker.PlayerGameStatuses.ContainsKey(session.UserId), Is.False);
            Assert.That(ticker.ReadyPlayerCount(), Is.Zero);
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Connected));
            Assert.That(session.AttachedEntity, Is.Null);
            Assert.That(SEntMan.System<ServerQueue>().PlayerInQueueCount, Is.EqualTo(1));
        });
        await Client.WaitAssertion(() => Assert.That(Client.ResolveDependency<IStateManager>().CurrentState, Is.TypeOf<QueueState>()));
    }

    [Test]
    public async Task ReconnectHonorsTheExistingTemporaryBypassAndCleansTheOldProfile()
    {
        var (name, session) = await ConnectQueued();
        await Server.WaitPost(() => Server.ResolveDependency<IConnectionManager>()
            .AddTemporaryConnectBypass(session.UserId, TimeSpan.FromMinutes(1)));
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientDisconnect("Queue reconnect test"));
        await Pair.ReallyBeIdle(20);
        await Server.WaitAssertion(() => Assert.Throws<KeyNotFoundException>(() => Server.ResolveDependency<UserDbDataManager>().GetLoadTask(session)));
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, name));
        await WaitServer(() => Server.ResolveDependency<IPlayerManager>().Sessions.Any(s => s.Status == SessionStatus.InGame));
        await Server.WaitAssertion(() =>
        {
            var current = Server.ResolveDependency<IPlayerManager>().Sessions.Single();
            Assert.That(current, Is.Not.SameAs(session));
            Assert.That(current.UserId, Is.EqualTo(session.UserId));
            Assert.That(SEntMan.System<ServerQueue>().PlayerInQueueCount, Is.Zero);
            Assert.That(SEntMan.System<ServerQueue>().ActualPlayersCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DisablingTheQueueDisconnectsWaitingPlayersAndCancelsTheirLoadedData()
    {
        var (_, session) = await ConnectQueued();
        await Server.WaitPost(() => Server.ResolveDependency<IConfigurationManager>().SetCVar(GoobCVars.QueueEnabled, false));
        await WaitServer(() => session.Status == SessionStatus.Disconnected);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<ServerQueue>().PlayerInQueueCount, Is.Zero);
            Assert.Throws<KeyNotFoundException>(() => Server.ResolveDependency<UserDbDataManager>().GetLoadTask(session));
        });
    }

    private async Task<(string Name, ICommonSession Session)> ConnectQueued()
    {
        await Server.WaitPost(() =>
        {
            var cfg = Server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(CCVars.ConsoleLoginLocal, false);
            cfg.SetCVar(CCVars.AdminBypassMaxPlayers, false);
            cfg.SetCVar(CCVars.SoftMaxPlayers, 0);
            cfg.SetCVar(GoobCVars.QueueEnabled, true);
        });
        var name = "queued_" + Guid.NewGuid().ToString("N")[..12];
        Client.SetConnectTarget(Server);
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, name));
        ICommonSession? session = null;
        await WaitServer(() =>
        {
            session = Server.ResolveDependency<IPlayerManager>().Sessions.FirstOrDefault();
            return session != null && session.Status == SessionStatus.Connected
                && Server.ResolveDependency<UserDbDataManager>().IsLoadComplete(session)
                && SEntMan.System<ServerQueue>().PlayerInQueueCount == 1;
        });
        await Pair.ReallyBeIdle(5);
        return (name, session!);
    }

    private async Task WaitServer(Func<bool> predicate)
    {
        for (var i = 0; i < 200; i++)
        {
            var done = false;
            await Pair.ReallyBeIdle(2);
            await Server.WaitPost(() => done = predicate());
            if (done)
                return;
        }
        Assert.Fail("The queue lifecycle transition did not complete.");
    }
}
