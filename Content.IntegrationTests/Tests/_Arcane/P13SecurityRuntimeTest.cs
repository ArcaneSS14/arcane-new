#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
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
            var slots = SEntMan.System<ItemSlotsSystem>();
            var toggle = SEntMan.System<ItemToggleSystem>();
            Assert.That(slots.TryInsert(weapon, "flashlight", light, user: null), Is.True);
            Assert.That(slots.TryInsert(weapon, "flashlight", extra, user: null), Is.False);
            Assert.That(toggle.TrySetActive(light, true, predicted: false), Is.True);
            Assert.That(toggle.IsActivated(light), Is.True);
            Assert.That(slots.TryEject(weapon, "flashlight", user: null, out var ejected), Is.True);
            Assert.That(ejected, Is.EqualTo(light));
            Assert.That(slots.TryInsert(weapon, "flashlight", light, user: null), Is.True);
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
