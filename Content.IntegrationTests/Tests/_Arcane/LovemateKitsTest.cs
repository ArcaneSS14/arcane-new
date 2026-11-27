// Arcane-Start
using System.Linq;
using Content.Shared.Interaction.Events;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class LovemateKitsTest : GameTest
{
    private static readonly ProtoId<VendingMachineInventoryPrototype> Inventory = "LoveMateInventory";

    [TestCase("KitOfficer", "ClothingUniformJumpsuitPoliceman,ClothingHeadHatPolicemanHat,SoftHandcuffs")]
    [TestCase("KitOfficerDress", "ClothingJumpsuitOfficerDress,ClothingHatOficerCapLovemate,SoftHandcuffs")]
    [TestCase("KitTeacher", "ClothingUniformJumpsuitTeacher,ClothingEyesGlassesTeacher,TeacherRuler")]
    [TestCase("KitTeacherDress", "ClothingUniformJumpskirtTeacher,ClothingEyesGlassesTeacher,TeacherRuler")]
    [TestCase("KitNurse", "ClothingUniformJumpsuitNurse,ClothingHeadHatNurseCap")]
    [TestCase("KitPupil", "ClothingUniformJumpsuitPupil,ClothingShoesPupil,ClothingSocksPupil")]
    [TestCase("KitRabbit", "ClothingUniformJumpsuitRabbit,ClothingMaskRabbit,ClothingSocksRabbitTights")]
    [TestCase("KitSexyMaid", "ClothingUniformJumpskirtSexyMaid,MaidPanicle")]
    public async Task VendingKitUnpacksOriginalContents(string prototype, string contents)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var inventory = Server.ResolveDependency<IPrototypeManager>().Index(Inventory);
            Assert.That(inventory.StartingInventory[new EntProtoId(prototype)], Is.EqualTo(3));
            var actor = SSpawnAtPosition("MobHuman", map.GridCoords);
            var kit = SSpawnAtPosition(prototype, map.GridCoords);
            var expected = contents.Split(',');
            var before = expected.ToDictionary(id => id, Count);
            var use = new UseInHandEvent(actor);
            SEntMan.EventBus.RaiseLocalEvent(kit, use);
            Assert.That(use.Handled, Is.True);
            foreach (var id in expected)
                Assert.That(Count(id), Is.EqualTo(before[id] + 1), $"{prototype}: {id}");
        });
    }

    private int Count(string prototype) => SEntMan.EntityQuery<MetaDataComponent>()
        .Count(meta => meta.EntityPrototype?.ID == prototype);
}
// Arcane-End
