using Content.Arcane.Common.ServerProtection;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.ServerProtection;

/// <summary>
///     Remembers who last changed each protection cvar, so toggles can be attributed in admin alerts.
/// </summary>
public sealed partial class ServerProtectionAuditSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ServerProtectionPunishmentSystem _punishment = default!;

    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(5);

    private readonly Dictionary<string, (string Actor, TimeSpan ChangedAt)> _changes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProtectionCVarChangedEvent>(OnCVarChanged);
    }

    private void OnCVarChanged(ref ProtectionCVarChangedEvent args)
    {
        if (Equals(args.OldValue, args.NewValue))
            return;

        var actor = args.Actor == null
            ? Loc.GetString("server-protection-unknown-actor")
            : $"{args.Actor.Name} ({args.Actor.UserId})";

        _changes[args.CVar] = (actor, _timing.CurTime);
    }

    /// <summary>
    ///     The player that changed the cvar in the last few seconds, or "unknown".
    /// </summary>
    public string GetRecentActor(string cvar)
    {
        return _changes.TryGetValue(cvar, out var change) && _timing.CurTime - change.ChangedAt <= MaxAge
            ? change.Actor
            : Loc.GetString("server-protection-unknown-actor");
    }

    /// <summary>
    ///     Tells admins whenever the bool cvar is toggled, and by whom.
    /// </summary>
    public void AnnounceToggles(CVarDef<bool> cvar, string systemName)
    {
        _cfg.OnValueChanged(cvar, enabled => AnnounceToggle(cvar.Name, systemName, enabled));
    }

    private void AnnounceToggle(string cvar, string systemName, bool enabled)
    {
        _punishment.SendAdminAlert(Loc.GetString(enabled ? "server-protection-toggled-on" : "server-protection-toggled-off",
            ("system", systemName),
            ("actor", GetRecentActor(cvar))));
    }
}
