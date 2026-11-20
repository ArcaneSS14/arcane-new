using System;
using System.Collections.Generic;
using Content.Arcane.Shared.InfinityDorm;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.InfinityDorm;

[TestFixture]
public sealed class InfinityDormMapsTest : GameTest
{
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
