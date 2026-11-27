using System;
using System.Collections.Generic;
using System.Linq;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.InfinityDorm;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.EntitySystems; // Arcane
using Content.Shared.Storage; // Arcane
using Content.Shared.Storage.EntitySystems; // Arcane
using Content.Shared.Tag; // Arcane
using Robust.Shared.Containers; // Arcane

namespace Content.IntegrationTests.Tests._Arcane.InfinityDorm;

[TestFixture]
public sealed class InfinityDormMapsTest : GameTest
{
    private static readonly ProtoId<TagPrototype> DormItem = "InfiniteDormItem"; // Arcane
    private static readonly ProtoId<TagPrototype> OriginalItem = "InfiniteDormItemBlock"; // Arcane
    // Arcane-Start: shared visits and belongings survive in dorms
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task MultipleVisitorsAndDroppedBelongingsSurvive()
    {
        await OverrideCVar(Side.Server, ArcaneCVars.InfinityDormsEnabled, true);
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var maps = entities.System<SharedMapSystem>();
        var transforms = entities.System<SharedTransformSystem>();
        var hands = entities.System<SharedHandsSystem>();
        var originalMap = MapId.Nullspace;
        var dormMap = MapId.Nullspace;
        EntityUid pen = default;
        EntityUid visitor = default;
        EntityUid secondVisitor = default;
        EntityUid otherArrival = default;
        EntityUid exit = default;

        try
        {
            await server.WaitAssertion(() =>
            {
                maps.CreateMap(out originalMap);
                var teleporter = entities.SpawnEntity("InfinityDormTeleporter", new MapCoordinates(Vector2.Zero, originalMap));
                visitor = entities.SpawnEntity("MobHuman", new MapCoordinates(Vector2.One, originalMap));
                pen = entities.SpawnEntity("Pen", new MapCoordinates(Vector2.One, originalMap));
                Assert.That(hands.TryPickupAnyHand(visitor, pen, checkActionBlocker: false), Is.True);
                entities.EventBus.RaiseLocalEvent(teleporter, new InfinityDormTeleportMessage("Nature", 12345)
                {
                    Actor = visitor,
                    UiKey = InfinityDormTeleporterUiKey.Key,
                });
                Assert.That(entities.HasComponent<InfinityDormVisitorComponent>(visitor), Is.True);
                dormMap = entities.GetComponent<TransformComponent>(visitor).MapID;
                Assert.That(hands.TryDrop(visitor, pen), Is.True);

                secondVisitor = entities.SpawnEntity("MobHuman", new MapCoordinates(Vector2.One, originalMap));
                entities.EventBus.RaiseLocalEvent(teleporter, new InfinityDormTeleportMessage("Nature", 12345)
                {
                    Actor = secondVisitor,
                    UiKey = InfinityDormTeleporterUiKey.Key,
                });
                Assert.That(entities.GetComponent<TransformComponent>(secondVisitor).ParentUid,
                    Is.EqualTo(entities.GetComponent<TransformComponent>(visitor).ParentUid));
                otherArrival = entities.SpawnEntity("MobHuman", new MapCoordinates(Vector2.One, originalMap));
                transforms.SetCoordinates(otherArrival, entities.GetComponent<TransformComponent>(visitor).Coordinates);
                exit = entities.AllEntities<InfinityDormExitComponent>()
                    .First(ent => entities.GetComponent<TransformComponent>(ent).MapID == dormMap).Owner;
                entities.GetComponent<InfinityDormExitComponent>(exit).TimeToExit = TimeSpan.Zero;
            });

            await server.WaitRunTicks(2);
            await server.WaitAssertion(() =>
            {
                Assert.That(entities.EntityExists(pen), Is.True, "A visitor's dropped belongings must survive.");
                foreach (var character in new[] { visitor, secondVisitor, otherArrival })
                {
                    Assert.That(entities.EntityExists(character), Is.True, "Every visitor must survive entering the room.");
                    Assert.That(entities.HasComponent<InfinityDormVisitorComponent>(character), Is.True);
                    transforms.SetCoordinates(character, entities.GetComponent<TransformComponent>(exit).Coordinates);
                    entities.EventBus.RaiseLocalEvent(exit, new InteractHandEvent(character, exit));
                    Assert.That(entities.GetComponent<TransformComponent>(character).MapID, Is.EqualTo(originalMap));
                }
            });
        }
        finally
        {
            await server.WaitPost(() =>
            {
                if (maps.MapExists(dormMap))
                    maps.DeleteMap(dormMap);
                if (maps.MapExists(originalMap))
                    maps.DeleteMap(originalMap);
            });
        }
    }

    // Arcane-End

    // Arcane-Start: bag insertion distinguishes belongings from items acquired in the room
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task BagKeepsBelongingsButCannotExportDormItems()
    {
        await OverrideCVar(Side.Server, ArcaneCVars.InfinityDormsEnabled, true);
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var maps = entities.System<SharedMapSystem>();
        var transforms = entities.System<SharedTransformSystem>();
        var hands = entities.System<SharedHandsSystem>();
        var storage = entities.System<SharedStorageSystem>();
        var containers = entities.System<SharedContainerSystem>();
        var tags = entities.System<TagSystem>();
        var originalMap = MapId.Nullspace;
        var dormMap = MapId.Nullspace;

        try
        {
            await server.WaitAssertion(() =>
            {
                maps.CreateMap(out originalMap);
                var coords = new MapCoordinates(Vector2.One, originalMap);
                var teleporter = entities.SpawnEntity("InfinityDormTeleporter", coords);
                var visitor = entities.SpawnEntity("MobHuman", coords);
                var bag = entities.SpawnEntity("BooksBag", coords);
                var ownBook = entities.SpawnEntity("BookTruth", coords);
                Assert.That(storage.Insert(bag, ownBook, out _), Is.True);
                Assert.That(hands.TryPickupAnyHand(visitor, bag, checkActionBlocker: false), Is.True);
                entities.EventBus.RaiseLocalEvent(teleporter, new InfinityDormTeleportMessage("Nature", 12345)
                {
                    Actor = visitor,
                    UiKey = InfinityDormTeleporterUiKey.Key,
                });
                dormMap = entities.GetComponent<TransformComponent>(visitor).MapID;
                Assert.That(tags.HasTag(ownBook, OriginalItem), Is.True);
                Assert.That(tags.HasTag(ownBook, DormItem), Is.False);

                var dormBook = entities.SpawnEntity("BookNarsieLegend", transforms.GetMapCoordinates(visitor));
                Assert.That(storage.Insert(bag, dormBook, out _, user: visitor), Is.True);
                Assert.That(tags.HasTag(dormBook, DormItem), Is.True);
                var exit = entities.AllEntities<InfinityDormExitComponent>()
                    .First(ent => entities.GetComponent<TransformComponent>(ent).MapID == dormMap);
                exit.Comp.TimeToExit = TimeSpan.Zero;
                transforms.SetCoordinates(visitor, entities.GetComponent<TransformComponent>(exit.Owner).Coordinates);
                entities.EventBus.RaiseLocalEvent(exit.Owner, new InteractHandEvent(visitor, exit.Owner));
                Assert.That(entities.GetComponent<TransformComponent>(visitor).MapID, Is.EqualTo(dormMap));

                Assert.That(containers.Remove(dormBook, entities.GetComponent<StorageComponent>(bag).Container), Is.True);
                entities.EventBus.RaiseLocalEvent(exit.Owner, new InteractHandEvent(visitor, exit.Owner));
                Assert.That(entities.GetComponent<TransformComponent>(visitor).MapID, Is.EqualTo(originalMap));
                Assert.That(entities.EntityExists(ownBook), Is.True);
            });
        }
        finally
        {
            await server.WaitPost(() =>
            {
                if (maps.MapExists(dormMap))
                    maps.DeleteMap(dormMap);
                if (maps.MapExists(originalMap))
                    maps.DeleteMap(originalMap);
            });
        }
    }
    // Arcane-End

    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task DisabledDormsRejectEntryAndCreationWhileVisitorsCanExit()
    {
        await OverrideCVar(Side.Server, ArcaneCVars.InfinityDormsEnabled, true);
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var configuration = server.ResolveDependency<IConfigurationManager>();
        var maps = entities.System<SharedMapSystem>();
        var transforms = entities.System<SharedTransformSystem>();
        var originalMap = MapId.Nullspace;
        var dormMap = MapId.Nullspace;

        try
        {
            await server.WaitAssertion(() =>
            {
                maps.CreateMap(out originalMap);
                var teleporter = entities.SpawnEntity("InfinityDormTeleporter", new MapCoordinates(Vector2.Zero, originalMap));
                var visitor = entities.SpawnEntity("MobHuman", new MapCoordinates(Vector2.One, originalMap));
                var request = new InfinityDormTeleportMessage("Nature", 12345)
                {
                    Actor = visitor,
                    UiKey = InfinityDormTeleporterUiKey.Key,
                };

                entities.EventBus.RaiseLocalEvent(teleporter, request);
                Assert.That(entities.HasComponent<InfinityDormVisitorComponent>(visitor), Is.True);
                dormMap = entities.GetComponent<TransformComponent>(visitor).MapID;
                Assert.That(dormMap, Is.Not.EqualTo(originalMap));
                Assert.That(entities.AllEntities<InfinityDormComponent>().Count(), Is.EqualTo(1));

                configuration.SetCVar(ArcaneCVars.InfinityDormsEnabled, false);
                var exit = entities.AllEntities<InfinityDormExitComponent>()
                    .First(ent => entities.GetComponent<TransformComponent>(ent).MapID == dormMap);
                exit.Comp.TimeToExit = TimeSpan.Zero;
                transforms.SetCoordinates(visitor, entities.GetComponent<TransformComponent>(exit).Coordinates);
                entities.EventBus.RaiseLocalEvent(exit.Owner, new InteractHandEvent(visitor, exit.Owner));
                Assert.That(entities.HasComponent<InfinityDormVisitorComponent>(visitor), Is.False);
                Assert.That(entities.GetComponent<TransformComponent>(visitor).MapID, Is.EqualTo(originalMap));

                entities.EventBus.RaiseLocalEvent(teleporter, request);
                entities.EventBus.RaiseLocalEvent(teleporter, new InfinityDormTeleportMessage("Nature", 12346)
                {
                    Actor = visitor,
                    UiKey = InfinityDormTeleporterUiKey.Key,
                });
                Assert.That(entities.HasComponent<InfinityDormVisitorComponent>(visitor), Is.False);
                Assert.That(entities.GetComponent<TransformComponent>(visitor).MapID, Is.EqualTo(originalMap));
                Assert.That(entities.AllEntities<InfinityDormComponent>().Count(), Is.EqualTo(1));

                configuration.SetCVar(ArcaneCVars.InfinityDormsEnabled, true);
                entities.EventBus.RaiseLocalEvent(teleporter, request);
                Assert.That(entities.HasComponent<InfinityDormVisitorComponent>(visitor), Is.True);
                Assert.That(entities.GetComponent<TransformComponent>(visitor).MapID, Is.EqualTo(dormMap));
                Assert.That(entities.AllEntities<InfinityDormComponent>().Count(), Is.EqualTo(1));
            });
        }
        finally
        {
            await server.WaitPost(() =>
            {
                if (maps.MapExists(dormMap))
                    maps.DeleteMap(dormMap);
                if (maps.MapExists(originalMap))
                    maps.DeleteMap(originalMap);
            });
        }
    }

    /// <summary>
    /// Every infinity dorm room loads as a grid, so the teleporter never fails at runtime.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task AllDormRoomsLoad()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var mapLoader = entManager.System<MapLoaderSystem>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var failures = new List<string>();

        await server.WaitPost(() =>
        {
            foreach (var dorm in prototypes.EnumeratePrototypes<InfinityDormPrototype>())
            {
                mapSystem.CreateMap(out var mapId);
                try
                {
                    if (!mapLoader.TryLoadGrid(mapId, dorm.GridPath, out _))
                        failures.Add($"{dorm.ID}: {dorm.GridPath} is not a single grid");
                }
                catch (Exception e)
                {
                    failures.Add($"{dorm.ID}: {e.Message}");
                }
                finally
                {
                    mapSystem.DeleteMap(mapId);
                }
            }
        });

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }
}
