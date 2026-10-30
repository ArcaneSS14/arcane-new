using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.StationGoal;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Station.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Arcane.Server.StationGoal;

/// <summary>
///     Sends each station a random goal by fax when the round starts.
/// </summary>
public sealed partial class StationGoalSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private FaxSystem _fax = default!;
    [Dependency] private StationSystem _station = default!;

    private static readonly Color CentCommStampColor = Color.FromHex("#006600");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (args.New != GameRunLevel.InRound || args.Old != GameRunLevel.PreRoundLobby)
            return;

        if (!_cfg.GetCVar(ArcaneCVars.StationGoal))
            return;

        var players = _player.PlayerCount;
        var query = EntityQueryEnumerator<StationGoalComponent>();
        while (query.MoveNext(out var station, out var comp))
        {
            var goals = new List<ProtoId<StationGoalPrototype>>(comp.Goals);
            while (goals.Count > 0)
            {
                var goal = _proto.Index(_random.PickAndTake(goals));
                if (players < goal.MinPlayers || players > goal.MaxPlayers)
                    continue;

                if (SendStationGoal(station, goal))
                    Log.Info($"Goal {goal.ID} has been sent to station {ToPrettyString(station)}");
                break;
            }
        }
    }

    /// <summary>
    ///     Prints the goal on every fax of the station and on faxes that receive the goals of all stations.
    /// </summary>
    /// <returns>True if a fax of the station itself received the goal.</returns>
    public bool SendStationGoal(EntityUid station, StationGoalPrototype goal)
    {
        var printout = new FaxPrintout(
            Loc.GetString(goal.Text, ("station", Name(station))),
            Loc.GetString("station-goal-fax-paper-name"),
            stampState: "paper_stamp-centcom",
            stampedBy: [new StampDisplayInfo { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = CentCommStampColor }]);

        var sent = false;
        var query = EntityQueryEnumerator<StationGoalReceiverComponent, FaxMachineComponent>();
        while (query.MoveNext(out var uid, out var receiver, out var fax))
        {
            var own = _station.GetOwningStation(uid) == station;
            if (!own && !receiver.AllStations)
                continue;

            _fax.Receive(uid, printout, null, fax);

            var coords = Transform(uid).Coordinates;
            foreach (var spawn in goal.Spawns)
            {
                Spawn(spawn, coords);
            }

            sent |= own;
        }

        return sent;
    }
}
