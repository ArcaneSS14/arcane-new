// Arcane-Start: forced ejection must keep valid coordinates without a free tile
using System.Linq;
using Content.Arcane.Shared.WashingMachine;
using Content.IntegrationTests.Fixtures;
using Content.Server.Power.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class WashingMachineEjectTest : GameTest
{
    [Test]
    public async Task StartRequiresAccessibleMachine()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var machine = SSpawnAtPosition("WashingMachine", map.GridCoords);
            var user = SSpawnAtPosition("MobHuman", map.GridCoords.Offset(new Vector2(1, 0)));
            Server.System<EntityStorageSystem>().CloseStorage(machine);
            SEntMan.GetComponent<ApcPowerReceiverComponent>(machine).Powered = true;
            var component = SEntMan.GetComponent<WashingMachineComponent>(machine);
            var verbs = Server.System<SharedVerbSystem>();
            var startName = Loc.GetString("washing-machine-start");
            var near = verbs.GetLocalVerbs(machine, user, typeof(AlternativeVerb)).Single(v => v.Text == startName);
            Server.System<SharedTransformSystem>().SetCoordinates(user, map.GridCoords.Offset(new Vector2(8, 0)));
            Assert.That(verbs.GetLocalVerbs(machine, user, typeof(AlternativeVerb)).Any(v => v.Text == startName), Is.False);
            near.Act!();
            Assert.That(component.WashingMachineState, Is.EqualTo(WashingMachineState.Idle));
            Server.System<SharedTransformSystem>().SetCoordinates(user, map.GridCoords.Offset(new Vector2(1, 0)));
            near.Act!();
            Assert.That(component.WashingMachineState, Is.EqualTo(WashingMachineState.Washing));
        });
    }

    [Test]
    public async Task ExtractRequiresAccessibleMachineAndUsableActor()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var machine = SSpawnAtPosition("WashingMachine", map.GridCoords);
            var user = SSpawnAtPosition("MobHuman", map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var item = SSpawnAtPosition("Pen", map.GridCoords);
            var storage = SEntMan.GetComponent<EntityStorageComponent>(machine);
            Assert.That(Server.System<SharedContainerSystem>().Insert(item, storage.Contents), Is.True);
            var verbs = Server.System<SharedVerbSystem>();
            var extractName = Loc.GetString("washing-machine-extract-verb");
            var near = verbs.GetLocalVerbs(machine, user, typeof(ActivationVerb)).Single(v => v.Text == extractName);
            Assert.That(verbs.GetLocalVerbs(machine, user, typeof(AlternativeVerb)).Any(v => v.Text == extractName), Is.True);

            var transform = Server.System<SharedTransformSystem>();
            transform.SetCoordinates(user, map.GridCoords.Offset(new Vector2(8, 0)));
            Assert.That(verbs.GetLocalVerbs(machine, user, typeof(ActivationVerb)).Any(v => v.Text == extractName), Is.False);
            Assert.That(verbs.GetLocalVerbs(machine, user, typeof(AlternativeVerb)).Any(v => v.Text == extractName), Is.False);
            near.Act!();
            Assert.That(storage.Contents.Contains(item), Is.True, "A stale verb must recheck access.");

            transform.SetCoordinates(user, map.GridCoords.Offset(new Vector2(0.5f, 0)));
            SEntMan.EnsureComponent<WashingMachineStuckComponent>(user);
            Assert.That(verbs.GetLocalVerbs(machine, user, typeof(ActivationVerb)).Any(v => v.Text == extractName), Is.False);
            near.Act!();
            Assert.That(storage.Contents.Contains(item), Is.True, "A blocked actor cannot extract contents.");
            SEntMan.RemoveComponent<WashingMachineStuckComponent>(user);
            near.Act!();
            Assert.That(storage.Contents.Contains(item), Is.False);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task BrokenMachineEjectsOnTheSameMap(bool hasFreeTile)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var coordinates = hasFreeTile
                ? map.GridCoords
                : new EntityCoordinates(map.MapUid, new Vector2(20, 20));
            if (hasFreeTile)
                Server.System<SharedMapSystem>().SetTile(map.Grid.Owner, map.Grid.Comp, map.GridCoords.Offset(new Vector2(1, 0)), map.Tile.Tile);

            var machine = SSpawnAtPosition("WashingMachine", coordinates);
            var person = SSpawnAtPosition("MobHuman", coordinates);
            SEntMan.EnsureComponent<WashingMachineStuckComponent>(person).Machine = machine;
            var originalMap = SEntMan.GetComponent<TransformComponent>(machine).MapID;
            SEntMan.EventBus.RaiseLocalEvent(machine, new BreakageEventArgs());
            var result = SEntMan.GetComponent<TransformComponent>(person);
            Assert.That(result.MapID, Is.EqualTo(originalMap));
            Assert.That(result.ParentUid.IsValid(), Is.True);
            Assert.That(SEntMan.HasComponent<WashingMachineStuckComponent>(person), Is.False);
            if (hasFreeTile)
                Assert.That(result.Coordinates.Position, Is.Not.EqualTo(coordinates.Position));
            else
                Assert.That(result.Coordinates, Is.EqualTo(coordinates));
        });
    }
}
// Arcane-End
