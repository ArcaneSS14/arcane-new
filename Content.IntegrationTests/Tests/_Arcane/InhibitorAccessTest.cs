// Arcane-Start: access to a worn inhibitor still requires an able, unobstructed user
using System.Linq;
using Content.Arcane.Server.AggressionInhibitor.Systems;
using Content.Arcane.Shared.AggressionInhibitor.Components;
using Content.Arcane.Shared.WashingMachine;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class InhibitorAccessTest : GameTest
{
    [Test]
    public async Task WornInhibitorRequiresAccessAndValidId()
    {
        var map = await Pair.CreateTestMap();
        EntityUid wearer = default;
        EntityUid actor = default;
        EntityUid inhibitor = default;
        EntityUid wall = default;
        await Server.WaitAssertion(() =>
        {
            Server.System<SharedMapSystem>().SetTile(map.Grid.Owner, map.Grid.Comp, map.GridCoords.Offset(new Vector2(1, 0)), map.Tile.Tile);
            Server.System<SharedMapSystem>().SetTile(map.Grid.Owner, map.Grid.Comp, map.GridCoords.Offset(new Vector2(2, 0)), map.Tile.Tile);
            wearer = SSpawnAtPosition("MobHuman", map.GridCoords);
            actor = SSpawnAtPosition("MobHuman", map.GridCoords.Offset(new Vector2(0.5f, 0)));
            inhibitor = SSpawnAtPosition("ClothingAggressionInhibitor", map.GridCoords);
            Assert.That(Server.System<InventorySystem>().TryEquip(wearer, inhibitor, "neck", force: true), Is.True);
            var component = SEntMan.GetComponent<AggressionInhibitorComponent>(inhibitor);
            var system = Server.System<AggressionInhibitorSystem>();
            var hands = Server.System<SharedHandsSystem>();
            var wrongCard = SSpawnAtPosition("PassengerIDCard", map.GridCoords);
            Assert.That(hands.TryPickupAnyHand(actor, wrongCard, checkActionBlocker: false), Is.True);
            system.ToggleLock(inhibitor, component, actor);
            Assert.That(component.IsLocked, Is.False);
            Assert.That(hands.TryDrop(actor, wrongCard), Is.True);
            var validCard = SSpawnAtPosition("CaptainIDCard", map.GridCoords);
            Assert.That(hands.TryPickupAnyHand(actor, validCard, checkActionBlocker: false), Is.True);

            var verbs = Server.System<SharedVerbSystem>();
            var lockText = Loc.GetString("stabikor-verb-lock");
            Assert.That(verbs.GetLocalVerbs(inhibitor, actor, typeof(ActivationVerb)).Any(v => v.Text == lockText), Is.True);
            var transforms = Server.System<SharedTransformSystem>();
            transforms.SetCoordinates(actor, map.GridCoords.Offset(new Vector2(4, 0)));
            Assert.That(verbs.GetLocalVerbs(inhibitor, actor, typeof(ActivationVerb)).Any(v => v.Text == lockText), Is.False);
            system.ToggleLock(inhibitor, component, actor);
            Assert.That(component.IsLocked, Is.False);

            transforms.SetCoordinates(actor, map.GridCoords.Offset(new Vector2(0.5f, 0)));
            SEntMan.EnsureComponent<WashingMachineStuckComponent>(actor);
            system.ToggleLock(inhibitor, component, actor);
            Assert.That(component.IsLocked, Is.False);
            SEntMan.RemoveComponent<WashingMachineStuckComponent>(actor);

            transforms.SetCoordinates(actor, map.GridCoords.Offset(new Vector2(1.8f, 0)));
            wall = SSpawnAtPosition("WallSolid", map.GridCoords.Offset(new Vector2(1, 0)));
        });

        await Pair.RunTicksSync(2);
        await Server.WaitAssertion(() =>
        {
            var component = SEntMan.GetComponent<AggressionInhibitorComponent>(inhibitor);
            var system = Server.System<AggressionInhibitorSystem>();
            var transforms = Server.System<SharedTransformSystem>();
            transforms.SetCoordinates(wearer, map.GridCoords.Offset(new Vector2(0.65f, 0.5f)));
            transforms.SetCoordinates(actor, map.GridCoords.Offset(new Vector2(2.35f, 0.5f)));
            Assert.That(Server.System<SharedInteractionSystem>().InRangeAndAccessible(actor, wearer, 2f), Is.False,
                $"actor={SEntMan.GetComponent<TransformComponent>(actor).Coordinates}; wearer={SEntMan.GetComponent<TransformComponent>(wearer).Coordinates}; wall={SEntMan.GetComponent<TransformComponent>(wall).Coordinates}");
            system.ToggleLock(inhibitor, component, actor);
            Assert.That(component.IsLocked, Is.False);
            SEntMan.DeleteEntity(wall);
            transforms.SetCoordinates(actor, map.GridCoords.Offset(new Vector2(0.35f, 0.5f)));
            system.ToggleLock(inhibitor, component, actor);
            Assert.That(component.IsLocked, Is.True);
            system.ToggleLock(inhibitor, component, actor);
            Assert.That(component.IsLocked, Is.False);
        });
    }
}
// Arcane-End
