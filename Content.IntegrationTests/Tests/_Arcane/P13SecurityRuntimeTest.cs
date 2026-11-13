#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Radio.Components;
using Content.Shared.Station.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Turrets;
using Content.Trauma.Common.Carrying;
using Content.Trauma.Shared.Carrying;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P13SecurityRuntimeTest : InteractionTest
{
    private static readonly EntProtoId Flashlight = "FlashlightSeclite";
    private static readonly EntProtoId Jumpsuit = "ClothingUniformJumpsuitColorGrey";
    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId Borg = "BorgChassisGeneric";
    private static readonly EntProtoId Turret = "WeaponTurretNanoTrasen";

    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [TestCase("WeaponPistolMk58")]
    [TestCase("WeaponPistolInteQCommissar")]
    [TestCase("WeaponPistolG22")]
    [TestCase("WeaponRevolverDeckard")]
    [TestCase("WeaponEarthGovLaserPistol")]
    [TestCase("WeaponLaserCellRevolver")]
    public async Task SidearmAcceptsAndEjectsOneFunctionalFlashlight(string prototype)
    {
        await Server.WaitAssertion(() =>
        {
            var weapon = SEntMan.SpawnAttachedTo(new EntProtoId(prototype), Center);
            var light = SEntMan.SpawnAttachedTo(Flashlight, Center);
            var extra = SEntMan.SpawnAttachedTo(Flashlight, Center);
            var user = SEntMan.SpawnAttachedTo(Human, Center);
            var slots = SEntMan.System<ItemSlotsSystem>();
            var hands = SEntMan.System<SharedHandsSystem>();
            var actions = SEntMan.System<SharedActionsSystem>();
            Assert.That(hands.TryPickupAnyHand(user, weapon, checkActionBlocker: false), Is.True);
            Assert.That(slots.TryGetSlot(weapon, "flashlight", out var mount), Is.True, "Mount was lost to the weapon's ammo slot data");
            if (slots.TryGetSlot(weapon, "gun_magazine", out var magazine))
                Assert.That(mount!.Priority, Is.LessThan(magazine.Priority));
            Assert.That(slots.TryInsert(weapon, "flashlight", light, user: null), Is.True);
            Assert.That(slots.TryInsert(weapon, "flashlight", extra, user: null), Is.False);
            var action = actions.GetActions(user).Single(action => action.Comp.Container == light);
            Assert.That(actions.PerformAction(user, action, predicted: false), Is.True);
            Assert.That(SEntMan.GetComponent<HandheldLightComponent>(light).Activated, Is.True);
            Assert.That(slots.TryEject(weapon, "flashlight", user: null, out var ejected), Is.True);
            Assert.That(ejected, Is.EqualTo(light));
            Assert.That(actions.GetActions(user).Any(action => action.Comp.Container == light), Is.False);
            Assert.That(slots.TryInsert(weapon, "flashlight", light, user: null), Is.True);
        });
    }

    [Test]
    public async Task DeniedPanicButtonUseDoesNotStartCooldown()
    {
        await Server.WaitAssertion(() =>
        {
            var user = SEntMan.SpawnAttachedTo(Human, Center);
            var button = SEntMan.SpawnAttachedTo(new EntProtoId("PanicButton"), Center);
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(user, button, checkActionBlocker: false), Is.True);
            Assert.That(SEntMan.System<SharedInteractionSystem>().UseInHandInteraction(user, button), Is.True);
            Assert.That(SEntMan.System<UseDelaySystem>().IsDelayed(button), Is.False);
        });
    }

    [Test]
    public async Task AuthorizedPanicButtonOnlyAlertsItsStationAndStartsCooldown()
    {
        await Server.WaitAssertion(() =>
        {
            var stations = SEntMan.System<ServerStationSystem>();
            var station = SEntMan.SpawnAttachedTo(new EntProtoId("TestStation"), Center);
            var otherStation = SEntMan.SpawnAttachedTo(new EntProtoId("TestStation"), Center);
            stations.AddGridToStation(station, MapData.Grid);

            var user = SEntMan.SpawnAttachedTo(Human, Center);
            var id = SEntMan.SpawnAttachedTo(new EntProtoId("SecurityIDCard"), Center);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(user, id, "id", force: true), Is.True);
            var button = SEntMan.SpawnAttachedTo(new EntProtoId("PanicButton"), Center);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryPickupAnyHand(user, button, checkActionBlocker: false), Is.True);
            SEntMan.AddComponent<TelecomExemptComponent>(button);

            var localReceiver = SEntMan.SpawnAttachedTo(null, Center);
            var distantReceiver = SEntMan.SpawnAttachedTo(null, Center);
            var distantTracker = SEntMan.AddComponent<StationTrackerComponent>(distantReceiver);
            stations.SetStation((distantReceiver, distantTracker), otherStation);
            SEntMan.AddComponent<ActiveRadioComponent>(localReceiver).ReceiveAllChannels = true;
            SEntMan.AddComponent<ActiveRadioComponent>(distantReceiver).ReceiveAllChannels = true;
            var localProbe = SEntMan.AddComponent<P09RadioProbeComponent>(localReceiver);
            var distantProbe = SEntMan.AddComponent<P09RadioProbeComponent>(distantReceiver);
            localProbe.ExpectedSource = distantProbe.ExpectedSource = button;

            var interaction = SEntMan.System<SharedInteractionSystem>();
            Assert.That(interaction.UseInHandInteraction(user, button), Is.True);
            Assert.That(SEntMan.System<UseDelaySystem>().IsDelayed(button), Is.True);
            Assert.That(interaction.UseInHandInteraction(user, button), Is.True);
            Assert.That(localProbe.Channels, Is.EqualTo(new[] { "Security" }));
            Assert.That(distantProbe.Channels, Is.Empty);
        });
    }

    [Test]
    public async Task RepeatedEmpRestoresOriginalSuitSensorMode()
    {
        EntityUid uniform = default;
        await Server.WaitAssertion(() =>
        {
            uniform = SEntMan.SpawnAttachedTo(Jumpsuit, Center);
            var sensors = SEntMan.System<SharedSuitSensorSystem>();
            var emp = SEntMan.System<SharedEmpSystem>();
            sensors.SetSensor(uniform, SuitSensorMode.SensorVitals);
            Assert.That(emp.TryEmpEffects(uniform, 0, TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(emp.TryEmpEffects(uniform, 0, TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(SEntMan.GetComponent<SuitSensorComponent>(uniform).Mode, Is.EqualTo(SuitSensorMode.SensorOff));
        });

        await RunSeconds(2.2f);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.GetComponent<SuitSensorComponent>(uniform).Mode, Is.EqualTo(SuitSensorMode.SensorVitals)));
    }

    [Test]
    public async Task MovementEscapesBeingCarriedAndClearsBothSides()
    {
        EntityUid carrier = default;
        EntityUid carried = default;
        await Server.WaitAssertion(() =>
        {
            carrier = SEntMan.SpawnAttachedTo(Human, Center);
            carried = SEntMan.SpawnAttachedTo(Human, Center);
            var carrying = SEntMan.System<CarryingSystem>();
            Assert.That(carrying.TryCarry(carrier, carried), Is.True);
            Assert.That(carrying.GetCarrier(carried), Is.EqualTo(carrier));
            var mover = SEntMan.GetComponent<InputMoverComponent>(carried);
            mover.HeldMoveButtons = MoveButtons.Up;
            var input = new MoveInputEvent((carried, mover), MoveButtons.None, Direction.North, true);
            SEntMan.EventBus.RaiseLocalEvent(carried, ref input);
        });

        await RunSeconds(5.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<CarryingSystem>().GetCarrier(carried), Is.Null);
            Assert.That(SEntMan.HasComponent<CarryingComponent>(carrier), Is.False);
            Assert.That(SEntMan.System<SharedHandsSystem>().EnumerateHeld(carrier), Is.Empty);
        });
    }

    [Test]
    public async Task InactiveBorgUsesExistingTurretBorgExemption()
    {
        await Server.WaitAssertion(() =>
        {
            var borg = SEntMan.SpawnAttachedTo(Borg, Center);
            var turret = SEntMan.SpawnAttachedTo(Turret, Center);
            var settings = (turret, SEntMan.EnsureComponent<TurretTargetSettingsComponent>(turret));
            var targeting = SEntMan.System<TurretTargetSettingsSystem>();
            ProtoId<Content.Shared.Access.AccessLevelPrototype> borgAccess = "Borg";
            targeting.SetAccessLevelExemption(settings, borgAccess, true);
            Assert.That(targeting.EntityIsTargetForTurret(settings, borg), Is.False);
            targeting.SetAccessLevelExemption(settings, borgAccess, false);
            Assert.That(targeting.EntityIsTargetForTurret(settings, borg), Is.True);
        });
    }
}
