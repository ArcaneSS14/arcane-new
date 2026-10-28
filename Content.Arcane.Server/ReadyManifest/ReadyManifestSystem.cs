using Content.Arcane.Shared.ReadyManifest;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.ReadyManifest;

/// <summary>
/// Shows lobby players which characters are ready for which jobs.
/// </summary>
public sealed partial class ReadyManifestSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;

    /// <summary>
    /// Medium-priority jobs with at least this weight (heads) are listed too.
    /// </summary>
    private const int MinJobWeightForAutoInclude = 10;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly Dictionary<ICommonSession, ReadyManifestEui> _openEuis = new();
    private TimeSpan _nextRefresh;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestReadyManifestMessage>(OnRequest);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_openEuis.Count == 0 || _timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + RefreshInterval;
        foreach (var eui in _openEuis.Values)
        {
            eui.StateDirty();
        }
    }

    private void OnRequest(RequestReadyManifestMessage message, EntitySessionEventArgs args)
    {
        if (!_cfg.GetCVar(CCVars.CrewManifestWithoutEntity) || _openEuis.ContainsKey(args.SenderSession))
            return;

        var eui = new ReadyManifestEui(this);
        _openEuis.Add(args.SenderSession, eui);
        _eui.OpenEui(eui, args.SenderSession);
        eui.StateDirty();
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        foreach (var eui in _openEuis.Values)
        {
            eui.Close();
        }

        _openEuis.Clear();
    }

    public void OnEuiClosed(ICommonSession session)
    {
        _openEuis.Remove(session);
    }

    public Dictionary<ProtoId<JobPrototype>, List<string>> BuildManifest()
    {
        var weights = _proto.TryIndex(JobWeightPrototype.Default, out var weightProto)
            ? weightProto.Weights
            : new Dictionary<ProtoId<JobPrototype>, int>();

        var manifest = new Dictionary<ProtoId<JobPrototype>, List<string>>();
        foreach (var (userId, status) in _gameTicker.PlayerGameStatuses)
        {
            if (status != PlayerGameStatus.ReadyToPlay
                || !_prefs.TryGetCachedPreferences(userId, out var preferences)
                || preferences.SelectedCharacter is not { } profile)
                continue;

            foreach (var (job, priority) in profile.JobPriorities)
            {
                var include = priority == JobPriority.High
                              || priority > JobPriority.Never && weights.GetValueOrDefault(job) >= MinJobWeightForAutoInclude;
                if (!include)
                    continue;

                var names = manifest.GetOrNew(job);
                if (!names.Contains(profile.Name))
                    names.Add(profile.Name);
            }
        }

        return manifest;
    }
}
