using System.Linq;
using Content.Arcane.Common.Ghost;
using Content.Arcane.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Ghost.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SSDIndicator;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Ghost;

/// <summary>
/// Answers a ghost's warp request with who each player is and which entities are public antagonists (Orion #107, #121).
/// </summary>
public sealed partial class GhostWarpDetailsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityQuery<MindComponent> _mindQuery = default!;

    // Prototype data only changes on reload: the antagonist list with its component types resolved, and which
    // antagonists each entity prototype matches through itself or its parents. Mind and role state is read live.
    private List<(GlobalAntagonistPrototype Antag, List<Type> Components)>? _antagonists;
    private readonly Dictionary<string, HashSet<GlobalAntagonistPrototype>> _antagonistsByPrototype = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeNetworkEvent<GhostWarpsRequestEvent>(OnWarpsRequest);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindRemoved(EntityUid uid, MindContainerComponent comp, MindRemovedMessage args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        EnsureComp<LastMindComponent>(uid).Mind = args.Mind.Owner;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>() && !args.WasModified<GlobalAntagonistPrototype>())
            return;

        _antagonists = null;
        _antagonistsByPrototype.Clear();
    }

    private List<(GlobalAntagonistPrototype Antag, List<Type> Components)> GetAntagonists()
    {
        if (_antagonists != null)
            return _antagonists;

        _antagonists = new();
        foreach (var antag in _proto.EnumeratePrototypes<GlobalAntagonistPrototype>())
        {
            var components = new List<Type>();
            foreach (var name in antag.Components)
            {
                if (EntityManager.ComponentFactory.TryGetRegistration(name, out var reg))
                    components.Add(reg.Type);
            }

            _antagonists.Add((antag, components));
        }

        return _antagonists;
    }

    private HashSet<GlobalAntagonistPrototype> GetPrototypeAntagonists(EntityPrototype proto)
    {
        if (_antagonistsByPrototype.TryGetValue(proto.ID, out var matches))
            return matches;

        matches = new HashSet<GlobalAntagonistPrototype>();
        foreach (var (antag, _) in GetAntagonists())
        {
            if (antag.Entities.Count == 0)
                continue;

            if (antag.Entities.Contains(proto.ID) ||
                _proto.EnumerateAllParents<EntityPrototype>(proto.ID).Any(parent => antag.Entities.Contains(parent.id)))
                matches.Add(antag);
        }

        _antagonistsByPrototype[proto.ID] = matches;
        return matches;
    }

    private void OnWarpsRequest(GhostWarpsRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } ghost || !HasComp<GhostComponent>(ghost))
            return;

        var players = new List<GhostWarpPlayerDetail>();
        var antagonists = new List<GhostWarpAntagonist>();
        var globalAntagonists = GetAntagonists();

        var query = EntityQueryEnumerator<MindContainerComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var container, out var meta))
        {
            var mind = container.Mind ?? CompOrNull<LastMindComponent>(uid)?.Mind;

            if (FindAntagonist(uid, meta, mind, globalAntagonists) is { } antag)
            {
                antagonists.Add(new GhostWarpAntagonist(GetNetEntity(uid), Loc.GetString(antag.Name), Loc.GetString(antag.Description), antag.Weight));
                continue;
            }

            if (!HasComp<HumanoidProfileComponent>(uid)
                && !HasComp<GhostComponent>(uid)
                && !HasComp<BorgChassisComponent>(uid)
                && !HasComp<BorgBrainComponent>(uid))
                continue;

            var jobName = Loc.GetString("generic-unknown-title");
            string? department = null;
            if (_jobs.MindTryGetJob(mind, out var job))
            {
                jobName = Loc.GetString(job.Name);
                if (_jobs.TryGetPrimaryDepartment(job.ID, out var dept) || _jobs.TryGetDepartment(job.ID, out dept))
                    department = dept.ID;
            }

            var state = GhostWarpPlayerState.Alive;
            if (HasComp<GhostComponent>(uid))
                state = GhostWarpPlayerState.Ghost;
            else if (_mobState.IsDead(uid))
                state = GhostWarpPlayerState.Dead;
            else if (mind != null && TryComp<SSDIndicatorComponent>(uid, out var ssd) && ssd.IsSSD)
                state = GhostWarpPlayerState.Left;

            players.Add(new GhostWarpPlayerDetail(GetNetEntity(uid), jobName, department, state));
        }

        RaiseNetworkEvent(new GhostWarpDetailsEvent(players, antagonists), args.SenderSession.Channel);
    }

    private GlobalAntagonistPrototype? FindAntagonist(EntityUid uid,
        MetaDataComponent meta,
        EntityUid? mind,
        List<(GlobalAntagonistPrototype Antag, List<Type> Components)> antagonists)
    {
        var byPrototype = meta.EntityPrototype is { } proto ? GetPrototypeAntagonists(proto) : null;
        foreach (var (antag, components) in antagonists)
        {
            if (byPrototype != null && byPrototype.Contains(antag))
                return antag;

            foreach (var type in components)
            {
                if (HasComp(uid, type))
                    return antag;
            }

            if (antag.MindRoles.Count > 0 && _mindQuery.TryComp(mind, out var mindComp))
            {
                foreach (var role in mindComp.MindRoleContainer.ContainedEntities)
                {
                    if (MetaData(role).EntityPrototype is { } roleProto && antag.MindRoles.Contains(roleProto.ID))
                        return antag;
                }
            }
        }

        return null;
    }
}
