using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.SSDIndicator;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.SSD;

/// <summary>
/// Reports network absence independently of the optional in-character SSD sleep timer.
/// </summary>
public sealed partial class SSDTimerSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _disconnectedAt = new();

    public override void Initialize()
    {
        base.Initialize();
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<SSDIndicatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _disconnectedAt.Clear();
        base.Shutdown();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _disconnectedAt.TryAdd(args.Session.UserId, _timing.RealTime);
        else
            _disconnectedAt.Remove(args.Session.UserId);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _disconnectedAt.Clear();
    }

    private void OnExamined(Entity<SSDIndicatorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange
            || !ent.Comp.IsSSD
            || !_mind.TryGetMind(ent, out _, out var mind)
            || mind.OwnedEntity != ent.Owner
            || mind.UserId is not { } user
            || !_disconnectedAt.TryGetValue(user, out var disconnectedAt))
        {
            return;
        }

        // Body changes and temporary ghost visits are not network disconnections.
        var minutes = Math.Max(0, (int) (_timing.RealTime - disconnectedAt).TotalMinutes);
        args.PushMarkup(Loc.GetString("arcane-ssd-duration-examined", ("minutes", minutes)));
    }
}
