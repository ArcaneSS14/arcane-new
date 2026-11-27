// Arcane-Start: supported borgs can rest and restore their normal appearance
using Content.Arcane.Shared.SiliconStanding;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class SiliconRestRuntimeTest : InteractionTest
{
    [Test]
    public async Task SelectingSupportedSubtypeAddsRestAction()
    {
        await Server.WaitAssertion(() =>
        {
            var borg = SEntMan.SpawnAttachedTo("BorgChassisSelectable", MapData.GridCoords);
            var standing = SEntMan.GetComponent<SiliconStandingComponent>(borg);
            Assert.That(standing.Action, Is.Null);
            var select = new BorgSelectTypeMessage("generic", "generic_dullahan")
            {
                Actor = SPlayer,
                UiKey = BorgSwitchableTypeUiKey.SelectBorgType,
            };
            SEntMan.EventBus.RaiseLocalEvent(borg, select);
            Assert.That(standing.Action, Is.Not.Null);
        });
    }

    [TestCase("BorgChassisGenericDullahan", "robot-rest", "robot", "robot_e_r")]
    [TestCase("BorgChassisMiningMeka", "miner-rest", "miner", "miner_e")]
    public async Task RestActionBlocksMovementAndUpdatesSprite(string prototype, string restingState, string standingState, string noMindState)
    {
        EntityUid borg = default;
        EntityUid unsupported = default;
        NetEntity netBorg = default;
        var originalCanMove = false;
        await Server.WaitAssertion(() =>
        {
            borg = SEntMan.SpawnAttachedTo(prototype, MapData.GridCoords);
            unsupported = SEntMan.SpawnAttachedTo("BorgChassisGenericBugborg", MapData.GridCoords);
            netBorg = SEntMan.GetNetEntity(borg);
            Assert.That(SEntMan.GetComponent<SiliconStandingComponent>(borg).Action, Is.Not.Null);
            Assert.That(SEntMan.GetComponent<SiliconStandingComponent>(unsupported).Action, Is.Null);
            Assert.That(SEntMan.HasComponent<SpriteMovementComponent>(borg), Is.False);
            originalCanMove = Server.System<ActionBlockerSystem>().CanMove(borg);
            var actions = Server.System<SharedActionsSystem>();
            var action = SEntMan.GetComponent<SiliconStandingComponent>(borg).Action!.Value;
            Assert.That(actions.PerformAction(borg, actions.GetAction(action)!.Value, predicted: false), Is.True);
            Assert.That(SEntMan.HasComponent<SiliconRestingComponent>(borg), Is.True);
            Assert.That(Server.System<ActionBlockerSystem>().CanMove(borg), Is.False);
        });

        await Pair.RunTicksSync(20);
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(netBorg);
            var sprite = CEntMan.GetComponent<SpriteComponent>(clientBorg);
            Assert.That(CEntMan.GetComponent<BorgChassisComponent>(clientBorg).NoMindState, Is.EqualTo(noMindState));
            var sprites = Client.System<SpriteSystem>();
            Assert.That(sprites.LayerMapTryGet((clientBorg, sprite), BorgVisualLayers.Body, out var layer, false), Is.True);
            Assert.That(sprites.LayerGetRsiState((clientBorg, sprite), layer).Name, Is.EqualTo(restingState));
        });

        await Server.WaitAssertion(() =>
        {
            var actions = Server.System<SharedActionsSystem>();
            var action = SEntMan.GetComponent<SiliconStandingComponent>(borg).Action!.Value;
            Assert.That(actions.PerformAction(borg, actions.GetAction(action)!.Value, predicted: false), Is.True);
            Assert.That(SEntMan.HasComponent<SiliconRestingComponent>(borg), Is.False);
            Assert.That(Server.System<ActionBlockerSystem>().CanMove(borg), Is.EqualTo(originalCanMove));
        });
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(netBorg);
            var sprite = CEntMan.GetComponent<SpriteComponent>(clientBorg);
            var sprites = Client.System<SpriteSystem>();
            Assert.That(sprites.LayerMapTryGet((clientBorg, sprite), BorgVisualLayers.Body, out var layer, false), Is.True);
            Assert.That(sprites.LayerGetRsiState((clientBorg, sprite), layer).Name, Is.EqualTo(standingState));
        });
    }
}
// Arcane-End
