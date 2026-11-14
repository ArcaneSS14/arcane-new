using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Construction;
using Content.Server.Inventory;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Slippery;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Wires;
using Robust.Shared.Containers;

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
