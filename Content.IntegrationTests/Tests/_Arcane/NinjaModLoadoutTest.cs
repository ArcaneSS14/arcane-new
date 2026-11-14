using System.Linq;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Ninja.Systems;
using Content.Server.Atmos.Components;
using Content.Server.Body.Systems;
using Content.Shared.Antag;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Ninja.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Stealth.Components;
using Content.Shared.Storage;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class NinjaModLoadoutTest : InteractionTest
{
    private static readonly ProtoId<AntagSpecifierPrototype> Ninja = "SpaceNinja";
    private static readonly ProtoId<StartingGearPrototype> LegacyGear = "SpaceNinjaGear";
    private static readonly ProtoId<InventorySlotPrototype> OuterClothing = "outerClothing";

    [Test]
    public async Task NinjaStartsWithSealedModAndWorkingInternals()
    {
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            var ninja = SEntMan.SpawnAttachedTo("MobHuman", position);
            SEntMan.AddComponent<SpaceNinjaComponent>(ninja);
            var prototypes = Server.ResolveDependency<IPrototypeManager>();
            var specifier = prototypes.Index(Ninja);
            Assert.That(specifier.StartingGear?.Id, Is.EqualTo("SpaceNinjaModGear"));
            SEntMan.System<LoadoutSystem>().Equip(ninja, [specifier.StartingGear!.Value], specifier.RoleLoadout);

            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryGetSlotEntity(ninja, "back", out var back), Is.True);
            var control = back!.Value;
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var mods = SEntMan.System<ModSuitSystem>();
            Assert.That(suit.Item2.State, Is.EqualTo(ModSuitState.Active));
            Assert.That(suit.Item2.PartEntities.Count, Is.EqualTo(4));
            foreach (var (slot, part) in suit.Item2.PartEntities)
            {
                Assert.That(mods.IsPartDeployed(suit, slot), Is.True, slot);
                Assert.That(SEntMan.GetComponent<ModPartComponent>(part).Sealed, Is.True, slot);
            }

            Assert.That(inventory.TryGetSlotEntity(ninja, "suitstorage", out var tank), Is.True);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(tank!.Value).EntityPrototype?.ID, Is.EqualTo("OxygenTankFilled"));
            var internals = SEntMan.GetComponent<InternalsComponent>(ninja);
            Assert.That(SEntMan.System<InternalsSystem>().AreInternalsWorking(internals), Is.True);
            Assert.That(SEntMan.HasComponent<GasTankComponent>(internals.GasTankEntity), Is.True);
            Assert.That(SEntMan.HasComponent<PressureProtectionComponent>(suit.Item2.PartEntities["head"]), Is.True);
            Assert.That(SEntMan.HasComponent<PressureProtectionComponent>(suit.Item2.PartEntities["outerClothing"]), Is.True);
            Assert.That(SEntMan.HasComponent<NinjaGlovesComponent>(suit.Item2.PartEntities["gloves"]), Is.True);
            Assert.That(SEntMan.GetComponent<StorageComponent>(control).Container.ContainedEntities
                .Any(item => SEntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID == "Multitool"), Is.True);

            var legacy = prototypes.Index(LegacyGear);
            Assert.That(legacy.Equipment[OuterClothing].Id, Is.EqualTo("ClothingOuterSuitSpaceNinja"));
            var battery = mods.GetBattery(suit)!.Value;
            Assert.That(SEntMan.System<SpaceNinjaSystem>().GetNinjaBattery(ninja, out var ninjaBattery, out _), Is.True);
            Assert.That(ninjaBattery, Is.EqualTo(battery.Owner));
            Assert.That(SEntMan.GetComponent<NinjaSuitComponent>(control).EmpActionEntity, Is.Null);
            Assert.That(SEntMan.System<ItemToggleSystem>().TryActivate(control, ninja), Is.True);
            Assert.That(SEntMan.HasComponent<StealthComponent>(ninja), Is.True);
            SEntMan.System<SharedBatterySystem>().SetCharge(battery.AsNullable(), 0);
            Assert.That(SEntMan.HasComponent<StealthComponent>(ninja), Is.False);
        });
    }
}
