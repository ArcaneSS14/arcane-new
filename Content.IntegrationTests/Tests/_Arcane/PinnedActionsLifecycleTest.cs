#nullable enable
using Content.Arcane.Client.Actions;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PinnedActionsLifecycleTest : InteractionTest
{
    private const int PinnedSlot = 7;
    private string? _hotkey;
    protected override string PlayerPrototype => "MobHuman";
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false, Dirty = true };

    [Test]
    public async Task PinsSurviveBodyChangesAndReconnectWithoutGrantingActions()
    {
        EntityUid mind = default;
        await Server.WaitPost(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            mind = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mind, SPlayer);
        });
        await PlaceInHands("FlashlightLantern");
        await Pair.RunUntilSynced();
        await Client.WaitPost(() => CEntMan.System<PinnedActionsSystem>().SetPinned("ActionToggleLight", true, PinnedSlot));
        await Pair.ReallyBeIdle(2);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var bar = Client.ResolveDependency<IUserInterfaceManager>().GetActiveUIWidgetOrNull<ActionsBar>()!;
            Assert.That(bar.ActionsContainer[PinnedSlot].Action, Is.Not.Null);
            _hotkey = bar.ActionsContainer[PinnedSlot].Label.Text;
        });
        await Server.WaitAssertion(() =>
        {
            Assert.That(ServerSession!.ContentData()!.PinnedActions.ContainsKey("ActionToggleLight"), Is.True);
            Assert.That(ServerSession!.ContentData()!.PinnedActions["ActionToggleLight"], Is.EqualTo(PinnedSlot));
            var body = SEntMan.SpawnEntity("MobHuman", SEntMan.GetCoordinates(PlayerCoords));
            SEntMan.System<SharedMindSystem>().TransferTo(mind, body);
        });
        await Pair.ReallyBeIdle(2);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(AssertPlaceholder);

        var name = ServerSession!.Name;
        var net = Client.ResolveDependency<IClientNetManager>();
        await Client.WaitPost(() => net.ClientDisconnect("Pinned actions integration test"));
        await Pair.RunTicksSync(20);
        Client.SetConnectTarget(Server);
        await Client.WaitPost(() => net.ClientConnect(null!, 0, name));
        await Pair.RunTicksSync(20);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(AssertPlaceholder);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.PlayerMan.PlayerCount, Is.EqualTo(1));
            Assert.That(ServerSession!.ContentData()!.PinnedActions.ContainsKey("ActionToggleLight"), Is.True);
            SEntMan.System<SharedMindSystem>().WipeMind(mind);
        });
    }

    private void AssertPlaceholder()
    {
        Assert.That(CEntMan.System<PinnedActionsSystem>().IsPinned("ActionToggleLight"), Is.True);
        var ui = Client.ResolveDependency<IUserInterfaceManager>();
        var customization = ui.GetUIController<ActionBarUIController>();
        var bar = ui.GetActiveUIWidgetOrNull<ActionsBar>();
        Assert.That(bar, Is.Not.Null);
        var pinned = bar!.ActionsContainer[PinnedSlot];
        Assert.That(pinned.Action, Is.Null);
        Assert.That(pinned.Modulate, Is.EqualTo(Color.Gray));
        Assert.That(pinned.Label.Visible, Is.True);
        Assert.That(pinned.Label.Text, Is.EqualTo(_hotkey));
        Assert.That(CEntMan.System<PinnedActionsSystem>().GetPosition("ActionToggleLight"), Is.EqualTo(PinnedSlot));
        foreach (var button in bar!.ActionsContainer.GetButtons())
        {
            // A placeholder carries a prototype and a key label, but cannot grant an action to the new body.
            if (button.Action is { } action)
                Assert.That(customization.GetPrototype(action.Owner)?.Id, Is.Not.EqualTo("ActionToggleLight"));
        }
    }
}
