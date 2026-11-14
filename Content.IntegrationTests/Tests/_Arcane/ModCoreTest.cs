using System.Linq;
using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Common.Body;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Inventory;
using Content.Shared.Actions;
using Content.Shared.Armor;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.NightVision;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Slippery;
using Content.Shared.Speech.Components;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Wires;
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

            var shell = SEntMan.SpawnAttachedTo("ModsuitShell", location);
            var custom = SEntMan.SpawnAttachedTo("ModsuitCorePlasma", location);
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(shell, "mod-core", out var shellSlot), Is.True);
            Assert.That(containers.Insert(custom, shellSlot!), Is.True);
            Assert.That(SEntMan.System<ConstructionSystem>().ChangeNode(shell, null, "standard"), Is.True);
            Assert.That(containers.TryGetContainingContainer(custom, out var installed), Is.True);
            Assert.That(installed!.Owner, Is.Not.EqualTo(shell));
            Assert.That(slots.GetItemOrNull(installed.Owner, "mod-core"), Is.EqualTo(custom));
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
