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

namespace Content.IntegrationTests.Tests._Arcane.InfinityDorm;

[TestFixture]
public sealed class InfinityDormMapsTest : GameTest
{
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
