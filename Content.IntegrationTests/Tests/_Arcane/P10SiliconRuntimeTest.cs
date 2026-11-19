#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Silicons.StationAi;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.PAI;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Content.Shared.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P10SiliconRuntimeTest : InteractionTest
{
    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public async Task PersonalAiCameraStartsOffAndResetsWhenItsMindLeaves()
    {
        await Server.WaitAssertion(() =>
        {
            var device = SEntMan.SpawnAttachedTo("PersonalAI", Center);
            var syndicate = SEntMan.SpawnAttachedTo("SyndicatePersonalAI", Center);
            var vision = SEntMan.GetComponent<StationAiVisionComponent>(device);
            var minds = SEntMan.System<SharedMindSystem>();
            var mind = minds.CreateMind(null, "Personal AI test");
            var actions = SEntMan.System<SharedActionsSystem>();

            Assert.That(vision.Enabled, Is.False);
            minds.TransferTo(mind, device);
            var action = SEntMan.GetComponent<PAIComponent>(device).CameraActionEntity;
            Assert.That(action, Is.Not.Null);
            Assert.That(actions.PerformAction(device, actions.GetAction(action!.Value)!.Value, predicted: false), Is.True);
            Assert.That(vision.Enabled, Is.True);
            minds.TransferTo(mind, syndicate);
            Assert.That(vision.Enabled, Is.False);
            Assert.That(SEntMan.GetComponent<PAIComponent>(syndicate).CameraActionEntity, Is.Null);
            minds.TransferTo(mind, device);
            Assert.That(vision.Enabled, Is.False);
        });
    }

    [Test]
    public async Task StationAiReleaseRestoresOnlyItsOwnAssignmentOnce()
    {
        await Server.WaitAssertion(() =>
        {
            var jobs = SEntMan.System<ServerStationJobsSystem>();
            var first = SEntMan.SpawnAttachedTo("TestStation", Center);
            var second = SEntMan.SpawnAttachedTo("TestStation", Center);
            var user = new NetUserId(Guid.NewGuid());
            foreach (var station in new[] { first, second })
            {
                Assert.That(jobs.TrySetJobSlot(station, "StationAi", 1, createSlot: true), Is.True);
                Assert.That(jobs.TryAssignJob(station, "StationAi", user), Is.True);
            }
            Assert.That(jobs.TrySetJobSlot(first, "Passenger", 1, createSlot: true), Is.True);
            Assert.That(jobs.TryAssignJob(first, "Passenger", user), Is.True);

            Assert.That(jobs.TryReleasePlayerJob(first, user, "StationAi"), Is.True);
            Assert.That(jobs.TryReleasePlayerJob(first, user, "StationAi"), Is.False);
            Assert.That(SEntMan.GetComponent<StationJobsComponent>(first).JobList["StationAi"], Is.EqualTo(1));
            Assert.That(SEntMan.GetComponent<StationJobsComponent>(second).JobList["StationAi"], Is.Zero);
            Assert.That(jobs.TryGetPlayerJobs(first, user, out var assignments), Is.True);
            Assert.That(assignments!.Select(job => job.Id), Is.EqualTo(new[] { "Passenger" }));
        });
    }

    // Arcane-Edit-Start
    [Test]
    public async Task StationAiRetirementChecksTheCoreAgainBeforeReleasingItsSlot()
    // Arcane-Edit-End
    {
        EntityUid ai = default;
        EntityUid core = default;
        EntityUid station = default;
        EntityUid mindId = default;
        await Server.WaitAssertion(() =>
        {
            var stations = SEntMan.System<ServerStationSystem>();
            var jobs = SEntMan.System<ServerStationJobsSystem>();
            var minds = SEntMan.System<SharedMindSystem>();
            var player = ServerSession!;
            station = SEntMan.SpawnAttachedTo("TestStation", Center);
            stations.AddGridToStation(station, MapData.Grid);
            Assert.That(jobs.TrySetJobSlot(station, "StationAi", 1, createSlot: true), Is.True);
            Assert.That(jobs.TryAssignJob(station, "StationAi", player.UserId), Is.True);

            core = SEntMan.SpawnAttachedTo("PlayerStationAiEmpty", Center);
            ai = SEntMan.SpawnAttachedTo("StationAiBrain", Center);
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(core, StationAiCoreComponent.Container, ai, user: null), Is.True);
            mindId = minds.CreateMind(player.UserId, "Retiring station AI");
            minds.TransferTo(mindId, ai);
            Assert.That(player.AttachedEntity, Is.EqualTo(ai));

            // Arcane-Edit-Start
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainingContainer(ai, out var holder), Is.True);
            Assert.That(containers.Remove(ai, holder!), Is.True);
            Assert.That(SEntMan.GetComponent<Content.Shared.Movement.Components.InputMoverComponent>(ai).CanMove, Is.False);
            var retirement = SEntMan.System<StationAiSystem>();
            retirement.RetireStationAi(player, ai, core, station, mindId);
            Assert.That(SEntMan.GetComponent<StationJobsComponent>(station).JobList["StationAi"], Is.Zero);
            Assert.That(player.AttachedEntity, Is.EqualTo(ai));

            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(core, StationAiCoreComponent.Container, ai, user: null), Is.True);
            retirement.RetireStationAi(player, ai, core, station, mindId);
            retirement.RetireStationAi(player, ai, core, station, mindId);
            Assert.That(SEntMan.GetComponent<StationJobsComponent>(station).JobList["StationAi"], Is.EqualTo(1));
            // Arcane-Edit-End
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(ai));
            Assert.That(SEntMan.TryGetComponent<GhostComponent>(player.AttachedEntity, out var ghost), Is.True);
            Assert.That(ghost!.CanReturnToBody, Is.False);
        });

        await RunSeconds(0.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(ai), Is.False); // Arcane-Edit
            Assert.That(SEntMan.EntityExists(core), Is.True);
            SEntMan.System<SharedMindSystem>().WipeMind(mindId);
        });
    }

}
