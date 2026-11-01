#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.SSDIndicator;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class SSDTimerTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false, Dirty = true };

    [Test]
    public async Task OnlyNetworkDisconnectionStartsTheTimerAndReconnectClearsIt()
    {
        var config = Server.ResolveDependency<IConfigurationManager>();
        var originalSleep = false;
        EntityUid mind = default;
        EntityUid otherBody = default;
        var name = ServerSession!.Name;
        await Server.WaitPost(() =>
        {
            originalSleep = config.GetCVar(CCVars.ICSSDSleep);
            config.SetCVar(CCVars.ICSSDSleep, false);
            var minds = SEntMan.System<SharedMindSystem>();
            mind = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mind, SPlayer);
            Assert.That(HasTimer(SPlayer), Is.False);
            var ghost = SEntMan.SpawnEntity("MobObserver", SEntMan.GetCoordinates(PlayerCoords));
            minds.Visit(mind, ghost);
            Assert.That(SEntMan.GetComponent<SSDIndicatorComponent>(SPlayer).IsSSD, Is.True);
            Assert.That(HasTimer(SPlayer), Is.False, "A temporary ghost visit is not a network disconnect.");
        });

        var net = Client.ResolveDependency<IClientNetManager>();
        await Client.WaitPost(() => net.ClientDisconnect("SSD timer integration test"));
        await Pair.RunTicksSync(20);
        await Server.WaitAssertion(() =>
        {
            Assert.That(HasTimer(SPlayer), Is.True, "The timer must work with IC SSD sleeping disabled.");
            Assert.That(HasTimer(SPlayer, false), Is.False);
            otherBody = SEntMan.SpawnEntity("MobHuman", SEntMan.GetCoordinates(PlayerCoords));
            var minds = SEntMan.System<SharedMindSystem>();
            minds.UnVisit(mind);
            minds.TransferTo(mind, otherBody);
            Assert.That(HasTimer(SPlayer), Is.False, "The old body no longer belongs to the disconnected mind.");
            Assert.That(HasTimer(otherBody), Is.True);
        });

        Client.SetConnectTarget(Server);
        await Client.WaitPost(() => net.ClientConnect(null!, 0, name));
        await Pair.RunTicksSync(20);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.PlayerMan.PlayerCount, Is.EqualTo(1));
            Assert.That(HasTimer(otherBody), Is.False);
            config.SetCVar(CCVars.ICSSDSleep, originalSleep);
            SEntMan.System<SharedMindSystem>().WipeMind(mind);
        });
    }

    private bool HasTimer(EntityUid body, bool details = true)
    {
        var examine = new ExaminedEvent(new FormattedMessage(), body, body, details, false);
        SEntMan.EventBus.RaiseLocalEvent(body, examine);
        return examine.GetTotalMessage().ToString().Contains("Disconnected for");
    }
}
