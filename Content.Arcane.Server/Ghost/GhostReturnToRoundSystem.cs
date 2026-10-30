using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.GameTicking;
using Content.Arcane.Shared.Ghost;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Ghost;

/// <summary>
/// Lets ghosts go back to the lobby after a delay and keeps them from rejoining as the same character
/// (Orion #54, #253, #261, #389, #396).
/// </summary>
public sealed partial class GhostReturnToRoundSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ServerGameTicker _ticker = default!;

    /// <summary>
    /// Names each returned player has already played this round.
    /// </summary>
    private readonly Dictionary<NetUserId, HashSet<string>> _playedNames = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeNetworkEvent<GhostReturnToRoundRequestEvent>(OnReturnRequest);
        SubscribeLocalEvent<LateJoinAttemptEvent>(OnLateJoinAttempt);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _playedNames.Clear());
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        var uid = args.Entity;

        // admin ghosts can already go anywhere
        if (!TryComp<GhostComponent>(uid, out var ghost) || ghost.CanGhostInteract || HasComp<GhostRespawnComponent>(uid))
            return;

        // TimeOfDeath is on the server's RealTime clock, the client only knows CurTime
        var dead = _timing.RealTime - ghost.TimeOfDeath;
        var left = TimeSpan.FromSeconds(_cfg.GetCVar(ArcaneCVars.GhostRespawnTime)) - dead;

        var respawn = AddComp<GhostRespawnComponent>(uid);
        respawn.RespawnAt = _timing.CurTime + (left > TimeSpan.Zero ? left : TimeSpan.Zero);
        Dirty(uid, respawn);
    }

    private void OnReturnRequest(GhostReturnToRoundRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is { } ghost)
            TryReturnToRound(ghost, args.SenderSession);
    }

    public bool TryReturnToRound(EntityUid ghost, ICommonSession session)
    {
        if (!TryComp<GhostRespawnComponent>(ghost, out var respawn) || !HasComp<GhostComponent>(ghost))
            return false;

        var maxPlayers = _cfg.GetCVar(ArcaneCVars.GhostRespawnMaxPlayers);
        if (_player.PlayerCount >= maxPlayers)
        {
            SendMessage(session, Loc.GetString("ghost-respawn-max-players", ("players", maxPlayers)));
            return false;
        }

        var left = respawn.RespawnAt - _timing.CurTime;
        if (left > TimeSpan.Zero)
        {
            SendMessage(session, Loc.GetString("ghost-respawn-time-left", ("time", left.ToString(@"mm\:ss"))));
            return false;
        }

        RememberPlayedNames(session.UserId);
        _ticker.Respawn(session);
        _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{session.Name} returned to the lobby as a ghost.");

        SendMessage(session, Loc.GetString("ghost-respawn-window-rules-footer"));
        return true;
    }

    private void RememberPlayedNames(NetUserId user)
    {
        if (!_playedNames.TryGetValue(user, out var names))
            _playedNames[user] = names = new HashSet<string>();

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mind))
        {
            if (mind.OriginalOwnerUserId == user && !string.IsNullOrWhiteSpace(mind.CharacterName))
                names.Add(mind.CharacterName);
        }
    }

    private void OnLateJoinAttempt(ref LateJoinAttemptEvent args)
    {
        if (args.Cancelled
            || !_playedNames.TryGetValue(args.Player.UserId, out var names)
            || _admin.IsAdmin(args.Player))
            return;

        foreach (var name in names)
        {
            var similarity = NameSimilarity(name, args.CharacterName);
            if (similarity < 50f)
                continue;

            var blocked = similarity >= 85f;
            _chat.SendAdminAlert(Loc.GetString("ghost-respawn-log-character-almost-same",
                ("player", args.Player.Name),
                ("try", !blocked),
                ("oldName", name),
                ("newName", args.CharacterName)));

            if (!blocked)
                continue;

            SendMessage(args.Player, Loc.GetString(name == args.CharacterName
                ? "ghost-respawn-same-character"
                : "ghost-respawn-same-character-slightly-changed-name"));
            args.Cancelled = true;
            return;
        }
    }

    /// <summary>
    /// Levenshtein similarity of two names in percent, case-insensitive.
    /// </summary>
    private static float NameSimilarity(string a, string b)
    {
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        if (a.Length == 0 || b.Length == 0)
            return 0f;

        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }

            (prev, cur) = (cur, prev);
        }

        return (1f - (float) prev[b.Length] / Math.Max(a.Length, b.Length)) * 100f;
    }

    private void SendMessage(ICommonSession session, string message)
    {
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, session.Channel, Color.Red);
    }
}
