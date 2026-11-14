using System.Linq;
using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Ninja.Systems;
using Content.Server.Atmos.Components;
using Content.Server.Body.Systems;
using Content.Shared.Antag;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Ninja.Components;
using Content.Shared.Projectiles;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Stealth.Components;
using Content.Shared.Storage;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class NinjaModLoadoutTest : InteractionTest
{
    private static readonly ProtoId<AntagSpecifierPrototype> Ninja = "SpaceNinja";
    private static readonly ProtoId<StartingGearPrototype> LegacyGear = "SpaceNinjaGear";
    private static readonly ProtoId<InventorySlotPrototype> OuterClothing = "outerClothing";
    private static readonly ProtoId<ReagentPrototype> Radium = "Radium";
    private static readonly ProtoId<ReagentPrototype> Water = "Water";

    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          id: NinjaStarTestMob
          parent: MobHuman
          components:
          - type: Battery
            maxCharge: 10000
            startingCharge: 10000

        - type: entity
          id: NinjaStarTestDevice
          parent: PowerCellHigh
          components:
          - type: Battery
            maxCharge: 10000
            startingCharge: 10000
        """;

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
            var hands = SEntMan.System<SharedHandsSystem>();
            var scroll = hands.EnumerateHeld(ninja).Single(item =>
                SEntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID != "JetpackBlackFilled");
            Assert.That(hands.TryDrop(ninja, scroll), Is.True);
            var starModule = suit.Item2.ModulesContainer!.ContainedEntities.Single(item =>
                SEntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID == "ModsuitModuleNinjaStarDispenser");
            var dispense = new ModModuleActionEvent { Performer = ninja };
            SEntMan.EventBus.RaiseLocalEvent(starModule, dispense);
            Assert.That(dispense.Handled, Is.True);
            Assert.That(hands.EnumerateHeld(ninja)
                .Any(item => SEntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID == "ThrowingStarNinjaEnergy"), Is.True);
            Assert.That(SEntMan.GetComponent<NinjaSuitComponent>(control).EmpActionEntity, Is.Null);
            Assert.That(SEntMan.System<ItemToggleSystem>().TryActivate(control, ninja), Is.True);
            Assert.That(SEntMan.HasComponent<StealthComponent>(ninja), Is.True);
            SEntMan.System<SharedBatterySystem>().SetCharge(battery.AsNullable(), 0);
            Assert.That(SEntMan.HasComponent<StealthComponent>(ninja), Is.False);
        });
    }

    [Test]
    public async Task KatanaRecallsByFlightOrByTeleportingIntoSheath()
    {
        EntityUid ninja = default;
        EntityUid control = default;
        EntityUid sheath = default;
        EntityUid blade = default;
        EntityUid module = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            ninja = SEntMan.SpawnAttachedTo("MobHuman", position);
            var role = SEntMan.AddComponent<SpaceNinjaComponent>(ninja);
            var specifier = Server.ResolveDependency<IPrototypeManager>().Index(Ninja);
            SEntMan.System<LoadoutSystem>().Equip(ninja, [specifier.StartingGear!.Value], specifier.RoleLoadout);
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryGetSlotEntity(ninja, "back", out var back), Is.True);
            Assert.That(inventory.TryGetSlotEntity(ninja, "belt", out var belt), Is.True);
            control = back!.Value;
            sheath = belt!.Value;
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            module = suit.Item2.ModulesContainer!.ContainedEntities.Single(item =>
                SEntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID == "ModsuitModuleNinjaRecall");
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryEject(sheath, "item", null, out var sword), Is.True);
            Assert.That(role.Katana, Is.EqualTo(sword));
            blade = sword!.Value;
            var xform = SEntMan.System<SharedTransformSystem>();
            xform.SetCoordinates(blade, position.Offset(new Vector2(2, 0)));
            var battery = SEntMan.System<ModSuitSystem>().GetBattery(suit)!.Value.Owner;
            var batteries = SEntMan.System<SharedBatterySystem>();
            var charge = batteries.GetCharge(battery).Charge;
            var recall = new ModModuleActionEvent { Performer = ninja };
            SEntMan.EventBus.RaiseLocalEvent(module, recall);
            Assert.That(recall.Handled, Is.True);
            Assert.That(SEntMan.TryGetComponent<ThrownItemComponent>(blade, out var thrown), Is.True);
            Assert.That(batteries.GetCharge(battery).Charge, Is.EqualTo(charge));

            SEntMan.System<ThrownItemSystem>().ThrowCollideInteraction(thrown!, blade, ninja);
            Assert.That(slots.GetItemOrNull(sheath, "item"), Is.EqualTo(blade));
        });

        await RunSeconds(0.6f);
        await Server.WaitAssertion(() =>
        {
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryEject(sheath, "item", null, out _), Is.True);
            var xform = SEntMan.System<SharedTransformSystem>();
            xform.SetCoordinates(blade, xform.GetMoverCoordinates(ninja).Offset(new Vector2(10, 0)));
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var battery = SEntMan.System<ModSuitSystem>().GetBattery(suit)!.Value.Owner;
            var batteries = SEntMan.System<SharedBatterySystem>();
            var before = batteries.GetCharge(battery).Charge;
            var recall = new ModModuleActionEvent { Performer = ninja };
            SEntMan.EventBus.RaiseLocalEvent(module, recall);
            Assert.That(recall.Handled, Is.True);
            Assert.That(slots.GetItemOrNull(sheath, "item"), Is.EqualTo(blade));
            Assert.That(batteries.GetCharge(battery).Charge, Is.EqualTo(before - 36f).Within(0.01f));
        });
    }

    [Test]
    public async Task EnergyNetReleasesTargetWhenSuitLosesPower()
    {
        EntityUid ninja = default;
        EntityUid target = default;
        EntityUid net = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            ninja = SEntMan.SpawnAttachedTo("MobHuman", position);
            var destination = position.Offset(new Vector2(2, 0));
            target = SEntMan.SpawnAttachedTo("MobHuman", destination);
            SEntMan.AddComponent<SpaceNinjaComponent>(ninja);
            var specifier = Server.ResolveDependency<IPrototypeManager>().Index(Ninja);
            SEntMan.System<LoadoutSystem>().Equip(ninja, [specifier.StartingGear!.Value], specifier.RoleLoadout);
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(ninja, "back", out var back), Is.True);
            var control = back!.Value;
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var module = suit.Item2.ModulesContainer!.ContainedEntities.Single(item =>
                SEntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID == "ModsuitModuleNinjaEnergyNet");

            var shoot = new ModNinjaNetActionEvent { Performer = ninja, Target = destination };
            SEntMan.EventBus.RaiseLocalEvent(module, shoot);
            Assert.That(shoot.Handled, Is.True);
            var projectiles = SEntMan.EntityQuery<MetaDataComponent>();
            var projectile = projectiles.Single(item => item.EntityPrototype?.ID == "NinjaNetProjectile").Owner;
            var hit = new ProjectileHitEvent(new DamageSpecifier(), target, ninja);
            SEntMan.EventBus.RaiseLocalEvent(projectile, ref hit);
            net = SEntMan.GetComponent<TransformComponent>(target).ParentUid;
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(net).EntityPrototype?.ID, Is.EqualTo("NinjaEnergyNet"));
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryUnbuckle(target, target, popup: false), Is.False);
            SEntMan.DeleteEntity(projectile);

            var coreBattery = SEntMan.System<ModSuitSystem>().GetBattery(suit)!.Value;
            SEntMan.System<SharedBatterySystem>().SetCharge(coreBattery.AsNullable(), 0);
        });

        await RunTicksSync(3);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(net), Is.False);
            Assert.That(SEntMan.EntityExists(target), Is.True);
            Assert.That(SEntMan.GetComponent<BuckleComponent>(target).Buckled, Is.False);
        });
    }

    [Test]
    public async Task EmergencyStimulantUsesOneDoseAndRefillsFromMixedSolution()
    {
        EntityUid ninja = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            ninja = SEntMan.SpawnAttachedTo("MobHuman", position);
            SEntMan.AddComponent<SpaceNinjaComponent>(ninja);
            var specifier = Server.ResolveDependency<IPrototypeManager>().Index(Ninja);
            SEntMan.System<LoadoutSystem>().Equip(ninja, [specifier.StartingGear!.Value], specifier.RoleLoadout);
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryGetSlotEntity(ninja, "back", out var back), Is.True);
            var control = back!.Value;
            var modules = SEntMan.GetComponent<ModSuitComponent>(control).ModulesContainer!;
            var module = modules.ContainedEntities.Single(item =>
                SEntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID == "ModsuitModuleNinjaStimulant");
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(module, "radium", out _, out var fuel), Is.True);
            var stamina = SEntMan.System<SharedStaminaSystem>();
            stamina.TakeStaminaDamage(ninja, 50f);
            Assert.That(SEntMan.System<SharedStunSystem>().TryAddParalyzeDuration(ninja, TimeSpan.FromSeconds(10)), Is.True);
            var dose = new ModModuleActionEvent { Performer = ninja };
            SEntMan.EventBus.RaiseLocalEvent(module, dose);
            Assert.That(dose.Handled, Is.True);
            Assert.That(stamina.GetStaminaDamage(ninja), Is.Zero);
            Assert.That(fuel!.Volume.Float(), Is.Zero);

            var beaker = SEntMan.SpawnAttachedTo("Beaker", position);
            Assert.That(solutions.TryGetDrainableSolution(beaker, out var source, out var mixture), Is.True);
            Assert.That(solutions.TryAddReagent(source!.Value, Radium.Id, 20), Is.True);
            Assert.That(solutions.TryAddReagent(source.Value, Water.Id, 5), Is.True);
            var refill = new InteractUsingEvent(ninja, beaker, control, position);
            SEntMan.EventBus.RaiseLocalEvent(control, refill);
            Assert.That(refill.Handled, Is.True);
            Assert.That(fuel.Volume.Float(), Is.EqualTo(20));
            Assert.That(mixture!.GetReagentQuantity(new ReagentId(Water.Id, null)).Float(), Is.EqualTo(5));
        });

        await RunTicksSync(3);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<StunnedComponent>(ninja), Is.False));
    }

    [Test]
    public async Task EnergyStarPulsesOnImpactAndOnlyRepeatsWhileEmbedded()
    {
        EntityUid star = default;
        EntityUid mob = default;
        EntityUid device = default;
        EntityUid deviceStar = default;
        float deviceCharge = 0;

        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            mob = SEntMan.SpawnAttachedTo("NinjaStarTestMob", position);
            device = SEntMan.SpawnAttachedTo("NinjaStarTestDevice", position);
            star = SEntMan.SpawnAttachedTo("ThrowingStarNinjaEnergy", position);
            deviceStar = SEntMan.SpawnAttachedTo("ThrowingStarNinjaEnergy", position);
            var batteries = SEntMan.System<SharedBatterySystem>();
            var throws = SEntMan.System<ThrownItemSystem>();
            deviceCharge = batteries.GetCharge(device).Charge;

            throws.ThrowCollideInteraction(new ThrownItemComponent(), star, mob);
            throws.ThrowCollideInteraction(new ThrownItemComponent(), deviceStar, device);

            Assert.That(SEntMan.GetComponent<EmbeddableProjectileComponent>(star).EmbeddedIntoUid, Is.EqualTo(mob));
            Assert.That(batteries.GetCharge(mob).Charge, Is.EqualTo(7500));
            Assert.That(batteries.GetCharge(device).Charge, Is.EqualTo(deviceCharge - 2500));
            Assert.That(SEntMan.IsQueuedForDeletion(deviceStar), Is.True);
        });

        await RunSeconds(6.1f);
        await Server.WaitAssertion(() =>
        {
            var batteries = SEntMan.System<SharedBatterySystem>();
            Assert.That(batteries.GetCharge(mob).Charge, Is.EqualTo(6750));
            Assert.That(batteries.GetCharge(device).Charge, Is.EqualTo(deviceCharge - 2500));
            Assert.That(SEntMan.EntityExists(deviceStar), Is.False);
            SEntMan.System<SharedProjectileSystem>().EmbedDetach(star, SEntMan.GetComponent<EmbeddableProjectileComponent>(star));
        });

        await RunSeconds(6.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(star), Is.False);
            Assert.That(SEntMan.System<SharedBatterySystem>().GetCharge(mob).Charge, Is.EqualTo(6750));
        });
    }
}
