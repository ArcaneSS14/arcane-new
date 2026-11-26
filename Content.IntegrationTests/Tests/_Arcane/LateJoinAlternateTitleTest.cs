// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Common.CCVar;
using Content.Client.UserInterface.Systems.Chat;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class LateJoinAlternateTitleTest : GameTest
{
    private const string Map = "TestLateJoinAlternateTitle";
    private const string Job = "Quartermaster";
    private const string Foreman = "QuartermasterSupplyForeman";

    public override PoolSettings PoolSettings => new() { DummyTicker = false, Dirty = true, InLobby = true };

    [TestPrototypes]
    private const string Prototypes = """
- type: gameMap
  id: TestLateJoinAlternateTitle
  mapName: TestLateJoinAlternateTitle
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
      - type: StationNameSetup
        mapNameTemplate: Test Station
      - type: StationJobs
        availableJobs:
          Quartermaster: [-1, -1]
""";

    [TestCase(true, Foreman, Gender.Male, "job-name-alt-qm-2-m")]
    [TestCase(true, Foreman, Gender.Female, "job-name-alt-qm-2-f")]
    [TestCase(false, Foreman, Gender.Female, "job-name-qm")]
    [TestCase(true, null, Gender.Male, "job-name-qm")]
    [TestCase(true, "JanitorMaid", Gender.Female, "job-name-qm")]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameRoleTimers), false)]
    public async Task LateJoinAnnouncementUsesAllowedTitleOrJobFallback(bool enabled, string? title, Gender gender, string expectedTitleKey)
    {
        await OverrideCVar(Side.Server, CCVars.ICRandomCharacters, false);
        var user = Client.User!.Value;
        var preferences = Server.ResolveDependency<IServerPreferencesManager>();
        var ticker = SEntMan.System<ServerGameTicker>();
        var configuration = Server.ResolveDependency<IConfigurationManager>();
        var savedPreferences = preferences.GetPreferences(user);
        Assert.That(savedPreferences.SelectedCharacterIndex, Is.Zero);
        var original = (HumanoidCharacterProfile) savedPreferences.SelectedCharacter;
        var originalEnabled = configuration.GetCVar(ArcaneCVars.AlternateJobTitlesEnabled);
        string expected = string.Empty;

        try
        {
            Task saved = Task.CompletedTask;
            await Server.WaitPost(() =>
            {
                configuration.SetCVar(CCVars.GameMap, Map);
                configuration.SetCVar(ArcaneCVars.AlternateJobTitlesEnabled, enabled);
                ProtoId<JobAlternateTitlePrototype>? selectedTitle = title == null ? null : new(title);
                var profile = original.WithGender(gender).WithJobAltTitle(Job, selectedTitle);
                saved = preferences.SetProfile(user, 0, profile);
            });
            await saved;
            await Server.WaitPost(() =>
            {
                ticker.ToggleReadyAll(false);
                ticker.SetGamePreset(ticker.FindGamePreset("sandbox")!);
                ticker.StartRound(force: true);
            });
            await Pair.RunTicksSync(10);
            await Client.WaitPost(() => Client.ResolveDependency<IUserInterfaceManager>().GetUIController<ChatUIController>().History.Clear());

            await Server.WaitAssertion(() =>
            {
                Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
                var station = SEntMan.System<ServerStationSystem>().GetStations().Single(station =>
                    SEntMan.TryGetComponent<StationJobsComponent>(station, out var jobs) && jobs.SetupAvailableJobs.ContainsKey(Job));
                var session = Server.PlayerMan.GetSessionById(user);
                ticker.MakeJoinGame(session, station, Job);
                Assert.That(session.AttachedEntity, Is.Not.Null);
                Assert.That(ticker.PlayerGameStatuses[user], Is.EqualTo(PlayerGameStatus.JoinedGame));
                var mob = session.AttachedEntity!.Value;
                expected = Loc.GetString("latejoin-arrival-announcement",
                    ("character", SEntMan.GetComponent<MetaDataComponent>(mob).EntityName),
                    ("entity", mob),
                    ("job", Loc.GetString(expectedTitleKey)));
            });
            await Pair.RunTicksSync(10);
            await Client.WaitAssertion(() =>
            {
                var history = Client.ResolveDependency<IUserInterfaceManager>().GetUIController<ChatUIController>().History;
                Assert.That(history.Any(entry => entry.Msg.Channel == ChatChannel.Radio && entry.Msg.Message == expected), Is.True,
                    $"The client did not receive the expected late-join announcement: {expected}");
            });
        }
        finally
        {
            Task restored = Task.CompletedTask;
            await Server.WaitPost(() =>
            {
                ticker.RestartRound();
                configuration.SetCVar(ArcaneCVars.AlternateJobTitlesEnabled, originalEnabled);
                restored = preferences.SetProfile(user, 0, original);
            });
            await restored;
        }
    }
}
// Arcane-End
