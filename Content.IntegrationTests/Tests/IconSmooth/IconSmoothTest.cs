// Arcane-Start
using System.Collections.Generic;
using Content.Client.IconSmoothing;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Fluids.Components;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.IconSmooth;

[TestFixture]
public sealed class IconSmoothTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        Connected = true,
    };

    private static readonly ProtoId<ContentTileDefinition> TileRef = "Plating";

    [SidedDependency(Side.Server)] private ITileDefinitionManager _tileDefMan = default!;
    [SidedDependency(Side.Client)] private IconSmoothSystem _iconSmooth = default!;
    [SidedDependency(Side.Server)] private SharedMapSystem _map = default!;
    [SidedDependency(Side.Server)] private SharedTransformSystem _xform = default!;

    private static readonly string[] IconSmoothEntities = GameDataScrounger.EntitiesWithComponent("IconSmooth");
    private List<EntityUid> _spawned = new ();

    /// <summary>
    /// Every pair of these keys is its own key set: 276 of them, more than a byte could index.
    /// </summary>
    private const int StressKeys = 24;

    [TestPrototypes]
    private const string StressPrototypes = @"
- type: entity
  id: IconSmoothStressBase
  abstract: true
  components:
  - type: Transform
    anchored: true
  - type: Sprite

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress0
  components:
  - type: IconSmooth
    key: stress0

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress1
  components:
  - type: IconSmooth
    key: stress1

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress2
  components:
  - type: IconSmooth
    key: stress2

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress3
  components:
  - type: IconSmooth
    key: stress3

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress4
  components:
  - type: IconSmooth
    key: stress4

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress5
  components:
  - type: IconSmooth
    key: stress5

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress6
  components:
  - type: IconSmooth
    key: stress6

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress7
  components:
  - type: IconSmooth
    key: stress7

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress8
  components:
  - type: IconSmooth
    key: stress8

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress9
  components:
  - type: IconSmooth
    key: stress9

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress10
  components:
  - type: IconSmooth
    key: stress10

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress11
  components:
  - type: IconSmooth
    key: stress11

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress12
  components:
  - type: IconSmooth
    key: stress12

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress13
  components:
  - type: IconSmooth
    key: stress13

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress14
  components:
  - type: IconSmooth
    key: stress14

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress15
  components:
  - type: IconSmooth
    key: stress15

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress16
  components:
  - type: IconSmooth
    key: stress16

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress17
  components:
  - type: IconSmooth
    key: stress17

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress18
  components:
  - type: IconSmooth
    key: stress18

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress19
  components:
  - type: IconSmooth
    key: stress19

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress20
  components:
  - type: IconSmooth
    key: stress20

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress21
  components:
  - type: IconSmooth
    key: stress21

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress22
  components:
  - type: IconSmooth
    key: stress22

- type: entity
  parent: IconSmoothStressBase
  id: IconSmoothStress23
  components:
  - type: IconSmooth
    key: stress23
";

    /// <summary>
    /// Fills the key cache with more different key sets than a byte index can tell apart, and checks that every tile
    /// still resolves to its own keys instead of one that wrapped around onto it.
    /// </summary>
    [Test]
    public async Task ManyKeySetsKeepTheirOwnCache()
    {
        await Pair.CreateTestMap();
        await Pair.SyncTicks();

        var pairs = new List<(int A, int B)>();
        for (var a = 0; a < StressKeys; a++)
        {
            for (var b = a + 1; b < StressKeys; b++)
            {
                pairs.Add((a, b));
            }
        }

        var side = (int) Math.Ceiling(Math.Sqrt(pairs.Count));
        Assert.That(pairs.Count, Is.GreaterThan(256));

        await Server.WaitPost(() =>
        {
            for (var i = 0; i < pairs.Count; i++)
            {
                var tile = new Vector2i(i % side, i / side);
                _map.SetTile(TestMap!.Grid, TestMap!.Grid, tile, new Tile(_tileDefMan[TileRef].TileId));

                var coords = new EntityCoordinates(TestMap!.Grid, tile.X + 0.5f, tile.Y + 0.5f);
                _spawned.Add(SEntMan.SpawnAtPosition($"IconSmoothStress{pairs[i].A}", coords));
                _spawned.Add(SEntMan.SpawnAtPosition($"IconSmoothStress{pairs[i].B}", coords));
            }
        });
        await Pair.RunUntilSynced();

        var grid = TestMap!.CGridUid;
        await Client.WaitAssertion(() =>
        {
            var cache = CComp<IconSmoothGridComponent>(grid);
            for (var i = 0; i < pairs.Count; i++)
            {
                var keys = _iconSmooth.GetCachedKeys((grid, cache), new Vector2i(i % side, i / side));
                Assert.That(keys, Is.EquivalentTo(new[] { $"stress{pairs[i].A}", $"stress{pairs[i].B}" }), $"Tile {i}");
            }

            Assert.That(_iconSmooth.CountTracked(), Is.EqualTo(pairs.Count));
        });

        await Server.WaitPost(DeleteAll);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(_iconSmooth.CountTracked(), Is.Zero);
        });
    }

    [Test]
    public async Task PreventIconSmoothCacheLeaks()
    {
        await Pair.CreateTestMap();
        await Pair.SyncTicks();

        var dimensions = (int)Math.Sqrt(IconSmoothEntities.Length) + 1;

        // Set up a thin line of tiles to place our objects on. They should be anchored for a "realistic" scenario...
        await Server.WaitPost(() =>
        {
            for (var x = 0; x < dimensions; x++)
            {
                for (var y = 0; y < dimensions; y++)
                {
                    _map.SetTile(TestMap!.Grid,
                        TestMap!.Grid,
                        new Vector2i(x, y),
                        new Tile(_tileDefMan[TileRef].TileId));
                }
            }
        });

        // Spawn entities
        await Server.WaitPost(() =>
        {
            PlaceEntities(dimensions);
        });
        await Pair.RunUntilSynced();

        var cache = CComp<IconSmoothGridComponent>(TestMap!.CGridUid);

        // Ensure client is aware of the entities and has cached them.
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Disable and Re-Enable IconSmooth on all entities.
        await Client.WaitPost(() =>
        {
            SetEnabled(false);
        });
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, 0);
        });
        await Client.WaitPost(() =>
        {
            SetEnabled(true);
        });
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Anchor and then unanchor all entities.
        await Server.WaitPost(UnanchorAll);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, 0);
        });
        await Server.WaitPost(AnchorAll);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Delete and then respawn all entities.
        await Server.WaitPost(DeleteAll);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, 0);
        });
        await Server.WaitPost(() =>
        {
            PlaceEntities(dimensions);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Delete the grid.
        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(TestMap.Grid);
        });
        await Pair.RunUntilSynced();
        Assert.That(_iconSmooth.CountTracked(), Is.EqualTo(0));
    }

    private void ValidateCache(IconSmoothGridComponent cache, int count)
    {
        var tracked = _iconSmooth.CountTracked();
        Assert.That(tracked, Is.EqualTo(count), $"Cached: {tracked}, Count: {count}");

        var cached = 0;
        foreach (var (_, chunk) in cache.Chunks)
        {
            foreach (var tile in chunk.Tiles)
            {
                if (tile == null)
                    continue;

                cached++;
            }
        }

        Assert.That(cached, Is.EqualTo(count), $"Cached: {cached}, Count: {count}");
    }

    private void UnanchorAll()
    {
        foreach (var uid in _spawned)
        {
            _xform.Unanchor(uid);
        }
    }

    private void AnchorAll()
    {
        foreach (var uid in _spawned)
        {
            _xform.AnchorEntity(uid);
        }
    }

    private void SetEnabled(bool enabled)
    {
        var iconEnumerator = CEntMan.EntityQueryEnumerator<IconSmoothComponent>();
        while (iconEnumerator.MoveNext(out var uid, out var comp))
        {
            _iconSmooth.SetEnabled((uid, comp), enabled);
        }
    }

    private void PlaceEntities(int dimensions)
    {
        var i = 0;
        for (var x = 0; x < dimensions; x++)
        {
            for (var y = 0; y < dimensions; y++)
            {
                if (i >= IconSmoothEntities.Length)
                    return;

                var proto = IconSmoothEntities[i++];
                // Puddles may disable smoothing or be deleted when unanchored, so skip them in this cache lifecycle test.
                if (!SProtoMan.Resolve(proto, out var resolved) || resolved.HasComp<PuddleComponent>(SEntMan.ComponentFactory))
                    continue;

                _spawned.Add(SEntMan.SpawnEntity(proto, new EntityCoordinates(TestMap!.Grid, x + 0.5f, y + 0.5f)));
            }
        }
    }

    private void DeleteAll()
    {
        foreach (var uid in _spawned)
        {
            SEntMan.DeleteEntity(uid);
        }
        _spawned.Clear();
    }
}
// Arcane-End
