// <Trauma>
using Content.Goobstation.Common.Pirates;
using Content.Goobstation.Common.Research;
using Robust.Shared.Prototypes;
using System.Linq;
// </Trauma>
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research; // Arcane

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private IdentitySystem _identity = default!;

    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseSynchronizedEvent>(OnConsoleDatabaseSynchronized);
//        SubscribeLocalEvent<ResearchConsoleComponent, GotEmaggedEvent>(OnEmagged); // Arcane-Edit
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        // goob edit - spirates
        var eqe = EntityQueryEnumerator<ResourceSiphonComponent>();
        while (eqe.MoveNext(out var siphon))
        {
            if (siphon.Active)
            {
                _popup.PopupEntity(Loc.GetString("console-block-something"), args.Actor);
                return;
            }
        }
        // goob edit end

        var act = args.Actor;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(args.Id, out var technologyPrototype))
            return;

        if (TryComp<AccessReaderComponent>(uid, out var access) && !_accessReader.IsAllowed(act, uid, access))
        {
            _popup.PopupEntity(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        if (!UnlockTechnology(uid, args.Id, act))
        // Arcane-Edit-Start
        {
            _popup.PopupEntity(Loc.GetString("research-console-unlock-failed-popup"), act);
            return;
        }
        // Arcane-Edit-End

        if (!_emag.CheckFlag(uid, EmagType.Interaction) && technologyPrototype.AnnounceOnUnlock) // Arcane-Edit
        {
            var costText = FormatResearchPointAmounts(technologyPrototype.PointCosts); // Arcane
            var message = Loc.GetString(
                "research-console-unlock-technology-radio-broadcast",
                ("technology", Loc.GetString(technologyPrototype.Name)),
                ("amount", costText), // Arcane-Edit: technologyPrototype.Cost > costText
                ("approver", _identity.GetIdentityShortInfo(act, uid) ?? string.Empty)
            );
            // Arcane-Edit-Start: announce on every channel of the technology
            var announceChannels = technologyPrototype.AnnounceChannels.Count > 0
                ? technologyPrototype.AnnounceChannels
                : [component.AnnouncementChannel];

            foreach (var channel in announceChannels)
            {
                _radio.SendRadioMessage(uid, message, channel, uid, escapeMarkup: false);
            }
            // Arcane-Edit-End
            SayTechnologyUnlocked(uid, technologyPrototype, costText, _identity.GetIdentityShortInfo(act, uid) ?? string.Empty); // Arcane
        }

        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleBeforeUiOpened(EntityUid uid, ResearchConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        SyncClientWithServer(uid);
    }

    private void UpdateConsoleInterface(EntityUid uid, ResearchConsoleComponent? component = null, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(uid, ref component, ref clientComponent, false))
            return;

        // R&D Console Rework Start
        var allTechs = ProtoMan.EnumeratePrototypes<TechnologyPrototype>().ToList();
        Dictionary<string, ResearchAvailability> techList;
        // Arcane-Start
        Dictionary<string, ResearchTechnologyLockReason> lockReasons;
        List<ResearchConsoleExperimentData> experiments;
        var networkId = string.Empty;
        List<ResearchPointAmount> pointBalances = new();
        List<ResearchLogEntry> logs = new();
        TechnologyDatabaseComponent? syncedDb = null;
        // Arcane-End
        var points = 0;

        if (TryGetClientServer(uid, out var serverUid, out var server, clientComponent) &&
            TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
        {
/* // Arcane-Edit
            var unlockedTechs = new HashSet<ProtoId<TechnologyPrototype>>(db.UnlockedTechnologies);
            techList = allTechs.ToDictionary(
                proto => proto.ID,
                proto =>
*/
            // Arcane-Start
            syncedDb = db;
            networkId = server.NetworkId;
            pointBalances = server.PointBalances.ToList();
            logs = server.Logs.ToList();
            var visible = new HashSet<ProtoId<TechnologyPrototype>>(db.VisibleTechnologies);
            var available = new HashSet<ProtoId<TechnologyPrototype>>(db.AvailableTechnologies);
            var researched = new HashSet<ProtoId<TechnologyPrototype>>(db.ResearchedTechnologies);

            techList = visible.ToDictionary(
                techId => techId.ToString(),
                techId =>
            // Arcane-End
                {
                    if (researched.Contains(techId)) // Arcane-Edit
                        return ResearchAvailability.Researched;

/* // Arcane-Edit
                    var prereqsMet = proto.TechnologyPrerequisites.All(p => unlockedTechs.Contains(p));
                    var canAfford = server.Points >= proto.Cost;
*/

                    // Arcane-Start
                    if (!available.Contains(techId))
                        return ResearchAvailability.PrereqsMet;

                    var proto = ProtoMan.Index(techId);
                    var allCosts = GetTechnologyFinalPointCosts(db, proto);
                    var canAfford = HasSufficientPoints(serverUid.Value, allCosts, server);
                    // Arcane-End

                    return canAfford // Arcane-Edit
                        ? ResearchAvailability.Available // Arcane-Edit
                        : ResearchAvailability.Unavailable;
                });

            // Arcane-Start
            lockReasons = ProtoMan.EnumeratePrototypes<TechnologyPrototype>()
                .Where(proto => db.SupportedDisciplines.Contains(proto.Discipline))
                .ToDictionary(proto => proto.ID,
                    proto =>
                {
                    var reason = GetTechnologyLockReason(db, proto);
                    if (reason != ResearchTechnologyLockReason.None)
                        return reason;

                    var costs = GetTechnologyFinalPointCosts(db, proto);

                    if (!HasSufficientPoints(serverUid.Value, costs, server) && !db.ResearchedTechnologies.Contains(proto.ID))
                        return ResearchTechnologyLockReason.InsufficientPoints;

                    return reason;
                });

            experiments = BuildExperimentUiData(db);
            // Arcane-End

            if (clientComponent != null)
                points = clientComponent.ConnectedToServer ? server.Points : 0;
        }
        else
        {
            techList = new Dictionary<string, ResearchAvailability>(); // Arcane-Edit
            // Arcane-Start
            lockReasons = new Dictionary<string, ResearchTechnologyLockReason>();
            experiments = new List<ResearchConsoleExperimentData>();
            // Arcane-End
        }

        // Arcane-Edit-Start
        _uiSystem.SetUiState(uid,
            ResearchConsoleUiKey.Key,
            new ResearchConsoleBoundInterfaceState(
                points,
                techList,
                syncedDb?.VisibleTechnologies.ToList() ?? new List<ProtoId<TechnologyPrototype>>(),
                syncedDb?.AvailableTechnologies.ToList() ?? new List<ProtoId<TechnologyPrototype>>(),
                syncedDb?.ResearchedTechnologies.ToList() ?? new List<ProtoId<TechnologyPrototype>>(),
                syncedDb?.CompletedExperiments.ToList() ?? new List<string>(),
                experiments,
                lockReasons,
                networkId,
                pointBalances,
                logs));
        // Arcane-Edit-End
        // R&D Console Rework End
    }

    // Arcane-Start
    private List<ResearchConsoleExperimentData> BuildExperimentUiData(TechnologyDatabaseComponent database)
    {
        var data = new List<ResearchConsoleExperimentData>();
        foreach (var experiment in ProtoMan.EnumeratePrototypes<ResearchExperimentPrototype>())
        {
            if (experiment.Hidden)
                continue;

            var progress = database.ExperimentProgress.FirstOrDefault(p => p.ExperimentId == experiment.ID);
            var state = ResearchExperimentState.Unavailable;

            if (database.SkippedExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Skipped;
            else if (database.CompletedExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Completed;
            else if (database.ActiveExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Active;
            else if (database.AvailableExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Available;

            if (state == ResearchExperimentState.Unavailable)
                continue;

            var target = progress.Target > 0 ? progress.Target : Math.Max(1, experiment.Objective.Target);
            data.Add(new ResearchConsoleExperimentData(experiment.ID, progress.Progress, target, state));
        }

        return data.OrderByDescending(e => e.State == ResearchExperimentState.Active)
            .ThenBy(e => e.Id)
            .ToList();
    }
    // Arcane-End

    private void OnPointsChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(uid, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleRegistrationChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseModified(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseSynchronized(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseSynchronizedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

/* // Arcane-Edit
    private void OnEmagged(Entity<ResearchConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }
*/

    // Arcane-Start
    private static string BuildCorruptedMessage(string message)
    {
        const string symbols = "!@#$%^&*?№";
        var chars = message.ToCharArray();
        var replaced = 0;

        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetter(chars[i]) || Random.Shared.NextDouble() > 0.24)
                continue;

            chars[i] = symbols[Random.Shared.Next(symbols.Length)];
            replaced++;
        }

        if (replaced != 0)
            return new string(chars);
        {
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetter(chars[i]))
                    continue;

                chars[i] = symbols[Random.Shared.Next(symbols.Length)];
                break;
            }
        }

        return new string(chars);
    }
    // Arcane-End
}
