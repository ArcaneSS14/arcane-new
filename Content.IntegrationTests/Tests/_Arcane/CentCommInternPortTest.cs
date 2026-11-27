// Arcane-Start
using System.Linq;
using Content.Arcane.Server.CloningAppearance;
using Content.Server.Antag.Components;
using Content.Shared.Access.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class CentCommInternPortTest : GameTest
{
    [TestCase("LeadIntern")]
    [TestCase("NTIntern")]
    [TestCase("NTNerdIntern")]
    public async Task AttachedInternGetsProfileEquipmentAndMindshield(string prototype)
    {
        var map = await Pair.CreateTestMap();
        EntityUid placeholder = default;
        EntityUid? oldAttachment = null;
        try
        {
            await Server.WaitAssertion(() =>
            {
                oldAttachment = ServerSession!.AttachedEntity;
                placeholder = SSpawnAtPosition(prototype, map.GridCoords);
                Assert.That(SHasComp<CloningAppearanceComponent>(placeholder), Is.True);
                Server.PlayerMan.SetAttachedEntity(ServerSession, placeholder, out _, true);
            });
            await Pair.RunTicksSync(5);
            await Server.WaitAssertion(() =>
            {
                var mob = ServerSession!.AttachedEntity;
                Assert.That(mob, Is.Not.Null.And.Not.EqualTo(placeholder));
                Assert.That(SHasComp<AntagImmuneComponent>(mob!.Value), Is.True);
                var inventory = Server.System<InventorySystem>();
                Assert.That(inventory.TryGetSlotEntity(mob.Value, "id", out var card), Is.True);
                Assert.That(SComp<MetaDataComponent>(card!.Value).EntityPrototype?.ID, Is.EqualTo("NTInternIDCard"));
                Assert.That(SComp<AccessComponent>(card.Value).Tags.Select(x => x.Id), Does.Contain("CentralCommand"));
                var implants = SComp<ImplantedComponent>(mob.Value).ImplantContainer.ContainedEntities;
                Assert.That(implants.Any(x => SComp<MetaDataComponent>(x).EntityPrototype?.ID == "MindShieldImplant"), Is.True);
            });
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, oldAttachment));
        }
    }
}
// Arcane-End
