// Arcane-Start: only removing the worn collar detaches its leash
using Content.Arcane.Server.Leash;
using Content.Arcane.Shared.Leash;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Inventory;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class LeashInventoryTest : GameTest
{
    [Test]
    public async Task RemovingSpareCollarDoesNotDetachLeash()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SSpawnAtPosition("MobHuman", map.GridCoords);
            var handler = SSpawnAtPosition("MobHuman", map.GridCoords);
            var worn = SSpawnAtPosition("Collar", map.GridCoords);
            var spare = SSpawnAtPosition("Collar", map.GridCoords);
            var leash = SSpawnAtPosition("Leash", map.GridCoords);
            var inventory = Server.System<InventorySystem>();
            Assert.That(inventory.TryEquip(wearer, worn, "neck", force: true), Is.True);
            Assert.That(inventory.TryEquip(wearer, spare, "pocket1", force: true), Is.True);
            Assert.That(Server.System<LeashSystem>().TryAttachLeash(leash, handler, wearer), Is.True);
            var component = SEntMan.GetComponent<LeashComponent>(leash);
            Assert.That(inventory.TryUnequip(wearer, "pocket1", force: true), Is.True);
            Assert.That(component.AttachedEntity, Is.EqualTo(wearer));
            Assert.That(inventory.TryUnequip(wearer, "neck", force: true), Is.True);
            Assert.That(component.AttachedEntity, Is.Null);
        });
    }
}
// Arcane-End
