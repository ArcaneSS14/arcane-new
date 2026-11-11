#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.NPC.HTN;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class GrowingCompanyPortTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [TestPrototypes]
    private const string Prototypes = """
- type: weightedRandomFillSolution
  id: TestArcaneSpaceshroomFill
  fills:
  - quantity: 10
    weight: 1
    reagents: [SpaceDrugs]
- type: entity
  parent: FoodSpaceshroom
  id: TestArcaneFixedSpaceshroom
  components:
  - type: RandomFillSolution
    weightedRandomId: TestArcaneSpaceshroomFill
""";

    [Test]
    public async Task WildSporesLeadToARealHarvestWithoutErasingItsRandomMixture()
    {
        await Server.WaitAssertion(() =>
        {
            var botany = SEntMan.System<BotanySystem>();
            var coords = SEntMan.GetCoordinates(TargetCoords);
            var wild = SEntMan.SpawnAtPosition("FoodSpaceshroom", coords);
            var produce = SEntMan.GetComponent<ProduceComponent>(wild);
            Assert.That(produce.PlantProtoId?.Id, Is.EqualTo("SpaceshroomPlants"));
            Assert.That(botany.TryGetPlantComponent<PlantDataComponent>(produce.PlantData,
                produce.PlantProtoId, out var data), Is.True);
            var packet = botany.SpawnSeedPacket(data!, produce.PlantProtoId!.Value, produce.PlantData, coords, SPlayer);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(packet).EntityPrototype!.ID, Is.EqualTo("SpaceshroomSeeds"));
            var seed = SEntMan.GetComponent<SeedComponent>(packet);
            Assert.That(seed.PlantProtoId.Id, Is.EqualTo("SpaceshroomPlants"));

            var plant = SEntMan.SpawnAtPosition(seed.PlantProtoId.Id, coords);
            Assert.That(SEntMan.GetComponent<PlantHarvestComponent>(plant).HarvestRepeat, Is.EqualTo(HarvestType.Repeat));
            // The ordinary table can intentionally choose zero. Pin a nonzero draw for this regression.
            SEntMan.GetComponent<PlantDataComponent>(plant).ProductPrototypes = ["TestArcaneFixedSpaceshroom"];
            var crop = HarvestOne(plant);
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(crop, "food", out _, out var food), Is.True);
            Assert.That(food!.GetTotalPrototypeQuantity("Nutriment"), Is.GreaterThanOrEqualTo(FixedPoint2.New(12)));
            Assert.That(food.GetTotalPrototypeQuantity("Vitamin"), Is.GreaterThanOrEqualTo(FixedPoint2.New(8)));
            Assert.That(food.Volume, Is.GreaterThan(FixedPoint2.New(20)), "The randomized contents must survive the growing path.");
            Assert.That(food.GetTotalPrototypeQuantity("SpaceDrugs"), Is.EqualTo(FixedPoint2.New(10)));

            // Added plant chemicals still replace the prototype mixture, including on spaceshrooms.
            SEntMan.GetComponent<PlantChemicalsComponent>(plant).Chemicals["Nutriment"] =
                new PlantChemQuantity { Min = 3, Max = 3 };
            var modified = HarvestOne(plant);
            Assert.That(solutions.TryGetSolution(modified, "food", out _, out var changed), Is.True);
            Assert.That(changed!.GetTotalPrototypeQuantity("Nutriment"), Is.EqualTo(FixedPoint2.New(3)));
            Assert.That(changed.Volume, Is.EqualTo(FixedPoint2.New(3)));

            // Existing empty-chemistry crops keep Trauma's original clearing behavior.
            var originalWheat = SEntMan.SpawnAtPosition("WheatBushel", coords);
            Assert.That(solutions.TryGetSolution(originalWheat, "food", out _, out var originalFood), Is.True);
            Assert.That(originalFood!.Volume, Is.GreaterThan(FixedPoint2.Zero));
            SEntMan.DeleteEntity(originalWheat);
            var wheat = SEntMan.SpawnAtPosition("WheatPlants", coords);
            SEntMan.GetComponent<PlantChemicalsComponent>(wheat).Chemicals.Clear();
            var empty = HarvestOne(wheat);
            Assert.That(solutions.TryGetSolution(empty, "food", out _, out var cleared), Is.True);
            Assert.That(cleared!.Volume, Is.EqualTo(FixedPoint2.Zero));
        });
    }

    [Test]
    public async Task MothroachVariantsUseTheirOwnWorldAndClothingResources()
    {
        foreach (var (suffix, sprite) in new[]
        {
            ("Mustard", "mustard"), ("Leopard", "leopard"), ("Cecropia", "cecropia"), ("Lunar", "lunar"),
        })
        {
            var net = NetEntity.Invalid;
            var path = $"_Arcane/Mobs/Animals/{sprite}_mothroach.rsi";
            await Server.WaitAssertion(() =>
            {
                var uid = SEntMan.SpawnAtPosition($"MobMothroach{suffix}", SEntMan.GetCoordinates(TargetCoords));
                SEntMan.RemoveComponent<HTNComponent>(uid);
                net = SEntMan.GetNetEntity(uid);
                var clothing = SEntMan.GetComponent<ClothingComponent>(uid);
                Assert.That(clothing.RsiPath, Is.EqualTo(path));
                Assert.That(clothing.EquippedPrefix, Is.EqualTo("0"));
            });
            await RunUntilSynced();
            await Client.WaitAssertion(() => Assert.That(CEntMan.GetComponent<SpriteComponent>(ToClient(net)).BaseRSI!.Path.ToString(),
                Is.EqualTo($"/Textures/{path}")));
        }
    }

    private EntityUid HarvestOne(EntityUid plant)
    {
        var before = ProduceEntities().ToHashSet();
        SEntMan.System<BotanySystem>().SpawnProduce((plant, SEntMan.GetComponent<PlantComponent>(plant),
            SEntMan.GetComponent<PlantDataComponent>(plant)), SEntMan.GetCoordinates(TargetCoords), 1);
        return ProduceEntities().Single(uid => !before.Contains(uid));
    }

    private IEnumerable<EntityUid> ProduceEntities()
    {
        var result = new List<EntityUid>();
        var query = SEntMan.EntityQueryEnumerator<ProduceComponent>();
        while (query.MoveNext(out var uid, out _))
            result.Add(uid);
        return result;
    }
}
