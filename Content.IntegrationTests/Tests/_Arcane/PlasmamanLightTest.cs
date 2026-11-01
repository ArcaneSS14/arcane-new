#nullable enable
using Content.Arcane.Shared.Light;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Body;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell.Components;
using Content.Shared.Toggleable;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using ServerHandheldLightSystem = Content.Server.Light.EntitySystems.HandheldLightSystem;
using ServerPointLightComponent = Robust.Server.GameObjects.PointLightComponent;
using ClientPointLightComponent = Robust.Client.GameObjects.PointLightComponent;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PlasmamanLightTest : InteractionTest
{
    protected override string PlayerPrototype => "MobPlasmaman";

    [Test]
    public async Task WornClothingDimsBodyGlowAndRemovingItRestoresTheCurrentBodyColour()
    {
        await Server.WaitPost(() => SetSkin(Color.Cyan));
        await Pair.RunTicksSync(3);
        await AssertGlow(1f, Color.Cyan);
        await Equip("ClothingUniformEnvirosuit", "jumpsuit");
        await AssertGlow(0.4f, Color.Cyan);
        await Equip("ClothingHandsGlovesEnvirogloves", "pocket1", force: false);
        await AssertGlow(0.4f, Color.Cyan);
        await Server.WaitPost(() => Assert.That(SEntMan.System<InventorySystem>().TryUnequip(SPlayer, "pocket1", out _, force: true), Is.True));
        await Equip("ClothingHeadEnvirohelm", "head");
        await AssertGlow(0.1f, Color.Cyan);
        await Equip("ClothingHandsGlovesEnvirogloves", "gloves");
        await AssertGlow(0f, Color.Cyan);

        await Server.WaitPost(() =>
        {
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryUnequip(SPlayer, "head", out _, force: true), Is.True);
            Assert.That(inventory.TryUnequip(SPlayer, "gloves", out _, force: true), Is.True);
            Assert.That(inventory.TryUnequip(SPlayer, "jumpsuit", out _, force: true), Is.True);
            SetSkin(Color.Orange);
        });
        await Pair.RunTicksSync(3);
        await AssertGlow(1f, Color.Orange);
    }

    [Test]
    public async Task HelmetUsesTheCurrentWearerColourAndNeedsNoReplaceableCell()
    {
        await Server.WaitPost(() => SetSkin(Color.Cyan));
        await SpawnTarget("ClothingHeadEnvirohelm");
        var helmet = STarget!.Value;
        var fallback = Color.FromHex("#ffa1ff");
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<PowerCellSlotComponent>(helmet), Is.False);
            Assert.That(SEntMan.HasComponent<ItemSlotsComponent>(helmet), Is.False);
            Assert.That(SEntMan.System<ServerHandheldLightSystem>().TurnOn(SPlayer,
                (helmet, SEntMan.GetComponent<HandheldLightComponent>(helmet))), Is.True);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(SPlayer, helmet, "head", force: true), Is.True);
        });
        await Pair.RunUntilSynced();
        await AssertHelmet(helmet, Color.Cyan, false);
        await Server.WaitPost(() => SetSkin(Color.Orange));
        await Pair.RunUntilSynced();
        await AssertHelmet(helmet, Color.Orange, false);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<BatteryComponent>(helmet), Is.True);
            var batteries = SEntMan.System<SharedBatterySystem>();
            var before = batteries.GetCharge(helmet).Charge;
            SEntMan.System<ServerHandheldLightSystem>().TryUpdate((helmet, SEntMan.GetComponent<HandheldLightComponent>(helmet)), 60f);
            Assert.That(batteries.GetCharge(helmet).Charge, Is.EqualTo(before));
            SEntMan.System<SharedEmpSystem>().TryEmpEffects(helmet, 1000f, TimeSpan.FromSeconds(10));
            Assert.That(batteries.GetCharge(helmet).Charge, Is.EqualTo(before));
            Assert.That(SEntMan.System<InventorySystem>().TryUnequip(SPlayer, "head", out _, force: true), Is.True);
        });
        await Pair.RunUntilSynced();
        await AssertHelmet(helmet, fallback, false);
        await Server.WaitAssertion(() =>
        {
            var human = SEntMan.SpawnEntity("MobHuman", SEntMan.GetCoordinates(PlayerCoords));
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(human, helmet, "head", force: true), Is.True);
        });
        await Pair.RunUntilSynced();
        await AssertHelmet(helmet, fallback, true);
    }

    private void SetSkin(Color color) => SEntMan.System<SharedVisualBodySystem>().ApplyProfile(SPlayer, new OrganProfileData { SkinColor = color });

    private async Task Equip(string prototype, string slot, bool force = true)
    {
        await Server.WaitAssertion(() =>
        {
            var clothing = SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(SPlayer, clothing, slot, force: force), Is.True,
                $"Could not equip {prototype} in {slot}.");
        });
        await Pair.RunTicksSync(3);
    }

    private async Task AssertGlow(float factor, Color color)
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<BodyGlowComponent>(SPlayer).Factor, Is.EqualTo(factor).Within(0.0001f));
            var light = SEntMan.GetComponent<ServerPointLightComponent>(SPlayer);
            Assert.That(light.Enabled, Is.EqualTo(factor > 0f));
            Assert.That(light.Color, Is.EqualTo(color));
            if (factor > 0f)
            {
                Assert.That(light.Radius, Is.EqualTo(3f * factor).Within(0.0001f));
                Assert.That(light.Energy, Is.EqualTo(1.5f * factor).Within(0.0001f));
            }
        });
    }

    private async Task AssertHelmet(EntityUid helmet, Color color, bool suppressed)
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<WearerLightColorComponent>(helmet).LightSuppressed, Is.EqualTo(suppressed));
            Assert.That(SEntMan.GetComponent<ServerPointLightComponent>(helmet).Color, Is.EqualTo(color));
            Assert.That(SEntMan.GetComponent<ServerPointLightComponent>(helmet).Enabled, Is.EqualTo(!suppressed));
            var appearance = SEntMan.System<SharedAppearanceSystem>();
            Assert.That(appearance.TryGetData<Color>(helmet, ToggleableVisuals.Color, out var tint), Is.True);
            Assert.That(tint, Is.EqualTo(color));
        });
        await Client.WaitAssertion(() =>
        {
            var light = CEntMan.GetComponent<ClientPointLightComponent>(ToClient(helmet));
            Assert.That(light.Color, Is.EqualTo(color));
            Assert.That(light.Enabled, Is.EqualTo(!suppressed));
        });
    }
}
