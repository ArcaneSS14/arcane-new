// Arcane-Start
using Content.Arcane.Shared.Body.Part;
using Content.Arcane.Shared.ERP;
using Content.Arcane.Shared.ErpPanel.Requirements;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ArcaneInteractionPortTest : GameTest
{
    private static readonly ProtoId<TagPrototype> Skirt = "Skirt";

    [Test]
    public async Task PlayerPreferencesOnlyApplyToAllowedSpecies()
    {
        var map = await Pair.CreateTestMap();
        EntityUid? previous = null;
        try
        {
            await Server.WaitAssertion(() =>
            {
                previous = ServerSession!.AttachedEntity;
                var monkey = SSpawnAtPosition("MobMonkey", map.GridCoords);
                var human = SSpawnAtPosition("MobHuman", map.GridCoords);
                var xeno = SSpawnAtPosition("MobHumanoidXeno", map.GridCoords);
                var ipc = SSpawnAtPosition("MobIPC", map.GridCoords);
                Assert.That(SEntMan.HasComponent<HumanoidProfileComponent>(monkey), Is.True);
                Assert.That(SEntMan.HasComponent<ErpAllowedComponent>(monkey), Is.False);
                Assert.That(SEntMan.HasComponent<ErpAllowedComponent>(human), Is.True);
                Assert.That(SEntMan.HasComponent<ErpAllowedComponent>(xeno), Is.True);
                Assert.That(SEntMan.HasComponent<ErpAllowedComponent>(ipc), Is.True);

                Server.PlayerMan.SetAttachedEntity(ServerSession, monkey);
                Assert.That(SEntMan.HasComponent<ErpStatusComponent>(monkey), Is.False);
                Assert.That(SEntMan.HasComponent<ArousalComponent>(monkey), Is.False);

                Server.PlayerMan.SetAttachedEntity(ServerSession, human);
                Assert.That(SEntMan.HasComponent<ErpStatusComponent>(human), Is.True);
                Assert.That(SEntMan.HasComponent<ArousalComponent>(human), Is.True);

                Server.PlayerMan.SetAttachedEntity(ServerSession, xeno);
                Assert.That(SEntMan.HasComponent<ErpStatusComponent>(xeno), Is.True);
            });
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, previous));
        }
    }

    [TestCase("ClothingUniformJumpsuitColorGrey", false)]
    [TestCase("ClothingUniformJumpskirtColorGrey", true)]
    [TestCase("ClothingUniformJumpskirtOperative", true)]
    public async Task SkirtTagsDoNotMakeOtherUniformsUncovering(string uniformPrototype, bool skirt)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition("MobHuman", map.GridCoords);
            var uniform = SSpawnAtPosition(uniformPrototype, map.GridCoords);
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(SEntMan.System<TagSystem>().HasTag(uniform, Skirt), Is.EqualTo(skirt));
            Assert.That(inventory.TryEquip(human, uniform, "jumpsuit", force: true), Is.True);

            var groin = new ExposedBodyPartRequirement { Part = ErpBodyPartType.Groin };
            var butt = new ExposedBodyPartRequirement { Part = ErpBodyPartType.Butt };
            var chest = new ExposedBodyPartRequirement { Part = ErpBodyPartType.Chest };
            Assert.That(groin.IsAvailable(human, SEntMan), Is.EqualTo(skirt));
            Assert.That(butt.IsAvailable(human, SEntMan), Is.EqualTo(skirt));
            Assert.That(chest.IsAvailable(human, SEntMan), Is.False);

            if (!skirt)
                return;

            var underwear = SSpawnAtPosition("ClothingUnderwearBoxerShorts", map.GridCoords);
            Assert.That(inventory.TryEquip(human, underwear, "underwear", force: true), Is.True);
            Assert.That(groin.IsAvailable(human, SEntMan), Is.False);
            Assert.That(butt.IsAvailable(human, SEntMan), Is.False);
        });
    }
}
// Arcane-End
