#nullable enable
using System.Linq;
using Content.Arcane.Server.Silicons.Boris;
using Content.Goobstation.Shared.CustomLawboard;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Lock;
using Content.Shared.Mind;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Content.Shared.Wires;
using Content.Trauma.Common.Silicons.Borgs;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P27RoboticsRuntimeTest : InteractionTest
{
    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public void LawboardCapsCountAndLengthAndKeepsOrder()
    {
        var laws = Enumerable.Range(0, SharedCustomLawboardSystem.MaxLaws + 1)
            .Select(index => new SiliconLaw
            {
                LawString = "Test\r\n" + new string('x', 520),
                LawIdentifierOverride = new string('y', 511) + "\U0001F680",
                Order = index + 1,
            })
            .ToList();
        var result = SharedCustomLawboardSystem.SanitizeLaws(laws);
        Assert.That(result, Has.Count.EqualTo(15));
        Assert.That(result[0].LawString, Has.Length.EqualTo(512).And.StartsWith("Test  "));
        Assert.That(result[0].LawIdentifierOverride, Has.Length.EqualTo(511));
        Assert.That(result.Select(law => (int) law.Order), Is.EqualTo(Enumerable.Range(1, 15)));
        Assert.That(laws[0].LawString, Does.Contain("\r\n"));
    }

    [Test]
    public async Task BorisRejectsAnotherStationAndOccupiedShellAndReturnsOnBoardRemoval()
    {
        await Server.WaitAssertion(() =>
        {
            var stations = SEntMan.System<ServerStationSystem>();
            var station = SEntMan.SpawnAttachedTo("TestStation", Center);
            var otherStation = SEntMan.SpawnAttachedTo("TestStation", Center);
            stations.AddGridToStation(station, MapData.Grid);

            var core = SEntMan.SpawnAttachedTo("PlayerStationAiEmpty", Center);
            var brain = SEntMan.SpawnAttachedTo("StationAiBrain", Center);
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(core, StationAiCoreComponent.Container, brain, user: null), Is.True);
            var actions = SEntMan.System<SharedActionsSystem>();
            Assert.That(actions.GetActions(brain).Any(action => SEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID == "ActionOpenBorisDevices"), Is.True);
            var minds = SEntMan.System<SharedMindSystem>();
            var aiMind = minds.CreateMind(null, "Boris test AI");
            minds.TransferTo(aiMind, brain);
            var borg = SEntMan.SpawnAttachedTo("BorgChassisGeneric", Center);
            var battery = SEntMan.SpawnAttachedTo("PowerCellHigh", Center);
            var panel = SEntMan.GetComponent<WiresPanelComponent>(borg);
            SEntMan.System<LockSystem>().Unlock(borg, null);
            Assert.That(SEntMan.System<SharedWiresSystem>().TogglePanel(borg, panel, true), Is.True);
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(borg, "cell_slot", battery, user: null), Is.True);

            var board = SEntMan.SpawnAttachedTo("AiRemoteBrain", Center);
            var install = new BorgChassisInteractAfterEvent(borg, SPlayer);
            SEntMan.EventBus.RaiseLocalEvent(board, ref install);
            Assert.That(install.Handled, Is.True);
            var tracker = SEntMan.AddComponent<StationTrackerComponent>(borg);
            stations.SetStation((borg, tracker), otherStation);
            var boris = SEntMan.System<BorisSystem>();
            Assert.That(boris.TryControl(brain, borg), Is.False);
            stations.SetStation((borg, tracker), station);

            var otherMind = minds.CreateMind(null, "Boris test occupant");
            minds.TransferTo(otherMind, borg);
            Assert.That(boris.TryControl(brain, borg), Is.False);
            Assert.That(minds.TryGetMind(borg, out var occupant, out _) && occupant == otherMind.Owner, Is.True);
            minds.TransferTo(otherMind, null, createGhost: false);

            var radio = SEntMan.GetComponent<ActiveRadioComponent>(borg);
            var channels = radio.Channels.ToArray();
            Assert.That(boris.TryControl(brain, borg), Is.True);
            Assert.That(minds.TryGetMind(borg, out var controlled, out _) && controlled == aiMind.Owner, Is.True);
            Assert.That(minds.TryGetMind(brain, out _, out _), Is.False);
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainingContainer(board, out var slot), Is.True);
            Assert.That(containers.Remove(board, slot!), Is.True);
            Assert.That(minds.TryGetMind(brain, out var returned, out _) && returned == aiMind.Owner, Is.True);
            Assert.That(minds.TryGetMind(borg, out _, out _), Is.False);
            Assert.That(radio.Channels, Is.EquivalentTo(channels));
            Assert.That(SEntMan.GetComponent<StationAiCoreComponent>(core).Remote, Is.True);
        });
    }

    [Test]
    public async Task SecurityCyborgStartsWithDepartmentRadioAndHailer()
    {
        await Server.WaitAssertion(() =>
        {
            var borg = SEntMan.SpawnAttachedTo("BorgChassisSEC", Center);
            var radio = SEntMan.GetComponent<ActiveRadioComponent>(borg);
            Assert.That(radio.Channels.Select(channel => channel.Id), Does.Contain("Security"));
            var actions = SEntMan.System<SharedActionsSystem>().GetActions(borg);
            Assert.That(actions.Count(action => SEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID == "ActionHailer"), Is.EqualTo(1));
            Assert.That(SEntMan.System<ItemSlotsSystem>().GetItemOrNull(borg, "cell_slot"), Is.Not.Null);
        });
    }
}
