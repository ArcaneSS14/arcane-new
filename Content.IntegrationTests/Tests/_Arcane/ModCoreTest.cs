using System.Linq;
using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Common.Body;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Cloning;
using Content.Server.Construction;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Inventory;
using Content.Shared.Actions;
using Content.Shared.Armor;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Cuffs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Instruments;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.NightVision;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Projectiles;
using Content.Shared.Slippery;
using Content.Shared.Speech.Components;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Stealth.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wires;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ModCoreTest : InteractionTest
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: modTheme
          id: TestModTheme
          chargeDrain: 0.8
          activationStepTime: 0.25

        - type: entity
          id: TestModPassive
          components:
          - type: ModModule
            complexity: 1
            idlePower: 0.25
            activePower: 0.5
            requiredParts: [ head ]

        - type: entity
          id: TestModMountless
          components:
          - type: ModModule
            requiredParts: [ eyes ]

        - type: entity
          parent: ClothingHeadHelmetBasic
          id: TestModHelmet
          components:
          - type: ModPart
            sealedComponents:
            - type: NoSlip
            - type: IngestionBlocker
          - type: PointLight
            enabled: true
            radius: 2

        - type: entity
          parent: ClothingBackpack
          id: TestModControlUnit
          components:
          - type: ModSuit
            theme: TestModTheme
            parts:
              head: TestModHelmet
          - type: ItemSlots
            slots:
              mod-core:
                whitelist:
                  components: [ ModCore ]
          - type: WiresPanel
          - type: ContainerContainer
            containers:
              mod-core: !type:ContainerSlot

        - type: entity
          parent: TestModControlUnit
          id: TestActiveModControlUnit
          components:
          - type: ModSuit
            state: Active
        """;

    [TestCase("ModsuitDeviceMister")]
    [TestCase("ModsuitDeviceJanitorMister")]
    public async Task ModSprayersRegenerateTheirOwnReservoir(string prototype)
    {
        EntityUid device = default;
        await Server.WaitAssertion(() =>
        {
            device = SEntMan.SpawnAttachedTo(prototype, MapData.GridCoords);
            var chemistry = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(chemistry.TryGetSolution(device, "spray", out var reservoir, out var solution), Is.True);
            Assert.That(solution!.Volume.Float(), Is.EqualTo(100f));
            chemistry.SplitSolution(reservoir!.Value, 10);
        });

        await RunSeconds(1.2f);
        await Server.WaitAssertion(() =>
        {
            var chemistry = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(chemistry.TryGetSolution(device, "spray", out _, out var solution), Is.True);
            Assert.That(solution!.Volume.Float(), Is.EqualTo(95f));
        });
    }

    [Test]
    public async Task MouthholeAllowsIngestionUntilItsSuitRunsOutOfPower()
    {
        EntityUid wearer = default;
        EntityUid control = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            control = SEntMan.SpawnAttachedTo("TestModControlUnit", position);
            var module = SEntMan.SpawnAttachedTo("ModsuitModuleMouthhole", position);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", position);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);
            var wires = SEntMan.System<SharedWiresSystem>();

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(control, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var ingestion = SEntMan.System<IngestionSystem>();
            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            Assert.That(ingestion.HasMouthAvailable(wearer), Is.True);
            SEntMan.System<SharedBatterySystem>().SetCharge(battery, 0);
            Assert.That(ingestion.HasMouthAvailable(wearer), Is.False);
        });
    }

    [Test]
    public async Task ArmorModulesRespectCoveragePenetrationAndPower()
    {
        EntityUid wearer = default;
        EntityUid control = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            control = SEntMan.SpawnAttachedTo("TestModControlUnit", position);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", position);
            var head = SEntMan.SpawnAttachedTo("ModsuitModuleHeadProtector", position);
            var shielding = SEntMan.SpawnAttachedTo("ModsuitModuleRadProtection", position);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);
            var wires = SEntMan.System<SharedWiresSystem>();

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(control, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, head, wearer), Is.True);
            Assert.That(suits.TryInstallModule(suit, shielding, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            const string blunt = "Blunt";
            const string radiation = "Radiation";
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryGetSlotEntity(wearer, "head", out var helmet), Is.True);
            var armor = SEntMan.GetComponent<ArmorComponent>(helmet!.Value);
            armor.Modifiers = new DamageModifierSet
            {
                Coefficients = new() { [blunt] = 0.8f },
                FlatReductions = new() { [blunt] = 4f },
            };

            float Damage(string type, BodyPartType part, float penetration)
            {
                var specifier = new DamageSpecifier
                {
                    ArmorPenetration = penetration,
                    DamageDict = { [type] = 100 },
                };
                var ev = new DamageModifyEvent(wearer, specifier, targetPart: part);
                SEntMan.EventBus.RaiseLocalEvent(wearer, ev);
                return ev.Damage.DamageDict[type].Float();
            }

            Assert.That(Damage(blunt, BodyPartType.Head, 0.5f), Is.EqualTo(98f * MathF.Sqrt(0.8f * 0.9f)).Within(0.02f));
            Assert.That(Damage(blunt, BodyPartType.Head, 1f), Is.EqualTo(100f));
            Assert.That(Damage(blunt, BodyPartType.Torso, 0f), Is.EqualTo(100f));
            Assert.That(Damage(radiation, BodyPartType.Torso, 1f), Is.EqualTo(20f));
            Assert.That(armor.Modifiers.Coefficients[blunt], Is.EqualTo(0.8f));

            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            SEntMan.System<SharedBatterySystem>().SetCharge(battery, 0);
            Assert.That(Damage(blunt, BodyPartType.Head, 0f), Is.EqualTo(76.8f).Within(0.02f));
            Assert.That(Damage(radiation, BodyPartType.Torso, 1f), Is.EqualTo(100f));
        });
    }

    [Test]
    public async Task ArmorBoosterSuppressesPressureProtectionOnlyWhilePowered()
    {
        EntityUid wearer = default;
        EntityUid control = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            control = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", position);
            var booster = SEntMan.SpawnAttachedTo("ModsuitModuleArmorBooster", position);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);

            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, booster, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var battery = SEntMan.System<ModSuitSystem>().GetBattery(suit)!.Value.AsNullable();
            var pressure = SEntMan.System<BarotraumaSystem>();
            var barotrauma = SEntMan.GetComponent<BarotraumaComponent>(wearer);
            Assert.That(pressure.GetFeltLowPressure(wearer, barotrauma, 0f), Is.Zero);
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(wearer, "head", out var helmet), Is.True);
            Assert.That(SEntMan.HasComponent<PressureProtectionComponent>(helmet!.Value), Is.True);
            SEntMan.System<SharedBatterySystem>().SetCharge(battery, 0);
            Assert.That(pressure.GetFeltLowPressure(wearer, barotrauma, 0f), Is.GreaterThan(0f));
        });
    }

    [Test]
    public async Task BalloonDispenserCreatesOnlyCashlessBalloons()
    {
        EntityUid wearer = default;
        EntityUid control = default;
        EntityUid blower = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            control = SEntMan.SpawnAttachedTo("TestModControlUnit", position);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", position);
            blower = SEntMan.SpawnAttachedTo("ModsuitModuleBalloonBlower", position);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(control, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, blower, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var battery = SEntMan.System<ModSuitSystem>().GetBattery(suit)!.Value.AsNullable();
            var charge = SEntMan.System<SharedBatterySystem>();
            var before = charge.GetCharge(battery);
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(blower, action);
            Assert.That(action.Handled, Is.True);
            Assert.That(charge.GetCharge(battery), Is.EqualTo(before - 5f).Within(0.01f));

            string[] variants = ["ModsuitBalloonRed", "ModsuitBalloonBlue", "ModsuitBalloonGreen"];
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.TryGetActiveItem(wearer, out var item), Is.True);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(item.Value).EntityPrototype?.ID, Is.AnyOf(variants));
            foreach (var variant in variants)
            {
                var balloon = SEntMan.SpawnAttachedTo(variant, MapData.GridCoords);
                var destruction = SEntMan.GetComponent<DestructibleComponent>(balloon);
                Assert.That(destruction.Thresholds.SelectMany(threshold => threshold.Behaviors),
                    Has.None.TypeOf<SpawnEntitiesBehavior>(), variant);
            }
        });
    }

    [Test]
    public async Task EmpShieldScopeFollowsModuleAndSuitState()
    {
        EntityUid wearer = default;
        EntityUid control = default;
        EntityUid advanced = default;
        EntityUid held = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            control = SEntMan.SpawnAttachedTo("TestModControlUnit", position);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", position);
            var standard = SEntMan.SpawnAttachedTo("ModsuitModuleEmpShield", position);
            advanced = SEntMan.SpawnAttachedTo("ModsuitModuleEmpShieldAdvanced", position);
            held = SEntMan.SpawnAttachedTo("PowerCellHigh", position);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(control, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryPickupAnyHand(wearer, held), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, standard, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suits = SEntMan.System<ModSuitSystem>();
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            var emp = SEntMan.System<SharedEmpSystem>();
            Assert.That(emp.TryEmpEffects(battery.Owner, 100f, TimeSpan.Zero), Is.False);
            Assert.That(emp.TryEmpEffects(held, 100f, TimeSpan.Zero), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suits = SEntMan.System<ModSuitSystem>();
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            var charge = SEntMan.System<SharedBatterySystem>();
            var before = charge.GetCharge(battery);
            SEntMan.System<SharedEmpSystem>().EmpPulse(MapData.GridCoords, 1f, 100f, TimeSpan.Zero, predicted: false);
            Assert.That(charge.GetCharge(battery), Is.EqualTo(before - 100f).Within(0.01f));

            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, advanced, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suits = SEntMan.System<ModSuitSystem>();
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            var emp = SEntMan.System<SharedEmpSystem>();
            Assert.That(emp.TryEmpEffects(battery.Owner, 100f, TimeSpan.Zero), Is.False);
            Assert.That(emp.TryEmpEffects(held, 100f, TimeSpan.Zero), Is.False);
            SEntMan.System<SharedBatterySystem>().SetCharge(battery, 0);
            Assert.That(emp.TryEmpEffects(held, 100f, TimeSpan.Zero), Is.True);
        });
    }

    [Test]
    public async Task DeviceReturnsToItsModuleAndIsDeletedWithIt()
    {
        EntityUid wearer = default;
        EntityUid module = default;
        EntityUid device = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            var unit = SEntMan.SpawnAttachedTo("TestModControlUnit", position);
            module = SEntMan.SpawnAttachedTo("ModsuitModuleGps", position);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", position);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(unit, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, unit, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, action);
            Assert.That(action.Handled, Is.True);
            device = SEntMan.GetComponent<ModDeviceModuleComponent>(module).DeviceEntity!.Value;
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.IsHolding(wearer, device), Is.True);
            Assert.That(hands.TryDrop(wearer, device), Is.True);
            Assert.That(SEntMan.GetComponent<ModModuleComponent>(module).Active, Is.False);
        });

        await RunSeconds(0.25f);
        await Server.WaitAssertion(() =>
        {
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainingContainer(device, out var stored), Is.True);
            Assert.That(stored!.Owner, Is.EqualTo(module));
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, action);
            Assert.That(action.Handled, Is.True);
            Assert.That(SEntMan.System<SharedHandsSystem>().IsHolding(wearer, device), Is.True);
            SEntMan.DeleteEntity(module);
        });

        await RunSeconds(0.25f);
        await Server.WaitAssertion(() => Assert.That(SEntMan.EntityExists(device), Is.False));
    }

    [Test]
    public async Task MedicalVisorAndWeldingModuleReleaseOnlyTheirOwnEffects()
    {
        EntityUid wearer = default;
        EntityUid unit = default;
        EntityUid medical = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            unit = SEntMan.SpawnAttachedTo("TestModControlUnit", position);
            medical = SEntMan.SpawnAttachedTo("ModsuitModuleVisorMedical", position);
            var security = SEntMan.SpawnAttachedTo("ModsuitModuleVisorSecurity", position);
            var welding = SEntMan.SpawnAttachedTo("ModsuitModuleWelding", position);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", position);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);
            var wires = SEntMan.System<SharedWiresSystem>();

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(unit, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, unit, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, medical, wearer), Is.True);
            Assert.That(suits.TryInstallModule(suit, security, wearer), Is.False);
            Assert.That(suits.TryInstallModule(suit, welding, wearer), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(wearer, "head", out var head), Is.True);
            var helmet = head!.Value;
            Assert.That(SEntMan.HasComponent<EyeProtectionComponent>(helmet), Is.True);
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(medical, action);
            Assert.That(action.Handled, Is.True);
            Assert.That(SEntMan.HasComponent<Content.Shared.Overlays.ShowHealthBarsComponent>(helmet), Is.True);
            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            SEntMan.System<SharedBatterySystem>().SetCharge(battery, 0);
            Assert.That(SEntMan.HasComponent<EyeProtectionComponent>(helmet), Is.False);
            Assert.That(SEntMan.HasComponent<Content.Shared.Overlays.ShowHealthBarsComponent>(helmet), Is.False);
            Assert.That(SEntMan.HasComponent<NoSlipComponent>(helmet), Is.True);
        });
    }

    [Test]
    public async Task OreBagCollectsOnlyWhileActiveAndSpillsWhenRemoved()
    {
        EntityUid wearer = default;
        EntityUid unit = default;
        EntityUid ore = default;
        EntityUid bag = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            unit = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", position);
            ore = SEntMan.SpawnAttachedTo("SteelOre1", position);
            bag = SEntMan.SpawnAttachedTo("ModsuitModuleOreBag", position);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);

            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, unit, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, bag, wearer), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, false), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var storage = SEntMan.GetComponent<StorageComponent>(bag);
            Assert.That(storage.Container.Contains(ore), Is.False);
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(bag, action);
            Assert.That(action.Handled, Is.True);
            SEntMan.System<SharedPhysicsSystem>().SetBodyStatus(ore, SEntMan.GetComponent<PhysicsComponent>(ore), BodyStatus.OnGround);
            Assert.That(SEntMan.HasComponent<MagnetPickupComponent>(bag), Is.True);
            var lookup = SEntMan.System<EntityLookupSystem>();
            Assert.That(lookup.GetEntitiesInRange(bag, 1f, LookupFlags.Dynamic | LookupFlags.Sundries), Does.Contain(ore));
            var storageSystem = SEntMan.System<SharedStorageSystem>();
            Assert.That(storageSystem.CanInsert(bag, ore, out var reason), Is.True, reason);
            SEntMan.System<MagnetPickupSystem>().Update(0);
            Assert.That(storage.Container.Contains(ore), Is.True);
        });

        await RunSeconds(1.2f);
        await Server.WaitAssertion(() =>
        {
            var storage = SEntMan.GetComponent<StorageComponent>(bag);
            Assert.That(storage.Container.Contains(ore), Is.True);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            Assert.That(SEntMan.System<ModSuitSystem>().TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);
            Assert.That(SEntMan.System<SharedWiresSystem>().TogglePanel(unit, panel, true), Is.True);
            Assert.That(SEntMan.System<ModSuitSystem>().TryRemoveModule(suit, bag, wearer), Is.True);
            Assert.That(SEntMan.GetComponent<StorageComponent>(bag).Container.Contains(ore), Is.False);
            Assert.That(SEntMan.EntityExists(ore), Is.True);
        });
    }

    [Test]
    public async Task ClampReleasesOriginalCargoWhenRetracted()
    {
        EntityUid wearer = default;
        EntityUid module = default;
        EntityUid cargo = default;
        EntityUid device = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            var control = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", position);
            module = SEntMan.SpawnAttachedTo("ModsuitModuleClamp", position);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);

            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, action);
            Assert.That(action.Handled, Is.True);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryGetActiveItem(wearer, out var item), Is.True);
            device = item!.Value;

            var coordinates = MapData.GridCoords;
            var targetCoordinates = coordinates.Offset(new System.Numerics.Vector2(0.9f, 0f));
            cargo = SEntMan.SpawnAttachedTo("CrateGenericSteel", targetCoordinates);
            var grab = new AfterInteractEvent(wearer, device, cargo, targetCoordinates, canReach: true);
            SEntMan.EventBus.RaiseLocalEvent(device, grab);
            Assert.That(grab.Handled, Is.True);
            Assert.That(SEntMan.GetComponent<DoAfterComponent>(wearer).DoAfters.Values.Any(after =>
                after.Args.Event is ModClampGrabDoAfterEvent), Is.True);
        });

        await RunSeconds(3);
        await Server.WaitAssertion(() =>
        {
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(device, "mod-clamp", out var storage), Is.True);
            Assert.That(storage!.Contains(cargo), Is.True);

            var retract = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, retract);
            Assert.That(retract.Handled, Is.True);
            Assert.That(storage.Contains(cargo), Is.False);
            Assert.That(SEntMan.EntityExists(cargo), Is.True);
            Assert.That(containers.TryGetContainer(module, "mod-device", out var deviceSlot), Is.True);
            Assert.That(deviceSlot!.Contains(device), Is.True);
        });
    }

    [Test]
    public async Task CorporateSphereRestoresWearerAndEquipmentOnPowerLoss()
    {
        await AddGravity();
        EntityUid wearer = default;
        Entity<ModSuitComponent> suit = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            var control = SEntMan.SpawnAttachedTo("ClothingModsuitCorporate", position);
            suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var module = suit.Comp.ModulesContainer!.ContainedEntities.Single(uid =>
                SEntMan.HasComponent<ModSphereTransformModuleComponent>(uid));
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, action);
            Assert.That(action.Handled, Is.True);
            var form = SEntMan.GetComponent<ModSphereFormComponent>(wearer);
            Assert.That(form.PrimaryColor, Is.EqualTo(Color.FromHex("#254022")));
            var missileAction = SEntMan.GetComponent<ModSphereTransformModuleComponent>(module).MissileActionEntity!.Value;
            Assert.That(SEntMan.System<SharedActionsSystem>().GetAction(missileAction)?.Comp.AttachedEntity, Is.EqualTo(wearer));

            var off = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, off);
            Assert.That(off.Handled, Is.True);
            Assert.That(SEntMan.HasComponent<ModSphereFormComponent>(wearer), Is.False);
        });

        await RunSeconds(2);
        await Server.WaitAssertion(() =>
        {
            var module = suit.Comp.ModulesContainer!.ContainedEntities.Single(uid =>
                SEntMan.HasComponent<ModSphereTransformModuleComponent>(uid));
            var on = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, on);
            Assert.That(on.Handled, Is.True);
            var battery = SEntMan.System<ModSuitSystem>().GetBattery(suit)!.Value;
            SEntMan.System<SharedBatterySystem>().SetCharge(battery.AsNullable(), 0);
            Assert.That(SEntMan.HasComponent<ModSphereFormComponent>(wearer), Is.False);
            var missileAction = SEntMan.GetComponent<ModSphereTransformModuleComponent>(module).MissileActionEntity!.Value;
            Assert.That(SEntMan.System<SharedActionsSystem>().GetAction(missileAction)?.Comp.AttachedEntity, Is.Null);
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(wearer, "head", out var helmet), Is.True);
            Assert.That(helmet, Is.EqualTo(suit.Comp.PartEntities["head"]));
        });
    }

    [TestCase("ModsuitModulePatientTransport", false)]
    [TestCase("ModsuitModuleCriminalCapture", true)]
    public async Task TransportReleasesOriginalOccupantOnDeviceRetraction(string prototype, bool capture)
    {
        await AddGravity();
        EntityUid wearer = default;
        EntityUid module = default;
        EntityUid target = default;
        EntityUid projector = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            var control = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", position);
            module = SEntMan.SpawnAttachedTo(prototype, position);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, action);
            Assert.That(action.Handled, Is.True);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryGetActiveItem(wearer, out var item), Is.True);
            projector = item!.Value;
            var position = MapData.GridCoords.Offset(new Vector2(0.9f, 0));
            target = SEntMan.SpawnAttachedTo("MobHuman", position);
            Assert.That(SEntMan.System<SharedCuffableSystem>().IsCuffed(target), Is.False);
            var use = new AfterInteractEvent(wearer, projector, target, position, canReach: true);
            SEntMan.EventBus.RaiseLocalEvent(projector, use);
            Assert.That(use.Handled, Is.True);
            Assert.That(SEntMan.GetComponent<DoAfterComponent>(wearer).DoAfters.Values.Any(after =>
                after.Args.Event is ModPatientTransportDoAfterEvent), Is.True);
        });

        await RunSeconds(5);
        await Server.WaitAssertion(() =>
        {
            var device = SEntMan.GetComponent<ModPatientTransportComponent>(projector);
            Assert.That(device.Bag, Is.Not.Null);
            var bag = device.Bag!.Value;
            Assert.That(SEntMan.GetComponent<EntityStorageComponent>(bag).Contents.Contains(target), Is.True);
            Assert.That(SEntMan.HasComponent<ModTransportStasisComponent>(target), Is.EqualTo(!capture));
            Assert.That(SEntMan.HasComponent<MumbleAccentComponent>(target), Is.EqualTo(capture));
            if (capture)
                Assert.That(SEntMan.System<SharedEntityStorageSystem>().TryOpenStorage(target, bag), Is.False);

            var retract = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, retract);
            Assert.That(retract.Handled, Is.True);
            Assert.That(device.Bag, Is.Null);
            Assert.That(SEntMan.EntityExists(target), Is.True);
            Assert.That(SEntMan.GetComponent<EntityStorageComponent>(bag).Contents.Contains(target), Is.False);
            Assert.That(SEntMan.HasComponent<ModTransportStasisComponent>(target), Is.False);
            Assert.That(SEntMan.HasComponent<MumbleAccentComponent>(target), Is.False);
        });
    }

    [Test]
    public async Task HatStabilizerReturnsTheSameHatWhenRemoved()
    {
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            var unit = SEntMan.SpawnAttachedTo("ClothingModsuitStandard", position);
            var wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            var module = SEntMan.SpawnAttachedTo("ModsuitModuleHatStabilizer", position);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var slots = SEntMan.System<ItemSlotsSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);
            Assert.That(inventory.TryEquip(wearer, unit, "back", force: true), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            var helmet = suit.Item2.PartEntities["head"];
            var hat = SEntMan.SpawnAttachedTo("ClothingHeadHatBeret", position);
            Assert.That(slots.TryInsert(helmet, ModHatHolderComponent.SlotId, hat, wearer), Is.True);
            Assert.That(slots.GetItemOrNull(helmet, ModHatHolderComponent.SlotId), Is.EqualTo(hat));
            Assert.That(suits.TryRemoveModule(suit, module, wearer), Is.True);
            Assert.That(SEntMan.EntityExists(hat), Is.True);
            Assert.That(SEntMan.System<SharedContainerSystem>().IsEntityInContainer(hat), Is.False);
            Assert.That(slots.TryGetSlot(helmet, ModHatHolderComponent.SlotId, out _), Is.False);
        });
    }

    [TestCase("ClothingModsuitContractor")]
    [TestCase("ClothingModsuitCorporate")]
    public async Task BuiltinInstrumentIsOnlyAvailableWithTheSealedHelmet(string prototype)
    {
        EntityUid wearer = default;
        Entity<ModSuitComponent> suit = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            var control = SEntMan.SpawnAttachedTo(prototype, position);
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            var suits = SEntMan.System<ModSuitSystem>();
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(SEntMan.HasComponent<Content.Server.Instruments.InstrumentComponent>(suit.Comp.PartEntities["head"]), Is.False);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var helmet = suit.Comp.PartEntities["head"];
            Assert.That(SEntMan.HasComponent<Content.Server.Instruments.InstrumentComponent>(helmet), Is.True);
            var module = suit.Comp.ModulesContainer!.ContainedEntities.Single(uid =>
                SEntMan.HasComponent<ModInstrumentModuleComponent>(uid));
            Assert.That(SEntMan.System<SharedUserInterfaceSystem>().HasUi(helmet, InstrumentUiKey.Key), Is.True);
            Assert.That(SEntMan.System<ModSuitSystem>().TryToggleActivation(suit, wearer), Is.True);
            Assert.That(SEntMan.EntityExists(module), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var helmet = suit.Comp.PartEntities["head"];
            Assert.That(SEntMan.HasComponent<Content.Server.Instruments.InstrumentComponent>(helmet), Is.False);
        });
    }

    [Test]
    public async Task GripModifiesEscapeWhileItsGauntletsArePowered()
    {
        EntityUid wearer = default;
        Entity<ModSuitComponent> suit = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            var control = SEntMan.SpawnAttachedTo("ClothingModsuitContractor", position);
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            var suits = SEntMan.System<ModSuitSystem>();
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var gloves = suit.Comp.PartEntities["gloves"];
            Assert.That(SEntMan.HasComponent<ModGripComponent>(gloves), Is.True);
            var target = SEntMan.SpawnAttachedTo("MobHuman", MapData.GridCoords.Offset(new Vector2(0.8f, 0)));
            var puller = SEntMan.GetComponent<PullerComponent>(wearer);
            var pullable = SEntMan.GetComponent<PullableComponent>(target);
            var pulling = SEntMan.System<PullingSystem>();
            Assert.That(pulling.TryStartPull(wearer, target, grabStageOverride: GrabStage.Hard), Is.True);
            Assert.That(pullable.GrabEscapeChance, Is.EqualTo(puller.EscapeChances[GrabStage.Hard] * 0.8f - 0.1f).Within(0.0001f));
            Assert.That(pulling.TryStopPull(target, pullable, wearer, ignoreGrab: true), Is.True);
            Assert.That(SEntMan.System<ModSuitSystem>().TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() => Assert.That(
            SEntMan.HasComponent<ModGripComponent>(suit.Comp.PartEntities["gloves"]), Is.False));
    }

    [Test]
    public async Task MagneticHarnessReturnsTheOriginalEquippedGun()
    {
        await AddGravity();
        EntityUid wearer = default;
        EntityUid gun = default;
        Entity<ModSuitComponent> suit = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            var control = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", position);
            var module = SEntMan.SpawnAttachedTo("ModsuitModuleMagneticHarness", position);
            suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);
            Assert.That(inventory.TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<ModMagneticHarnessComponent>(wearer), Is.True);
            var inventory = SEntMan.System<InventorySystem>();
            var position = MapData.GridCoords;
            gun = SEntMan.SpawnAttachedTo("WeaponShotgunKammerer", position);
            Assert.That(inventory.TryEquip(wearer, gun, "suitstorage", force: true), Is.True);
            Assert.That(SEntMan.GetComponent<ModMagnetizedGunComponent>(gun).Wearer, Is.EqualTo(wearer));
            Assert.That(inventory.TryUnequip(wearer, "suitstorage", force: true), Is.True);
            SEntMan.System<SharedTransformSystem>().SetCoordinates(gun, position.Offset(new Vector2(1f, 0)));
        });

        await RunSeconds(4);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(wearer, "suitstorage", out var returned), Is.True);
            Assert.That(returned, Is.EqualTo(gun));
        });
    }

    [Test]
    public async Task AttackingDisablesOnlyTheModCloak()
    {
        EntityUid wearer = default;
        EntityUid module = default;
        await Server.WaitAssertion(() =>
        {
            var at = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", at);
            var control = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", at);
            module = SEntMan.SpawnAttachedTo("ModsuitModuleCloak", at);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var toggle = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, toggle);
            Assert.That(toggle.Handled, Is.True);
            Assert.That(SEntMan.HasComponent<StealthComponent>(wearer), Is.True);
            var hit = new MeleeAttackEvent(wearer);
            SEntMan.EventBus.RaiseLocalEvent(wearer, ref hit);
            Assert.That(SEntMan.HasComponent<StealthComponent>(wearer), Is.False);
            Assert.That(SEntMan.HasComponent<ModCloakedComponent>(wearer), Is.False);
        });
    }

    [Test]
    public async Task ShootingAssistantReleasesGunModifiersWhenDropped()
    {
        EntityUid wearer = default;
        EntityUid module = default;
        var control = await EquipPoweredEngineerSuit("ModsuitModuleShootingAssistant", (user, installed) =>
        {
            wearer = user;
            module = installed;
        });

        await Server.WaitAssertion(() =>
        {
            var gun = SEntMan.SpawnAttachedTo("WeaponPistolMk58", MapData.GridCoords);
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(wearer, gun), Is.True);
            var stats = SEntMan.GetComponent<GunComponent>(gun);
            var fireRate = stats.FireRateModified;
            var projectileSpeed = stats.ProjectileSpeedModified;

            var cycle = new ModShootingModeActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, cycle);
            Assert.That(cycle.Handled, Is.True);
            Assert.That(stats.FireRateModified, Is.EqualTo(fireRate * 1.5f).Within(0.001f));
            cycle = new ModShootingModeActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, cycle);
            Assert.That(stats.ProjectileSpeedModified, Is.EqualTo(projectileSpeed * 1.3f).Within(0.001f));

            Assert.That(hands.TryDrop(wearer, gun), Is.True);
            Assert.That(stats.ProjectileSpeedModified, Is.EqualTo(projectileSpeed).Within(0.001f));
            Assert.That(stats.FireRateModified, Is.EqualTo(fireRate).Within(0.001f));
            var suits = SEntMan.System<ModSuitSystem>();
            Assert.That(suits.TryToggleActivation((control, SEntMan.GetComponent<ModSuitComponent>(control)), wearer), Is.True);
            Assert.That(SEntMan.HasComponent<ModShootingAssistantComponent>(wearer), Is.False);
        });
    }

    [Test]
    public async Task ProjectedModSuitKeepsSealedPartsWithoutProvidingAnyHardware()
    {
        EntityUid wearer = default;
        var control = await EquipPoweredEngineerSuit("ModsuitModuleKinesis", (user, _) => wearer = user);
        await Server.WaitAssertion(() =>
        {
            var at = MapData.GridCoords;
            var clone = SEntMan.SpawnAttachedTo("MobHuman", at);
            var copy = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", at);
            var suits = SEntMan.System<ModSuitSystem>();
            var source = SEntMan.GetComponent<ModSuitComponent>(control);
            var projection = SEntMan.GetComponent<ModSuitComponent>(copy);
            Entity<ModSuitComponent> projectedSuit = (copy, projection);
            var core = suits.GetCore(projectedSuit);
            var battery = suits.GetBattery(projectedSuit);
            var originals = source.PartEntities.Values.ToHashSet();
            var nativeModules = projection.ModulesContainer!.ContainedEntities.ToArray();
            Assert.That(originals.Count, Is.EqualTo(4));
            Assert.That(core, Is.Not.Null);
            Assert.That(battery, Is.Not.Null);
            suits.PrepareProjectedSuit(projectedSuit);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(clone, copy, "back", force: true), Is.True);
            suits.CopyDeployment((control, source), copy, originals, new HashSet<EntityUid>());

            Assert.That(projection.State, Is.EqualTo(ModSuitState.Active));
            Assert.That(suits.GetCore(projectedSuit), Is.Null);
            Assert.That(SEntMan.EntityExists(core!.Value), Is.False);
            Assert.That(SEntMan.EntityExists(battery!.Value.Owner), Is.False);
            Assert.That(projection.ModulesContainer!.ContainedEntities, Is.Empty);
            Assert.That(nativeModules.Any(SEntMan.EntityExists), Is.False);
            Assert.That(suits.GetBattery((control, source)), Is.Not.Null);
            Assert.That(suits.TryToggleActivation(projectedSuit, clone), Is.False);
            Assert.That(suits.TryToggleAllParts(projectedSuit, clone), Is.False);
            Assert.That(TryOpenPanel(), Is.False);
            foreach (var (slot, part) in projection.PartEntities)
            {
                Assert.That(originals, Does.Not.Contain(part));
                Assert.That(suits.IsPartDeployed(projectedSuit, slot), Is.True);
                Assert.That(SEntMan.GetComponent<ModPartComponent>(part).Sealed, Is.True);
                Assert.That(SEntMan.System<InventorySystem>().TryUnequip(clone, slot), Is.False);
            }
            Assert.That(source.Wearer, Is.EqualTo(wearer));

            bool TryOpenPanel() => SEntMan.System<SharedWiresSystem>()
                .TogglePanel(copy, SEntMan.GetComponent<WiresPanelComponent>(copy), true);
        });
    }

    [Test]
    public async Task EquipmentCopyCreatesItsOwnModPartsAndPreservesTheUnderlyingHat()
    {
        EntityUid original = default;
        var control = await EquipPoweredEngineerSuit("ModsuitModuleKinesis", (wearer, _) => original = wearer);
        EntityUid hat = default;
        await Server.WaitAssertion(() =>
        {
            var suits = SEntMan.System<ModSuitSystem>();
            var source = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            // Retraction provides a normal opportunity to change the clothing below the MOD.
            Assert.That(suits.TryToggleActivation(source, original), Is.True);
        });
        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var suits = SEntMan.System<ModSuitSystem>();
            var source = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(suits.TryRetractPart(source, "head", original), Is.True);
            hat = SEntMan.SpawnAttachedTo("ClothingHeadHatHardhatYellow", MapData.GridCoords);
            Assert.That(inventory.TryEquip(original, hat, "head", force: true), Is.True);
            Assert.That(suits.TryDeployPart(source, "head", original), Is.True);

            var backOnly = SEntMan.SpawnAttachedTo("MobHuman", MapData.GridCoords);
            SEntMan.System<CloningSystem>().CopyEquipment(original, backOnly, SlotFlags.BACK, copyStorage: false);
            Assert.That(inventory.TryGetSlotEntity(backOnly, "back", out _), Is.True);
            Assert.That(inventory.TryGetSlotEntity(backOnly, "head", out _), Is.False);

            var temporaryClone = SEntMan.SpawnAttachedTo("MobHuman", MapData.GridCoords);
            SEntMan.System<CloningSystem>().CopyEquipment(original, temporaryClone, SlotFlags.All,
                makeUnremoveable: true, copyStorage: false);
            Assert.That(inventory.TryGetSlotEntity(temporaryClone, "head", out var temporaryHelmet), Is.True);
            var temporaryHat = SEntMan.GetComponent<ModPartComponent>(temporaryHelmet!.Value).StoredClothing!.ContainedEntity;
            Assert.That(temporaryHat, Is.Not.Null);
            SEntMan.DeleteEntity(temporaryClone);
            Assert.That(SEntMan.EntityExists(temporaryHat!.Value), Is.False);

            var clone = SEntMan.SpawnAttachedTo("MobHuman", MapData.GridCoords);
            SEntMan.System<CloningSystem>().CopyEquipment(original, clone, SlotFlags.All, copyStorage: false);
            Assert.That(inventory.TryGetSlotEntity(clone, "back", out var clonedControl), Is.True);
            Assert.That(clonedControl, Is.Not.EqualTo(control));
            var copy = (clonedControl!.Value, SEntMan.GetComponent<ModSuitComponent>(clonedControl.Value));
            Assert.That(suits.IsPartDeployed(copy, "head"), Is.True);
            Assert.That(copy.Item2.ModulesContainer!.ContainedEntities.Any(uid => SEntMan.HasComponent<ModKinesisModuleComponent>(uid)),
                Is.False);
            Assert.That(suits.TryRetractPart(copy, "head", clone), Is.True);
            Assert.That(inventory.TryGetSlotEntity(clone, "head", out var clonedHat), Is.True);
            Assert.That(clonedHat, Is.Not.EqualTo(hat));
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(clonedHat!.Value).EntityPrototype?.ID,
                Is.EqualTo("ClothingHeadHatHardhatYellow"));
            Assert.That(suits.IsPartDeployed(source, "head"), Is.True);
        });
    }

    [Test]
    public async Task SealedModHelmetRestoresOnlyItsOwnOcclusion()
    {
        EntityUid wearer = default;
        EntityUid control = default;
        await Server.WaitAssertion(() =>
        {
            var at = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", at);
            control = SEntMan.SpawnAttachedTo("ClothingModsuitBrigMedPowerCell", at);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);

            var layers = SEntMan.GetComponent<HideableHumanoidLayersComponent>(wearer).HiddenLayers;
            SEntMan.System<SharedHideableHumanoidLayersSystem>()
                .SetLayerOcclusion(wearer, HumanoidVisualLayers.Hair, true, SlotFlags.MASK);
            Assert.That(layers[HumanoidVisualLayers.Hair], Is.EqualTo(SlotFlags.MASK));
            Assert.That(layers[HumanoidVisualLayers.Snout].HasFlag(SlotFlags.HEAD), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var layers = SEntMan.GetComponent<HideableHumanoidLayersComponent>(wearer).HiddenLayers;
            Assert.That(layers[HumanoidVisualLayers.Hair], Is.EqualTo(SlotFlags.MASK | SlotFlags.HEAD));
            Assert.That(SEntMan.System<ModSuitSystem>().TryToggleActivation(
                (control, SEntMan.GetComponent<ModSuitComponent>(control)), wearer), Is.True);
        });

        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            var layers = SEntMan.GetComponent<HideableHumanoidLayersComponent>(wearer).HiddenLayers;
            Assert.That(layers[HumanoidVisualLayers.Hair], Is.EqualTo(SlotFlags.MASK));
            Assert.That(layers[HumanoidVisualLayers.Snout].HasFlag(SlotFlags.HEAD), Is.True);
        });
    }

    [Test]
    public async Task EnergyShieldBlocksOneHitUntilItRecharges()
    {
        EntityUid wearer = default;
        EntityUid module = default;
        await EquipPoweredEngineerSuit("ModsuitModuleEnergyShield", (user, installed) =>
        {
            wearer = user;
            module = installed;
        });

        await Server.WaitAssertion(() =>
        {
            var attacker = SEntMan.SpawnAttachedTo("MobHuman", MapData.GridCoords);
            var damage = SEntMan.System<DamageableSystem>();
            var hit = new DamageSpecifier { DamageDict = { ["Blunt"] = 20 } };
            var shield = SEntMan.GetComponent<ModEnergyShieldModuleComponent>(module);
            Assert.That(shield.Charges, Is.EqualTo(1));
            damage.TryChangeDamage(wearer, hit, out var blocked, origin: attacker);
            Assert.That(blocked?.GetTotal().Float() ?? 0f, Is.Zero);
            Assert.That(shield.Charges, Is.Zero);
            Assert.That(damage.TryChangeDamage(wearer, hit, out var next, origin: attacker), Is.True);
            Assert.That(next!.GetTotal().Float(), Is.Positive);
        });
    }

    [Test]
    public async Task ProjectileDampenerRestoresSpeedAndStopsReducingDamageWhenSwitchedOff()
    {
        EntityUid wearer = default;
        EntityUid module = default;
        EntityUid bullet = default;
        var control = await EquipPoweredEngineerSuit("ModsuitModuleProjectileDampener", (user, installed) =>
        {
            wearer = user;
            module = installed;
        });

        await Server.WaitAssertion(() =>
        {
            var toggle = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, toggle);
            Assert.That(toggle.Handled, Is.True);
            bullet = SEntMan.SpawnAttachedTo("BulletPistol", MapData.GridCoords);
            SGun.ShootProjectile(bullet, Vector2.UnitX, Vector2.Zero, null, wearer, speed: 5f);
        });
        await RunSeconds(0.2f);

        await Server.WaitAssertion(() =>
        {
            var body = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var damage = SEntMan.GetComponent<ProjectileComponent>(bullet).Damage;
            Assert.That(body.LinearVelocity.Length(), Is.EqualTo(0.2f).Within(0.001f));
            var hit = new BeforeProjectileHitEvent(damage, wearer);
            SEntMan.EventBus.RaiseLocalEvent(bullet, ref hit);
            Assert.That(hit.Damage.GetTotal().Float(), Is.EqualTo(damage.GetTotal().Float() * 0.75f).Within(0.01f));
            Assert.That(SEntMan.System<ModSuitSystem>().TryToggleActivation(
                (control, SEntMan.GetComponent<ModSuitComponent>(control)), wearer), Is.True);
            hit = new BeforeProjectileHitEvent(damage, wearer);
            SEntMan.EventBus.RaiseLocalEvent(bullet, ref hit);
            Assert.That(hit.Damage.GetTotal(), Is.EqualTo(damage.GetTotal()));
        });
        await RunSeconds(0.05f);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<ModDampenedProjectileComponent>(bullet), Is.False);
            Assert.That(SEntMan.GetComponent<PhysicsComponent>(bullet).LinearVelocity.Length(), Is.EqualTo(5f).Within(0.001f));
        });
    }

    [Test]
    public async Task RecyclerCollectsAndUnloadsFoamOnce()
    {
        EntityUid wearer = default;
        EntityUid module = default;
        EntityUid trash = default;
        await EquipPoweredEngineerSuit("ModsuitModuleDonkRecycler", (user, installed) =>
        {
            wearer = user;
            module = installed;
        });

        await Server.WaitAssertion(() =>
        {
            var toggle = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, toggle);
            Assert.That(toggle.Handled, Is.True);
            trash = SEntMan.SpawnAttachedTo("DrinkColaCanEmpty", MapData.GridCoords);
        });
        await RunSeconds(0.55f);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(trash), Is.False);
            var recycler = SEntMan.GetComponent<ModRecyclerModuleComponent>(module);
            Assert.That(recycler.Products, Is.EqualTo(1));
            Assert.That(recycler.UnloadActionEntity, Is.Not.Null);

            var unload = new ModRecyclerUnloadActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(module, unload);
            Assert.That(unload.Handled, Is.True);
            Assert.That(recycler.Products, Is.Zero);
            var lookup = SEntMan.System<EntityLookupSystem>();
            Assert.That(lookup.GetEntitiesInRange(MapData.GridCoords, 0.5f)
                .Count(uid => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "BulletFoam"), Is.EqualTo(1));
        });
    }

    private async Task<EntityUid> EquipPoweredEngineerSuit(string prototype, Action<EntityUid, EntityUid> record)
    {
        EntityUid control = default;
        await Server.WaitAssertion(() =>
        {
            var at = MapData.GridCoords;
            var wearer = SEntMan.SpawnAttachedTo("MobHuman", at);
            control = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", at);
            var module = SEntMan.SpawnAttachedTo(prototype, at);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(control);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(wires.TogglePanel(control, panel, false), Is.True);
            foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
            record(wearer, module);
        });
        await RunSeconds(6);
        return control;
    }

    [Test]
    public async Task DetachingArmorUtilitiesRestoresTheOriginalControlUnit()
    {
        await Server.WaitAssertion(() =>
        {
            var at = MapData.GridCoords;
            var wearer = SEntMan.SpawnAttachedTo("MobHuman", at);
            var control = SEntMan.SpawnAttachedTo("ClothingModsuitEngineerPowerCell", at);
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();
            var originalName = SEntMan.GetComponent<MetaDataComponent>(control).EntityName;
            var originalSize = SEntMan.GetComponent<ItemComponent>(control).Size;
            var wires = SEntMan.System<SharedWiresSystem>();
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, control, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(control, SEntMan.GetComponent<WiresPanelComponent>(control), true), Is.True);

            var compression = SEntMan.SpawnAttachedTo("ModsuitModulePlateCompression", at);
            Assert.That(suits.TryInstallModule(suit, compression, wearer), Is.True);
            Assert.That(SEntMan.GetComponent<ItemComponent>(control).Size, Is.EqualTo(new ProtoId<ItemSizePrototype>("Normal")));
            var storage = SEntMan.SpawnAttachedTo("ModsuitModuleStorageCompact", at);
            Assert.That(suits.TryInstallModule(suit, storage, wearer), Is.False);
            Assert.That(suits.TryRemoveModule(suit, compression, wearer), Is.True);
            Assert.That(SEntMan.GetComponent<ItemComponent>(control).Size, Is.EqualTo(originalSize));

            var module = SEntMan.SpawnAttachedTo("ModsuitModuleChameleon", at);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(SEntMan.HasComponent<ChameleonClothingComponent>(control), Is.True);
            SEntMan.System<SharedChameleonClothingSystem>().SetSelectedPrototype(control,
                "ClothingBackpack", forceUpdate: true, validate: false);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(control).EntityName, Is.Not.EqualTo(originalName));
            Assert.That(suits.TryRemoveModule(suit, module, wearer), Is.True);
            Assert.That(SEntMan.HasComponent<ChameleonClothingComponent>(control), Is.False);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(control).EntityName, Is.EqualTo(originalName));
        });
    }

    [Test]
    public async Task OpticsUseExistingVisionAndSolutionScanEvents()
    {
        EntityUid wearer = default;
        EntityUid unit = default;
        EntityUid night = default;
        EntityUid scanner = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", position);
            unit = SEntMan.SpawnAttachedTo("TestModControlUnit", position);
            night = SEntMan.SpawnAttachedTo("ModsuitModuleNightVisor", position);
            scanner = SEntMan.SpawnAttachedTo("ModsuitModuleReagentScanner", position);
            var thermal = SEntMan.SpawnAttachedTo("ModsuitModuleThermalVisor", position);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", position);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);
            var wires = SEntMan.System<SharedWiresSystem>();

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(unit, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, unit, "back", force: true), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, thermal, wearer), Is.True);
            Assert.That(suits.TryInstallModule(suit, night, wearer), Is.False);
            Assert.That(suits.TryRemoveModule(suit, thermal, wearer), Is.True);
            Assert.That(suits.TryInstallModule(suit, night, wearer), Is.True);
            Assert.That(suits.TryInstallModule(suit, scanner, wearer), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var nightAction = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(night, nightAction);
            Assert.That(nightAction.Handled, Is.True);
            var scanAction = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(scanner, scanAction);
            Assert.That(scanAction.Handled, Is.True);

            var light = new RefreshNightVisionEvent();
            SEntMan.EventBus.RaiseLocalEvent(wearer, ref light);
            Assert.That(light.Entities, Has.Count.EqualTo(1));
            var scan = new SolutionScanEvent();
            SEntMan.EventBus.RaiseLocalEvent(wearer, scan);
            Assert.That(scan.CanScan, Is.True);

            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var battery = SEntMan.System<ModSuitSystem>().GetBattery(suit)!.Value.AsNullable();
            SEntMan.System<SharedBatterySystem>().SetCharge(battery, 0);
            light = new RefreshNightVisionEvent();
            SEntMan.EventBus.RaiseLocalEvent(wearer, ref light);
            Assert.That(light.Entities, Is.Empty);
            scan = new SolutionScanEvent();
            SEntMan.EventBus.RaiseLocalEvent(wearer, scan);
            Assert.That(scan.CanScan, Is.False);
        });
    }

    [Test]
    public async Task SealAddsAndRemovesOnlyItsOwnEffect()
    {
        EntityUid wearer = default;
        EntityUid unit = default;
        EntityUid helmet = default;
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", location);
            unit = SEntMan.SpawnAttachedTo("TestModControlUnit", location);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", location);
            var inventory = SEntMan.System<InventorySystem>();
            var suits = SEntMan.System<ModSuitSystem>();
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(unit, "mod-core", core, null), Is.True);
            Assert.That(inventory.TryEquip(wearer, unit, "back", silent: true, force: true), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.False);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(inventory.TryGetSlotEntity(wearer, "head", out var equipped), Is.True);
            helmet = equipped!.Value;
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<NoSlipComponent>(helmet), Is.True);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            Assert.That(SEntMan.System<ModSuitSystem>().TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<NoSlipComponent>(helmet), Is.False));
    }

    [Test]
    public async Task ModPartsRetractBeforeInventoryTransfer()
    {
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            var original = SEntMan.SpawnAttachedTo("MobHuman", location);
            var recipient = SEntMan.SpawnAttachedTo("MobHuman", location);
            var control = SEntMan.SpawnAttachedTo("TestModControlUnit", location);
            var hat = SEntMan.SpawnAttachedTo("ClothingHeadHatBeret", location);
            var inventory = SEntMan.System<ServerInventorySystem>();
            var suits = SEntMan.System<ModSuitSystem>();
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));

            Assert.That(inventory.TryEquip(original, control, "back", silent: true, force: true), Is.True);
            Assert.That(inventory.TryEquip(original, hat, "head", silent: true, force: true), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", original), Is.True);
            Assert.That(suits.IsPartDeployed(suit, "head"), Is.True);
            Assert.That(suits.TryRetractPart(suit, "head", original), Is.True);
            Assert.That(inventory.TryGetSlotEntity(original, "head", out var restored), Is.True);
            Assert.That(restored, Is.EqualTo(hat));

            Assert.That(suits.TryDeployPart(suit, "head", original), Is.True);
            inventory.TransferEntityInventories(original, recipient);
            Assert.That(inventory.TryGetSlotEntity(recipient, "back", out var transferred), Is.True);
            Assert.That(transferred, Is.EqualTo(control));
            Assert.That(inventory.TryGetSlotEntity(recipient, "head", out restored), Is.True);
            Assert.That(restored, Is.EqualTo(hat));
            Assert.That(suits.IsPartDeployed(suit, "head"), Is.False);
        });
    }

    [Test]
    public async Task ModulesRespectPanelAndPowerAndDetachCleanly()
    {
        EntityUid wearer = default;
        EntityUid unit = default;
        EntityUid module = default;
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", location);
            unit = SEntMan.SpawnAttachedTo("TestModControlUnit", location);
            module = SEntMan.SpawnAttachedTo("TestModPassive", location);
            var missingPart = SEntMan.SpawnAttachedTo("TestModMountless", location);
            var duplicate = SEntMan.SpawnAttachedTo("TestModPassive", location);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", location);

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(unit, "mod-core", core, null), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, unit, "back", force: true), Is.True);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.False);
            Assert.That(wires.TogglePanel(unit, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, missingPart, wearer), Is.False);
            Assert.That(suits.TryInstallModule(suit, module, wearer), Is.True);
            Assert.That(suits.TryInstallModule(suit, duplicate, wearer), Is.False);
            Assert.That(suits.GetComplexity(suit), Is.EqualTo(1));
            Assert.That(wires.TogglePanel(unit, panel, false), Is.True);
            Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var batteries = SEntMan.System<SharedBatterySystem>();
            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            var installed = SEntMan.GetComponent<ModModuleComponent>(module);
            Assert.That(installed.Active, Is.True);
            Assert.That(batteries.RefreshChargeRate(battery), Is.EqualTo(-1.55f).Within(0.001f));
            batteries.SetCharge(battery, 0);
            Assert.That(installed.Active, Is.False);
            batteries.SetCharge(battery, 100);
            Assert.That(installed.Active, Is.True);
            Assert.That(suits.TryRemoveModule(suit, module, wearer), Is.False);
            var wires = SEntMan.System<SharedWiresSystem>();
            Assert.That(wires.TogglePanel(unit, SEntMan.GetComponent<WiresPanelComponent>(unit), true), Is.False);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            Assert.That(SEntMan.System<SharedWiresSystem>().TogglePanel(unit, SEntMan.GetComponent<WiresPanelComponent>(unit), true), Is.True);
            Assert.That(suits.TryRemoveModule(suit, module, wearer), Is.True);
            Assert.That(suits.GetComplexity(suit), Is.Zero);
            Assert.That(SEntMan.GetComponent<ModModuleComponent>(module).Suit, Is.Null);
        });
    }

    [Test]
    public async Task ModulesLeaveNativeStorageAndHelmetLightIntact()
    {
        EntityUid wearer = default;
        EntityUid unit = default;
        EntityUid lamp = default;
        EntityUid storageModule = default;
        EntityUid nativeItem = default;
        EntityUid moduleItem = default;
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            wearer = SEntMan.SpawnAttachedTo("MobHuman", location);
            unit = SEntMan.SpawnAttachedTo("TestModControlUnit", location);
            lamp = SEntMan.SpawnAttachedTo("ModsuitModuleFlashlight", location);
            storageModule = SEntMan.SpawnAttachedTo("ModsuitModuleStorageCompact", location);
            nativeItem = SEntMan.SpawnAttachedTo("ClothingHeadHatBeret", location);
            moduleItem = SEntMan.SpawnAttachedTo("ClothingHeadHatBeret", location);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", location);
            var inventory = SEntMan.System<InventorySystem>();
            var storage = SEntMan.System<SharedStorageSystem>();
            var suits = SEntMan.System<ModSuitSystem>();
            var wires = SEntMan.System<SharedWiresSystem>();
            var panel = SEntMan.GetComponent<WiresPanelComponent>(unit);
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(unit, "mod-core", core, null), Is.True);
            Assert.That(inventory.TryEquip(wearer, unit, "back", force: true), Is.True);
            Assert.That(storage.Insert(unit, nativeItem, out _), Is.True);
            Assert.That(storage.Insert(storageModule, moduleItem, out _), Is.False);
            Assert.That(wires.TogglePanel(unit, panel, true), Is.True);
            Assert.That(suits.TryInstallModule(suit, storageModule, wearer), Is.True);
            Assert.That(suits.TryInstallModule(suit, lamp, wearer), Is.True);
            Assert.That(storage.Insert(storageModule, moduleItem, out _), Is.True);
            Assert.That(wires.TogglePanel(unit, panel, false), Is.True);
            Assert.That(suits.TryToggleAllParts(suit, wearer), Is.True);
        });

        await RunSeconds(0.25f);
        await Server.WaitAssertion(() =>
        {
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            Assert.That(SEntMan.System<ModSuitSystem>().TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var suits = SEntMan.System<ModSuitSystem>();
            var lights = SEntMan.System<SharedPointLightSystem>();
            var installedLamp = SEntMan.GetComponent<ModModuleComponent>(lamp);
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(wearer, "head", out var head), Is.True);
            Assert.That(lights.TryGetLight(head!.Value, out var nativeLight), Is.True);
            var action = new ModModuleActionEvent { Performer = wearer };
            SEntMan.EventBus.RaiseLocalEvent(lamp, action);
            Assert.That(action.Handled, Is.True);
            var effect = SEntMan.GetComponent<ModLightModuleComponent>(lamp).Light;
            Assert.That(effect, Is.Not.Null);
            Assert.That(nativeLight!.Enabled, Is.True);
            Assert.That(nativeLight.Radius, Is.EqualTo(2f));

            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            SEntMan.System<SharedBatterySystem>().SetCharge(battery, 0);
            Assert.That(installedLamp.Active, Is.False);
            Assert.That(SEntMan.GetComponent<ModLightModuleComponent>(lamp).Light, Is.Null);
            Assert.That(nativeLight.Enabled, Is.True);
            Assert.That(suits.TryToggleActivation(suit, wearer), Is.True);
        });

        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var suit = (unit, SEntMan.GetComponent<ModSuitComponent>(unit));
            var wires = SEntMan.System<SharedWiresSystem>();
            Assert.That(wires.TogglePanel(unit, SEntMan.GetComponent<WiresPanelComponent>(unit), true), Is.True);
            Assert.That(SEntMan.System<ModSuitSystem>().TryRemoveModule(suit, storageModule, wearer), Is.True);
            Assert.That(SEntMan.GetComponent<StorageComponent>(unit).Container.Contains(nativeItem), Is.True);
            Assert.That(SEntMan.GetComponent<StorageComponent>(storageModule).Container.Count, Is.Zero);
            Assert.That(SEntMan.EntityExists(moduleItem), Is.True);
        });
    }

    [Test]
    public async Task ExistingLoadoutsAndCraftedCoreUseTheSameControlSystem()
    {
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            var suits = SEntMan.System<ModSuitSystem>();
            var slots = SEntMan.System<ItemSlotsSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            var wearer = SEntMan.SpawnAttachedTo("MobHuman", location);
            var trials = new (string Suit, string Cell)[]
            {
                ("ClothingModsuitStandard", null),
                ("ClothingModsuitStandardPowerCell", "PowerCellHigh"),
                ("ClothingModsuitSuspiciousPowerCell", "PowerCellHigh"),
                ("ClothingModsuitContractor", "PowerCellVortex"),
                ("ClothingModsuitCorporate", "PowerCellVortex"),
                ("ClothingModsuitMining", null),
                ("ClothingModsuitLoader", null),
                ("ClothingModsuitBrigMed", null),
                ("ClothingModsuitBrigMedPowerCell", "PowerCellHigh"),
                ("ClothingModsuitResponsoryEngineering", "PowerCellBluespaceReactor"),
            };

            foreach (var (prototype, cellPrototype) in trials)
            {
                var control = SEntMan.SpawnAttachedTo(prototype, location);
                var component = SEntMan.GetComponent<ModSuitComponent>(control);
                var suit = (control, component);
                Assert.That(component.PartEntities.Count, Is.EqualTo(4), prototype);
                Assert.That(suits.GetCore(suit), Is.Not.Null, prototype);
                var battery = suits.GetBattery(suit);
                Assert.That(battery == null ? null : SEntMan.GetComponent<MetaDataComponent>(battery.Value).EntityPrototype?.ID,
                    Is.EqualTo(cellPrototype), prototype);
                Assert.That(inventory.TryEquip(wearer, control, "back", force: true), Is.True);
                Assert.That(suits.TryDeployPart(suit, "head", wearer), Is.True);
                Assert.That(inventory.TryUnequip(wearer, "back", force: true), Is.True);
                Assert.That(inventory.TryGetSlotEntity(wearer, "head", out _), Is.False);
            }

            foreach (var prototype in new[] { "ClothingModsuitSyndicateFilled", "ClothingModsuitSyndicateEliteFilled" })
            {
                var control = SEntMan.SpawnAttachedTo(prototype, location);
                var component = SEntMan.GetComponent<ModSuitComponent>(control);
                var suit = (control, component);
                Assert.That(component.PartEntities.Count, Is.EqualTo(4), prototype);
                Assert.That(suits.GetCore(suit), Is.Not.Null, prototype);
                Assert.That(SEntMan.GetComponent<MetaDataComponent>(suits.GetCore(suit)!.Value).EntityPrototype?.ID,
                    Is.EqualTo("ModsuitCoreStandardMicroreactor"), prototype);
                var modules = component.ModulesContainer!.ContainedEntities
                    .Select(uid => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID);
                Assert.That(modules, Is.EquivalentTo(new[]
                {
                    "ModsuitModuleBuiltinFlashProtection", "ModsuitModuleBuiltinNightVision", "ModsuitModuleBuiltinInsulation",
                    "ModsuitModuleStorageSyndicate", "ModsuitModuleFlashlight",
                }), prototype);
                Assert.That(inventory.TryEquip(wearer, control, "back", force: true), Is.True, prototype);
                foreach (var slot in new[] { "head", "outerClothing", "gloves", "shoes" })
                    Assert.That(suits.TryDeployPart(suit, slot, wearer), Is.True, prototype);
                Assert.That(inventory.TryUnequip(wearer, "back", force: true), Is.True, prototype);
            }

            var shell = SEntMan.SpawnAttachedTo("ModsuitShell", location);
            var custom = SEntMan.SpawnAttachedTo("ModsuitCorePlasma", location);
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(shell, "mod-core", out var shellSlot), Is.True);
            Assert.That(containers.Insert(custom, shellSlot!), Is.True);
            Assert.That(SEntMan.System<ConstructionSystem>().ChangeNode(shell, null, "standard"), Is.True);
            Assert.That(containers.TryGetContainingContainer(custom, out var installed), Is.True);
            Assert.That(installed!.Owner, Is.Not.EqualTo(shell));
            Assert.That(slots.GetItemOrNull(installed.Owner, "mod-core"), Is.EqualTo(custom));

            foreach (var (node, module) in new[]
            {
                ("mining", "ModsuitModuleBuiltinSphereTransform"),
                ("loader", "ModsuitModuleBuiltinLoaderMagnet"),
                ("brigmed", null),
            })
            {
                var empty = SEntMan.SpawnAttachedTo("ModsuitShell", location);
                var originalCore = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", location);
                Assert.That(containers.TryGetContainer(empty, "mod-core", out var coreSlot), Is.True);
                Assert.That(containers.Insert(originalCore, coreSlot!), Is.True);
                Assert.That(SEntMan.System<ConstructionSystem>().ChangeNode(empty, null, node), Is.True);
                Assert.That(containers.TryGetContainingContainer(originalCore, out var finished), Is.True);
                var completed = SEntMan.GetComponent<ModSuitComponent>(finished!.Owner);
                Assert.That(completed.PartEntities.Count, Is.EqualTo(4), node);
                if (module != null)
                    Assert.That(completed.ModulesContainer!.ContainedEntities.Any(uid =>
                        SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == module), Is.True, node);
            }
        });
    }

    [Test]
    public async Task InstalledCoreDrainsOnceAndDetachmentStopsTheDrain()
    {
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            var control = SEntMan.SpawnAttachedTo("TestActiveModControlUnit", location);
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", location);
            var slots = SEntMan.System<ItemSlotsSystem>();
            var batteries = SEntMan.System<SharedBatterySystem>();
            var suit = (control, SEntMan.GetComponent<ModSuitComponent>(control));
            var suits = SEntMan.System<ModSuitSystem>();

            Assert.That(suits.HasPower(suit), Is.False);
            Assert.That(slots.TryInsert(control, "mod-core", core, null), Is.True);
            Assert.That(suits.HasPower(suit), Is.True);
            var battery = suits.GetBattery(suit)!.Value.AsNullable();
            Assert.That(batteries.RefreshChargeRate(battery), Is.EqualTo(-0.8f));
            batteries.SetCharge(battery, 0);
            Assert.That(suits.HasPower(suit), Is.False);
            batteries.SetCharge(battery, 20);
            Assert.That(suits.HasPower(suit), Is.True);

            Assert.That(slots.TryEject(control, "mod-core", null, out var removed), Is.True);
            Assert.That(removed, Is.EqualTo(core));
            Assert.That(suits.HasPower(suit), Is.False);
            Assert.That(batteries.RefreshChargeRate(battery), Is.Zero);
        });
    }

    [Test]
    public async Task StandardCoreAcceptsSelfRechargingCells()
    {
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            var core = SEntMan.SpawnAttachedTo("ModsuitCoreStandard", location);
            var cell = SEntMan.SpawnAttachedTo("PowerCellVortex", location);
            var prefilled = SEntMan.SpawnAttachedTo("ModsuitCoreStandardHighCapacity", location);
            var power = SEntMan.System<PowerCellSystem>();
            var slots = SEntMan.System<ItemSlotsSystem>();

            Assert.That(SEntMan.HasComponent<ModCoreComponent>(core), Is.True);
            Assert.That(power.TryGetBatteryFromEntityOrSlot(core, out _), Is.False);
            Assert.That(slots.TryInsert(core, "cell_slot", cell, null), Is.True);
            Assert.That(power.TryGetBatteryFromEntityOrSlot(core, out var installed), Is.True);
            Assert.That(installed!.Value.Owner, Is.EqualTo(cell));

            Assert.That(power.TryGetBatteryFromEntityOrSlot(prefilled, out var starting), Is.True);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(starting!.Value.Owner).EntityPrototype!.ID,
                Is.EqualTo("PowerCellHigh"));
        });
    }

    [Test]
    public async Task PlasmaCoreOnlyUsesEnoughFuelToFillCharge()
    {
        await Server.WaitAssertion(() =>
        {
            var location = MapData.GridCoords;
            var core = SEntMan.SpawnAttachedTo("ModsuitCorePlasma", location);
            var plasma = SEntMan.SpawnAttachedTo("SheetPlasma10", location);
            var power = SEntMan.System<PowerCellSystem>();
            var batteries = SEntMan.System<SharedBatterySystem>();
            var stacks = SEntMan.System<SharedStackSystem>();
            Assert.That(power.TryGetBatteryFromEntityOrSlot(core, out var entry), Is.True);
            var battery = entry!.Value.AsNullable();

            Refuel();
            Assert.That(stacks.GetCount(plasma), Is.EqualTo(10));
            batteries.SetCharge(battery, 450);
            Refuel();
            Assert.That(batteries.GetCharge(battery), Is.EqualTo(720));
            Assert.That(stacks.GetCount(plasma), Is.EqualTo(8));

            void Refuel()
            {
                var interact = new InteractUsingEvent(SPlayer, plasma, core, location);
                SEntMan.EventBus.RaiseLocalEvent(core, interact);
                Assert.That(interact.Handled, Is.True);
            }
        });
    }
}
