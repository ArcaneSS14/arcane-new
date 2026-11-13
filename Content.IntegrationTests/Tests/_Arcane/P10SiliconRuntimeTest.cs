#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.PAI;
using Content.Shared.Power;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Content.Shared.StationAi;
using Content.Trauma.Shared.Language.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P10SiliconRuntimeTest : InteractionTest
{
    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [TestCase("BorgChassisGeneric", true)]
    [TestCase("BorgChassisSEC", true)]
    [TestCase("BorgChassisSyndicateAssault", true)]
    [TestCase("XenoborgEngi", true)]
    [TestCase("PersonalAI", false)]
    [TestCase("SyndicatePersonalAI", false)]
    [TestCase("MMI", false)]
    [TestCase("PositronicBrain", false)]
    [TestCase("Drone", false)]
    [TestCase("MobMineBot", false)]
    public async Task BinaryAccessMatchesTheChassisOrDevice(string id, bool expected)
    {
        await Server.WaitAssertion(() =>
        {
            var entity = SEntMan.SpawnAttachedTo(new EntProtoId(id), Center);
            var canReceive = SEntMan.TryGetComponent<ActiveRadioComponent>(entity, out var radio) && radio.Channels.Any(channel => channel.Id == "Binary");
            var canSend = SEntMan.TryGetComponent<IntrinsicRadioTransmitterComponent>(entity, out var transmitter) && transmitter.Channels.Any(channel => channel.Id == "Binary");
            Assert.That(canReceive, Is.EqualTo(expected));
            Assert.That(canSend, Is.EqualTo(expected));
        });
    }

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
    public async Task StationAiKnowsCrewLanguagesButNotFactionLanguage()
    {
        await Server.WaitAssertion(() =>
        {
            var brain = SEntMan.SpawnAttachedTo("StationAiBrain", Center);
            var language = SEntMan.System<SharedLanguageSystem>();
            Assert.That(language.GetSpokenLanguages(brain).Select(id => id.Id), Does.Contain("DroneTalk").And.Contain("Bubblish"));
            Assert.That(language.CanUnderstand(brain, "Schechi"), Is.True);
            Assert.That(language.GetUnderstoodLanguages(brain).Select(id => id.Id), Does.Not.Contain("OldFastCodes"));
        });
    }

    [Test]
    public async Task BorgCrewLanguagesFollowTheChassisThroughBrainReplacement()
    {
        await Server.WaitAssertion(() =>
        {
            var brain = SEntMan.SpawnAttachedTo("PositronicBrain", Center);
            var borg = SEntMan.SpawnAttachedTo("BorgChassisGeneric", Center);
            var language = SEntMan.System<SharedLanguageSystem>();
            Assert.That(language.CanSpeak(brain, "Bubblish"), Is.False);

            var containers = SEntMan.System<SharedContainerSystem>();
            var slot = SEntMan.GetComponent<BorgChassisComponent>(borg).BrainContainer;
            for (var index = 0; index < 2; index++)
            {
                Assert.That(containers.Insert(brain, slot), Is.True);
                Assert.That(language.CanUnderstand(borg, "Bubblish"), Is.True);
                Assert.That(language.CanSpeak(borg, "OldFastCodes"), Is.False);
                Assert.That(containers.Remove(brain, slot), Is.True);
                Assert.That(language.CanSpeak(brain, "Bubblish"), Is.False);
            }
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

    [Test]
    public async Task AutomaticCameraIlluminationPreservesManualLightAndPower()
    {
        await Server.WaitAssertion(() =>
        {
            var camera = SEntMan.SpawnAttachedTo("SurveillanceCameraEngineering", Center);
            var power = SEntMan.GetComponent<ApcPowerReceiverComponent>(camera);
            var lights = SEntMan.System<SlimPoweredLightSystem>();
            Assert.That(SEntMan.System<SharedPointLightSystem>().TryGetLight(camera, out var light), Is.True);

            power.Powered = true;
            lights.SetAiIllumination(camera, true);
            Assert.That(light!.Enabled, Is.True);
            lights.SetEnabled(camera, true);
            lights.SetAiIllumination(camera, false);
            Assert.That(light.Enabled, Is.True);

            lights.SetAiIllumination(camera, true);
            lights.SetEnabled(camera, false);
            Assert.That(light.Enabled, Is.True);

            power.Powered = false;
            var powerOff = new PowerChangedEvent(false, 0f);
            SEntMan.EventBus.RaiseLocalEvent(camera, ref powerOff);
            Assert.That(light.Enabled, Is.False);
            power.Powered = true;
            var powerOn = new PowerChangedEvent(true, 5f);
            SEntMan.EventBus.RaiseLocalEvent(camera, ref powerOn);
            Assert.That(light.Enabled, Is.True);
            lights.SetAiIllumination(camera, false);
            Assert.That(light.Enabled, Is.False);
        });
    }
}
