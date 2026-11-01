#nullable enable
using System.Linq;
using Content.Arcane.Client.Actions;
using Content.Arcane.Common.Actions;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Players;
using Content.Shared.Actions.Components;
using Content.Shared.Input;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Client.Input;
using Content.Arcane.Shared.Actions;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ActionBarPortTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task PinRoundTripsAndKeepsTheFlashlightSlotAcrossItemRemoval()
    {
        var flashlight = await PlaceInHands("FlashlightLantern");
        await Pair.RunUntilSynced();
        var index = -1;
        PinnedActionButton button = null!;
        await Client.WaitAssertion(() =>
        {
            var bar = Client.ResolveDependency<IUserInterfaceManager>().GetActiveUIWidgetOrNull<ActionsBar>()!;
            Assert.That(bar.ActionsContainer, Is.TypeOf<ArcaneActionButtonContainer>());
            var buttons = bar.ActionsContainer.GetButtons().ToArray();
            index = Array.FindIndex(buttons, b => b.Action?.Comp.Container == ToClient(flashlight));
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            button = (PinnedActionButton) buttons[index];
        });
        await ClickControl(button, ContentKeyFunctions.ToggleActionPin);
        await FlushPinRequest();
        await Server.WaitAssertion(() => Assert.That(ServerSession!.ContentData()!.PinnedActions.ContainsKey("ActionToggleLight"), Is.True));
        await Client.WaitAssertion(() =>
        {
            var bar = Client.ResolveDependency<IUserInterfaceManager>().GetActiveUIWidgetOrNull<ActionsBar>()!;
            Assert.That(bar.ActionsContainer[index].Action, Is.Not.Null);
            Assert.That(bar.ActionsContainer[index].Modulate, Is.EqualTo(Color.White));
        });
        await Drop();
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var bar = Client.ResolveDependency<IUserInterfaceManager>().GetActiveUIWidgetOrNull<ActionsBar>()!;
            Assert.That(bar.ActionsContainer[index].Modulate, Is.EqualTo(Color.Gray));
        });
        await Server.WaitPost(() => Assert.That(HandSys.TryPickupAnyHand(SPlayer, ToServer(flashlight)), Is.True));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var bar = Client.ResolveDependency<IUserInterfaceManager>().GetActiveUIWidgetOrNull<ActionsBar>()!;
            Assert.That(bar.ActionsContainer[index].Action, Is.Not.Null);
            Assert.That(bar.ActionsContainer[index].Modulate, Is.EqualTo(Color.White));
        });

        await Client.WaitPost(() => CEntMan.System<PinnedActionsSystem>().SetPinned("MobHuman", true));
        await FlushPinRequest();
        await Server.WaitAssertion(() => Assert.That(ServerSession!.ContentData()!.PinnedActions.Count, Is.EqualTo(1)));
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<PinnedActionsSystem>().IsPinned("MobHuman"), Is.False));
        await Client.WaitPost(() => CEntMan.System<PinnedActionsSystem>().SetPinned("P02MissingActionPrototype", true));
        await FlushPinRequest();
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<PinnedActionsSystem>().IsPinned("P02MissingActionPrototype"), Is.False));
        await Client.WaitPost(() => CEntMan.System<PinnedActionsSystem>().SetPinned("ActionToggleLight", false));
        await FlushPinRequest();
        await Server.WaitAssertion(() => Assert.That(ServerSession!.ContentData()!.PinnedActions, Is.Empty));
    }

    [Test]
    public async Task ServerCapsPinsAndReturnsItsAuthoritativeSet()
    {
        EntProtoId[] candidates = [];
        EntProtoId[] actions = [];
        await Client.WaitPost(() =>
        {
            candidates = Client.ResolveDependency<IPrototypeManager>().EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract
                    && p.TryGetComponent<ActionComponent>(out _, CEntMan.ComponentFactory)
                    && p.TryGetComponent<SpriteComponent>(out _, CEntMan.ComponentFactory))
                .Select(p => new EntProtoId(p.ID)).ToArray();
        });
        await Server.WaitPost(() =>
        {
            actions = Server.ResolveDependency<IPrototypeManager>().EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && candidates.Contains(p.ID)
                    && p.TryGetComponent<ActionComponent>(out _, SEntMan.ComponentFactory))
                .OrderBy(p => p.ID, StringComparer.Ordinal).Take(33).Select(p => new EntProtoId(p.ID)).ToArray();
        });
        Assert.That(actions, Has.Length.EqualTo(33));
        await Client.WaitPost(() =>
        {
            foreach (var action in actions)
                CEntMan.System<PinnedActionsSystem>().SetPinned(action, true);
        });
        await FlushPinRequest();
        EntProtoId[] accepted = [];
        await Server.WaitAssertion(() =>
        {
            accepted = ServerSession!.ContentData()!.PinnedActions.Keys.ToArray();
            Assert.That(accepted, Has.Length.EqualTo(32));
        });
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.System<PinnedActionsSystem>().GetPinned(), Is.EquivalentTo(accepted));
        });
        await Client.WaitPost(() =>
        {
            foreach (var action in CEntMan.System<PinnedActionsSystem>().GetPinned())
                CEntMan.System<PinnedActionsSystem>().SetPinned(action, false);
        });
        await FlushPinRequest();
        await Server.WaitAssertion(() => Assert.That(ServerSession!.ContentData()!.PinnedActions, Is.Empty));
    }

    [TestCase("Default")]
    [TestCase("Separated")]
    public async Task PlacementClampsToTheViewportAndKeepsTheDetachedButtonUsable(string screen)
    {
        var config = Client.ResolveDependency<IConfigurationManager>();
        var ui = Client.ResolveDependency<IUserInterfaceManager>();
        await Client.WaitPost(() => config.SetCVar(CCVars.UILayout, screen));
        await Pair.ReallyBeIdle(2);
        await PlaceInHands("FlashlightLantern");
        await Pair.RunUntilSynced();
        var layout = ui.GetUIController<ActionsBarLayoutUIController>();
        EntityUid? action = null;
        string? hotkey = null;
        try
        {
            await Client.WaitPost(() =>
            {
                config.SetCVar(ActionBarCVars.FreePlacement, true);
                var bar = ui.GetActiveUIWidgetOrNull<ActionsBar>()!;
                action = bar.ActionsContainer[0].Action;
                hotkey = bar.ActionsContainer[0].Label.Text;
                Assert.That(bar.Parent!.Size.X, Is.GreaterThan(0));
                Assert.That(bar.Parent.Size.Y, Is.GreaterThan(0));
                layout.SetBarPosition(new(100000, 100000));
            });
            await Pair.ReallyBeIdle(2);
            await Client.WaitAssertion(() =>
            {
                var bar = ui.GetActiveUIWidgetOrNull<ActionsBar>()!;
                Assert.That(bar.GlobalPosition.X + bar.Size.X, Is.EqualTo(bar.Parent!.GlobalPosition.X + bar.Parent.Size.X).Within(1));
                Assert.That(bar.GlobalPosition.Y + bar.Size.Y, Is.EqualTo(bar.Parent.GlobalPosition.Y + bar.Parent.Size.Y).Within(1));
                foreach (var slot in bar.ActionsContainer.GetButtons())
                {
                    Assert.That(slot.GlobalPosition.X + slot.Size.X, Is.LessThanOrEqualTo(bar.Parent.GlobalPosition.X + bar.Parent.Size.X + 1));
                    Assert.That(slot.GlobalPosition.Y + slot.Size.Y, Is.LessThanOrEqualTo(bar.Parent.GlobalPosition.Y + bar.Parent.Size.Y + 1));
                }
                layout.SetBarPosition(new(10, 10));
                layout.SetSlotPosition(0, new(100000, 100000));
            });
            await Pair.ReallyBeIdle(10);
            await Client.WaitAssertion(() =>
            {
                var bar = ui.GetActiveUIWidgetOrNull<ActionsBar>()!;
                var container = (ArcaneActionButtonContainer) bar.ActionsContainer;
                var button = container[0];
                var space = container.PositionSpace!;
                Assert.That(button.Size.X, Is.GreaterThan(0));
                Assert.That(button.GlobalPosition.X + button.Size.X, Is.EqualTo(space.GlobalPosition.X + space.Size.X).Within(1));
                Assert.That(button.GlobalPosition.Y + button.Size.Y, Is.EqualTo(space.GlobalPosition.Y + space.Size.Y).Within(1));
                Assert.That(button.Action?.Owner, Is.EqualTo(action));
                Assert.That(button.Label.Text, Is.EqualTo(hotkey));
                var point = new ScreenCoordinates(button.GlobalPixelPosition + button.PixelSize / 2, button.Window?.Id ?? default);
                var hovered = ui.MouseGetControl(point);
                while (hovered != null && hovered != button)
                    hovered = hovered.Parent;
                Assert.That(hovered, Is.SameAs(button), "A detached button must remain reachable outside the bar's flow rectangle.");
            });
        }
        finally
        {
            await Client.WaitPost(layout.Reset);
        }
    }

    private async Task FlushPinRequest()
    {
        // PVS catch-up alone can advance no ticks. Flush client -> server -> client before checking a reply.
        await Pair.ReallyBeIdle(2);
        await Pair.RunUntilSynced();
    }

    [Test]
    public async Task ClearingAPinnedSlotUnpinsItAndKeepsTheGrantedActionAvailable()
    {
        var flashlight = await PlaceInHands("FlashlightLantern");
        await Pair.RunUntilSynced();
        await Client.WaitPost(() => CEntMan.System<PinnedActionsSystem>().SetPinned("ActionToggleLight", true));
        await FlushPinRequest();
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var bar = ui.GetActiveUIWidgetOrNull<ActionsBar>()!;
            var slots = bar.ActionsContainer.GetButtons().Select(button => button.Action?.Owner).ToList();
            var index = slots.FindIndex(action => action != null && CEntMan.GetComponent<ActionComponent>(action.Value).Container == ToClient(flashlight));
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            var granted = slots[index]!.Value;
            var config = Client.ResolveDependency<IConfigurationManager>();
            config.SetCVar(ActionBarCVars.FreePlacement, true);
            var input = Client.ResolveDependency<IInputManager>();
            input.KeyDown(new KeyEventArgs(Keyboard.Key.Shift, false, false, false, true, false, 0));
            try
            {
                var customization = ui.GetUIController<ActionBarUIController>();
                Assert.That(customization.HandleDrop(bar.ActionsContainer[index], null, System.Numerics.Vector2.Zero, slots), Is.True);
                Assert.That(slots[index], Is.Null);
                customization.UpdateSlots(slots);
                Assert.That(slots.Contains(granted), Is.False);
                Assert.That(customization.IsUnavailable(granted), Is.False, "Removing a hotbar slot must not revoke the item's action.");
                Assert.That(CEntMan.System<PinnedActionsSystem>().IsPinned("ActionToggleLight"), Is.False);
            }
            finally
            {
                input.KeyUp(new KeyEventArgs(Keyboard.Key.Shift, false, false, false, false, false, 0));
            }
        });
        await FlushPinRequest();
        await Server.WaitAssertion(() => Assert.That(ServerSession!.ContentData()!.PinnedActions, Is.Empty));
    }

    [Test]
    public async Task ServerRejectsUnboundedPinPositions()
    {
        await Client.WaitPost(() => CEntMan.System<PinnedActionsSystem>().SetPinned("ActionToggleLight", true, 3));
        await FlushPinRequest();
        await Client.WaitPost(() =>
        {
            CEntMan.EntityNetManager.SendSystemNetworkMessage(new SetPinnedActionMessage("ActionToggleLight", true, int.MaxValue));
            CEntMan.EntityNetManager.SendSystemNetworkMessage(new SetPinnedActionMessage("ActionToggleLight", true, -1));
        });
        await FlushPinRequest();
        await Server.WaitAssertion(() => Assert.That(ServerSession!.ContentData()!.PinnedActions["ActionToggleLight"], Is.EqualTo(3)));
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<PinnedActionsSystem>().GetPosition("ActionToggleLight"), Is.EqualTo(3)));
        await Client.WaitPost(() => CEntMan.System<PinnedActionsSystem>().SetPinned("ActionToggleLight", false));
        await FlushPinRequest();
    }
}
