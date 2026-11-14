// Arcane-Start
#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P12LavalandRuntimeTest : InteractionTest
{
    private const string TestSpawner = "TestArcaneP12ProximitySpawner";
    private static readonly EntProtoId Result = "MaterialWoodPlank1";

    protected override string PlayerPrototype => "MobHuman";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: ArcaneLavalandProximitySpawner
  id: TestArcaneP12ProximitySpawner
  components:
  - type: SpawnEntityTableOnTrigger
    table: !type:EntSelector
      id: MaterialWoodPlank1
""";

    [Test]
    public async Task NpcProximityDoesNotConsumeTheLavalandSpawner()
    {
        try
        {
            await VerifyNpcContact();
        }
        catch (Exception error)
        {
            TestContext.Out.WriteLine(error);
            throw;
        }
    }

    private async Task VerifyNpcContact()
    {
        var spawnPoint = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
        var marker = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            Transform.SetCoordinates(SPlayer, spawnPoint.Offset(new Vector2(40, 0)));
            SEntMan.SpawnAttachedTo("MobHuman", spawnPoint);
            marker = SEntMan.SpawnAttachedTo(TestSpawner, spawnPoint);
        });

        await RunSeconds(0.6f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(marker), Is.True, "The spawner was consumed without a player.");
            Assert.That(CountResults(), Is.Zero);
            Assert.That(SEntMan.HasComponent<ActorComponent>(SPlayer), Is.True, "The test player has no Actor.");
            Transform.SetCoordinates(SPlayer, spawnPoint.Offset(new Vector2(-1, 0)));
        });

        await RunSeconds(0.6f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(marker), Is.False, "The player did not activate the spawner.");
            Assert.That(CountResults(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task BiomeLayerSeedChangeUsesTheNewNoise()
    {
        await Server.WaitAssertion(() =>
        {
            var biome = SEntMan.System<SharedBiomeSystem>();
            var layer = new BiomeTileLayer { Tile = "Plating" };
            var updatedLayer = new BiomeTileLayer { Tile = layer.Tile };
            updatedLayer.Noise.SetSeed(234);
            List<IBiomeLayer> layers = [layer];
            List<IBiomeLayer> updatedLayers = [updatedLayer];
            var noGrid = (Entity<MapGridComponent>?) null;

            for (var x = -48; x < 48; x++)
            {
                var indices = new Vector2i(x, 13);
                var hadTile = biome.TryGetTile(indices, layers, 0, noGrid, out _);
                var shouldHaveTile = biome.TryGetTile(indices, updatedLayers, 0, noGrid, out _);
                if (hadTile == shouldHaveTile)
                    continue;

                layer.Noise.SetSeed(updatedLayer.Noise.GetSeed());
                Assert.That(biome.TryGetTile(indices, layers, 0, noGrid, out _), Is.EqualTo(shouldHaveTile));
                return;
            }

            Assert.Fail("The selected noise seeds did not produce different tiles in the sampled area.");
        });
    }

    private int CountResults()
    {
        var result = 0;
        var query = SEntMan.EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var meta))
        {
            if (meta.EntityPrototype?.ID == Result.Id)
                result++;
        }
        return result;
    }
}
// Arcane-End
