#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Resist;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
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
// Arcane-Start
using Content.Arcane.Shared.Security;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.EntitySerialization.Systems;
// Arcane-End

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P13SecurityRuntimeTest : InteractionTest
{
    private static readonly EntProtoId Jumpsuit = "ClothingUniformJumpsuitColorGrey";
    private static readonly EntProtoId Human = "MobHuman";

    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    // Arcane-Start
    [Test]
    public async Task NewPanicButtonSurvivesMapSaveAndLoadWithLegacyDeletionRules()
    {
        await Server.WaitAssertion(() =>
        {
            var maps = SEntMan.System<SharedMapSystem>();
            var loader = SEntMan.System<MapLoaderSystem>();
            var path = new ResPath("/Maps/Test/security-panic-button.yml");
            Server.ResolveDependency<IResourceManager>().UserData.CreateDir(path.Directory);
            var mapEntity = maps.CreateMap(out var mapId);
            SEntMan.SpawnAttachedTo("SecurityPanicButton", new EntityCoordinates(mapEntity, Vector2.Zero));
            Assert.That(loader.TrySaveMap(mapId, path), Is.True);
            maps.DeleteMap(mapId);
            Assert.That(loader.TryLoadMap(path, out var loaded, out _), Is.True);
            var count = 0;
            var buttons = SEntMan.EntityQueryEnumerator<PanicButtonComponent, TransformComponent>();
            while (buttons.MoveNext(out _, out _, out var transform))
            {
                if (transform.MapID == loaded!.Value.Comp.MapId)
                    count++;
            }
            Assert.That(count, Is.EqualTo(1));
            maps.DeleteMap(loaded!.Value.Comp.MapId);
        });
    }
    // Arcane-End

    [Test]
    public async Task DeniedPanicButtonUseDoesNotStartCooldown()
    {
        await Server.WaitAssertion(() =>
        {
            var stations = SEntMan.System<ServerStationSystem>();
            var station = SEntMan.SpawnAttachedTo(new EntProtoId("TestStation"), Center);
            stations.AddGridToStation(station, MapData.Grid);
            var user = SEntMan.SpawnAttachedTo(Human, Center);
            var button = SEntMan.SpawnAttachedTo(new EntProtoId("SecurityPanicButton"), Center); // Arcane-Edit
            SEntMan.AddComponent<TelecomExemptComponent>(button);
            var receiver = SEntMan.SpawnAttachedTo(null, Center);
            SEntMan.AddComponent<ActiveRadioComponent>(receiver).ReceiveAllChannels = true;
            var probe = SEntMan.AddComponent<P09RadioProbeComponent>(receiver);
            probe.ExpectedSource = button;
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(user, button, checkActionBlocker: false), Is.True);
            Assert.That(SEntMan.System<SharedInteractionSystem>().UseInHandInteraction(user, button), Is.True);
            Assert.That(SEntMan.System<UseDelaySystem>().IsDelayed(button), Is.False);
            Assert.That(probe.Channels, Is.Empty);
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
            var button = SEntMan.SpawnAttachedTo(new EntProtoId("SecurityPanicButton"), Center); // Arcane-Edit
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
            var user = SEntMan.SpawnAttachedTo(Human, Center);
            var change = new SuitSensorChangeDoAfterEvent(SuitSensorMode.SensorCords);
            var doAfter = new DoAfterArgs(SEntMan, user, 0.5f, change, uniform, target: uniform);
            Assert.That(SEntMan.System<SharedDoAfterSystem>().TryStartDoAfter(doAfter), Is.True);
            Assert.That(emp.TryEmpEffects(uniform, 0, TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(SEntMan.GetComponent<SuitSensorComponent>(uniform).Mode, Is.EqualTo(SuitSensorMode.SensorOff));
        });

        await RunSeconds(0.7f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<SuitSensorComponent>(uniform).Mode, Is.EqualTo(SuitSensorMode.SensorOff));
            Assert.That(SEntMan.System<SharedEmpSystem>().TryEmpEffects(uniform, 0, TimeSpan.FromSeconds(2)), Is.True);
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
}
