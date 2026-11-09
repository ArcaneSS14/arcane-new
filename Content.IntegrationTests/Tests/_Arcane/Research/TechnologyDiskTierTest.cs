#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Research.TechnologyDisk.Components;
using Content.Shared.Research.TechnologyDisk.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Research;

[TestOf(typeof(TechnologyDiskSystem))]
public sealed class TechnologyDiskTierTest : GameTest
{
    private const string Disk = "TestTechnologyDiskRolledTierThree";

    [TestPrototypes]
    private const string Prototypes = @"
- type: weightedRandom
  id: TestTechDiskOnlyTierThree
  weights:
    3: 1

- type: entity
  parent: TechnologyDisk
  id: TestTechnologyDiskRolledTierThree
  components:
  - type: TechnologyDisk
    discipline: Service
    tierWeightPrototype: TestTechDiskOnlyTierThree
";

    [Test, Description("Service has no tier 3 recipes, so a disk that rolls that tier must still get one.")]
    public async Task RolledTierWithoutRecipesFallsBack()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var disk = SSpawnAtPosition(Disk, TestMap!.GridCoords);
            Assert.That(SComp<TechnologyDiskComponent>(disk).Recipes, Is.Not.Null.And.Not.Empty);
        });
    }
}
