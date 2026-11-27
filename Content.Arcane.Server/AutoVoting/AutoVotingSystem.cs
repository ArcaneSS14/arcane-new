using Content.Shared.GameTicking.Events;
using System.Linq;
using Content.Arcane.Server.GameTicking;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Prototypes;
using Content.Server.Voting.Managers;
using Content.Arcane.Common.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Random.Helpers;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.AutoVoting;

public sealed partial class AutoVotingSystem : EntitySystem
{
    [Dependency] private IVoteManager _voteManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ServerGameTicker _ticker = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IChatManager _chat = default!;

    private bool _enabled;
    private readonly GamePresetHistory _history = new();
    private int _restartGeneration;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, ArcaneCVars.AutoVotingEnabled, SetAutoVotingEnabled, true);
        Subs.CVar(_cfg, ArcaneCVars.GamePresetHistorySize, size => _history.Capacity = size, true);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);
    }

    private void SetAutoVotingEnabled(bool value)
    {
        _enabled = value;
    }

    private void OnRoundStarted(RoundStartingEvent args)
    {
        if (_ticker.CurrentPreset is { } preset)
            _history.Record(preset.ID);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        var generation = ++_restartGeneration;
        if (!_enabled || !_ticker.LobbyEnabled || _ticker.DummyTicker)
            return;

        Timer.Spawn(TimeSpan.FromSeconds(25), () =>
        {
            if (!_enabled || generation != _restartGeneration || !_ticker.CanUpdateMap())
                return;

            _voteManager.CreateStandardVote(null, StandardVoteType.Map);
            TrySelectGamePreset();
        });
    }

    private bool TrySelectGamePreset()
    {
        var weights = new Dictionary<GamePresetPrototype, float>();
        foreach (var preset in _prototypes.EnumeratePrototypes<GamePresetPrototype>())
        {
            if (!preset.ShowInVote ||
                _players.PlayerCount < (preset.MinPlayers ?? int.MinValue) ||
                _players.PlayerCount > (preset.MaxPlayers ?? int.MaxValue))
                continue;

            weights.Add(preset, _history.GetWeight(preset.ID,
                preset.RepeatWeightMultiplier,
                preset.PreviousRoundWeightMultiplier));
        }

        if (weights.Count == 0)
            return false;

        var selected = _random.Pick(weights);
        _ticker.SetGamePreset(selected);

        _chat.DispatchServerAnnouncement(Loc.GetString("game-preset-random-selected",
            ("preset", Loc.GetString(selected.ModeTitle))));

        var totalWeight = weights.Values.Sum();
        var chances = weights.Select(entry => Loc.GetString("game-preset-random-chance",
            ("preset", Loc.GetString(entry.Key.ModeTitle)),
            ("chance", entry.Value / totalWeight * 100f)));

        _chat.DispatchServerAnnouncement(Loc.GetString("game-preset-random-chances",
            ("chances", string.Join("\n", chances))));

        return true;
    }
}
