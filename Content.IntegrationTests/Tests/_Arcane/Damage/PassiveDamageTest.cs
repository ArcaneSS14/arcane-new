#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Damage;

public sealed class PassiveDamageTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };
    private const string TestItem = "TestArcanePassiveDamageItem";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: TestArcanePassiveDamageItem
  components:
  - type: Damageable
  - type: Injurable
  - type: PassiveDamage
    damage:
      types:
        Blunt: 1
""";

    [TestCase(false)]
    [TestCase(true)]
    public async Task EveryEligibleEntityReceivesPassiveDamage(bool includeStoredItem)
    {
        await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var items = new List<EntityUid>();
            EntityUid? stored = null;
            for (var i = 0; i < 4; i++)
            {
                var item = SSpawnAtPosition(TestItem, TestMap!.GridCoords);
                var passive = SComp<PassiveDamageComponent>(item);
                passive.NextDamage = TimeSpan.Zero;
                if (includeStoredItem && i == 1)
                {
                    var holder = SEntMan.SpawnEntity(null, TestMap.GridCoords);
                    var containers = SEntMan.System<SharedContainerSystem>();
                    var container = containers.EnsureContainer<Container>(holder, "test");
                    Assert.That(containers.Insert(item, container), Is.True);
                    passive.PauseWhenStored = true;
                    stored = item;
                }
                else
                {
                    items.Add(item);
                }
            }

            SEntMan.System<PassiveDamageSystem>().Update(0f);
            var damage = SEntMan.System<DamageableSystem>();
            foreach (var item in items)
                Assert.That(damage.GetTotalDamage(item).Int(), Is.EqualTo(1));
            if (stored is { } contained)
                Assert.That(damage.GetTotalDamage(contained).Int(), Is.Zero);
        });
    }
}
