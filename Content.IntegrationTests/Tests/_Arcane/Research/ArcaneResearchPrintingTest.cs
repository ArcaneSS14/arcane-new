// Arcane-Start
#nullable enable
using System.Linq;
using Content.Server.Lathe;
using Content.Server.Lathe.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Server.Station.Systems;
using Content.Shared.AlertLevel;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Research;

public sealed class ArcaneResearchPrintingTest : GameTest
{
    [TestCase("ExosuitFabricator", "MechHeavyArms", "WeaponMechCombatMissileRack6")]
    [TestCase("ExosuitFabricator", "MechHeavyArms", "WeaponMechChainSword")]
    [TestCase("ExosuitFabricator", "MechEnergyGuns", "WeaponMechCombatTeslaCannon")]
    [TestCase("SecurityTechFab", "ElectricWeapons", "WeaponDisablerSMG")]
    [TestCase("SecurityTechFab", "UraniumAmmo", "MagazineBoxShotgunUraniumHighCaliber")]
    public async Task ResearchUnlocksPrintingOnTheIntendedMachine(string machine, string technology, string recipeId)
    {
        var id = new ProtoId<LatheRecipePrototype>(recipeId);
        var map = await Pair.CreateTestMap();
        EntityUid lathe = default;
        LatheRecipePrototype recipe = default!;
        HashSet<EntityUid> originalResults = new();
        Dictionary<string, int> materialCosts = new();
        var completionTime = TimeSpan.Zero;

        await Server.WaitAssertion(() =>
        {
            var station = SSpawn("TestStation");
            SEntMan.System<ServerStationSystem>().AddGridToStation(station, map.Grid);
            SEntMan.System<AlertLevelSystem>().SetLevel(station, "Red", playSound: false, announce: false, force: true);

            lathe = SSpawnAtPosition(machine, map.GridCoords);
            Assert.That(SComp<TransformComponent>(lathe).Anchored, Is.True);
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(lathe, false);
            recipe = Server.ProtoMan.Index<LatheRecipePrototype>(recipeId);
            Assert.That(recipe.Result, Is.Not.Null);
            originalResults = FindResults(recipe.Result!.Value.Id);

            var materialStorage = SEntMan.System<SharedMaterialStorageSystem>();
            var component = SComp<LatheComponent>(lathe);
            foreach (var (material, amount) in recipe.Materials)
            {
                var cost = SharedLatheSystem.AdjustMaterial(amount, recipe.ApplyMaterialDiscount, component.FinalMaterialMultiplier);
                materialCosts.Add(material.Id, cost);
                Assert.That(materialStorage.TryChangeMaterialAmount(lathe, material.Id, cost * 2), Is.True);
            }
        });
        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            var lathes = SEntMan.System<LatheSystem>();
            var component = SComp<LatheComponent>(lathe);
            var materialStorage = SEntMan.System<SharedMaterialStorageSystem>();
            Assert.That(SEntMan.System<PowerReceiverSystem>().IsPowered(lathe), Is.True);
            Assert.That(lathes.GetAvailableRecipes(lathe, component), Does.Not.Contain(id));
            Assert.That(lathes.TryAddToQueue(lathe, recipe, 1), Is.False);
            foreach (var (material, cost) in materialCosts)
            {
                Assert.That(materialStorage.GetMaterialAmount(lathe, material), Is.EqualTo(cost * 2));
            }

            SEntMan.System<ResearchSystem>().AddTechnology(lathe, technology);

            Assert.That(lathes.GetAvailableRecipes(lathe, component), Does.Contain(id));
            Assert.That(lathes.TryAddToQueue(lathe, recipe, 1), Is.True);
            foreach (var (material, cost) in materialCosts)
            {
                Assert.That(materialStorage.GetMaterialAmount(lathe, material), Is.EqualTo(cost));
            }
            Assert.That(lathes.TryStartProducing(lathe), Is.True);
            completionTime = SComp<LatheProducingComponent>(lathe).ProductionLength;
        });
        await RunSeconds((float) completionTime.TotalSeconds + 1);

        await Server.WaitAssertion(() =>
        {
            var results = FindResults(recipe.Result!.Value.Id);
            results.ExceptWith(originalResults);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(SComp<LatheComponent>(lathe).CurrentRecipe, Is.Null);
            Assert.That(SComp<LatheComponent>(lathe).Queue, Is.Empty);
            foreach (var result in results)
            {
                SDel(result);
            }
            SDel(map.MapUid);
        });
    }

    private HashSet<EntityUid> FindResults(string prototype)
    {
        var result = new HashSet<EntityUid>();
        var query = SEntMan.EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var metadata))
        {
            if (metadata.EntityPrototype?.ID == prototype)
                result.Add(uid);
        }
        return result;
    }
}
// Arcane-End
