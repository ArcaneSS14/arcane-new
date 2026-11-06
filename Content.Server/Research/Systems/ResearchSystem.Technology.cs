using Content.Shared.Database;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
// Arcane-Start
using System.Linq;
using Content.Shared.Research;
// Arcane-End

using Content.Arcane.Common.Research; // Arcane

// Arcane-Start
using Content.Server.Construction.Completions;
using Robust.Shared.Containers;
// Arcane-End

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("research.tech-web"); // Arcane

    /// <summary>
    /// Syncs the primary entity's database to that of the secondary entity's database.
    /// </summary>
    private void Sync(EntityUid primaryUid, EntityUid otherUid, TechnologyDatabaseComponent? primaryDb = null, TechnologyDatabaseComponent? otherDb = null) // Arcane-Edit: Was public
    {
        if (!Resolve(primaryUid, ref primaryDb) || !Resolve(otherUid, ref otherDb))
            return;

        primaryDb.MainDiscipline = otherDb.MainDiscipline;
        // Arcane-Edit-Start
        primaryDb.CurrentTechnologyCards = new List<string>(otherDb.CurrentTechnologyCards);
        primaryDb.SupportedDisciplines = new List<ProtoId<TechDisciplinePrototype>>(otherDb.SupportedDisciplines);
        // Arcane-Edit-End
        // Arcane-Start
        primaryDb.VisibleTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.VisibleTechnologies);
        primaryDb.AvailableTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.AvailableTechnologies);
        primaryDb.ResearchedTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.ResearchedTechnologies);
        primaryDb.AvailableExperiments = new List<string>(otherDb.AvailableExperiments);
        primaryDb.UnlockedExperiments = new List<string>(otherDb.UnlockedExperiments);
        primaryDb.ActiveExperiments = new List<string>(otherDb.ActiveExperiments);
        primaryDb.CompletedExperiments = new List<string>(otherDb.CompletedExperiments);
        primaryDb.SkippedExperiments = new List<string>(otherDb.SkippedExperiments);
        primaryDb.ExperimentProgress = otherDb.ExperimentProgress
            .Select(CloneExperimentProgress)
            .ToList();
        // Arcane-End
        primaryDb.UnlockedRecipes = new List<ProtoId<LatheRecipePrototype>>(otherDb.UnlockedRecipes); // Arcane-Edit
        // Arcane-Start
        primaryDb.IndependentRecipes = new List<ProtoId<LatheRecipePrototype>>(otherDb.IndependentRecipes);
        primaryDb.RevealedTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.RevealedTechnologies);
        primaryDb.DiscoveryProgress = new List<TechnologyDiscoveryProgress>(otherDb.DiscoveryProgress);
        primaryDb.UnlockedInfrastructure = new List<string>(otherDb.UnlockedInfrastructure);
        // Arcane-End

        Dirty(primaryUid, primaryDb);

        var ev = new TechnologyDatabaseSynchronizedEvent();
        RaiseLocalEvent(primaryUid, ref ev);
    }

    /// <summary>
    ///     If there's a research client component attached to the owner entity,
    ///     and the research client is connected to a research server, this method
    ///     syncs against the research server, and the server against the local database.
    /// </summary>
    /// <returns>Whether it could sync or not</returns>
    private void SyncClientWithServer(EntityUid uid, TechnologyDatabaseComponent? databaseComponent = null, ResearchClientComponent? clientComponent = null) // Arcane-Edit: Was public
    {
        if (!Resolve(uid, ref databaseComponent, ref clientComponent, false))
            return;

        if (clientComponent.Server is not { } serverUid) // Arcane-Edit
            return;

        // Arcane-Start
        if (!TryComp(serverUid, out ResearchServerComponent? serverComponent))
            return;

        var authorityUid = GetNetworkAuthority(serverUid, serverComponent);
        if (authorityUid != serverUid)
        {
            UnregisterClient(uid, serverUid, clientComponent, serverComponent, dirtyServer: false);
            RegisterClient(uid, authorityUid, clientComponent, dirtyServer: false);
            return;
        }

        if (!TryComp<TechnologyDatabaseComponent>(serverUid, out var serverDatabase))
            return;
        // Arcane-End

        Sync(uid, serverUid, databaseComponent, serverDatabase); // Arcane-Edit
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    private bool UnlockTechnology(EntityUid client, // Arcane-Edit: Was public
        string prototypeid,
        EntityUid user,
        ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? clientDatabase = null)
    {
        if (!ProtoMan.TryIndex<TechnologyPrototype>(prototypeid, out var prototype))
            return false;

        return UnlockTechnology(client, prototype, user, component, clientDatabase);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    private bool UnlockTechnology(EntityUid client, // Arcane-Edit: Was public
        TechnologyPrototype prototype,
        EntityUid user,
        ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? clientDatabase = null)
    {
        if (!Resolve(client, ref component, ref clientDatabase, false))
            return false;

        if (!TryGetClientServer(client, out var serverEnt, out _, component))
            return false;

        if (!CanServerUnlockTechnology(client, prototype, out var finalCosts, clientDatabase, component)) // Arcane-Edit
            return false;

        AddTechnology(serverEnt.Value, prototype);
        //TrySetMainDiscipline(prototype, serverEnt.Value); // Goobstation commented
        TryConsumePoints(serverEnt.Value, finalCosts); // Arcane-Edit
        UpdateTechnologyCards(serverEnt.Value);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} unlocked {prototype.ID} (discipline: {prototype.Discipline}, tier: {prototype.Tier}) at {ToPrettyString(client)}, for server {ToPrettyString(serverEnt.Value)}.");
        LogNetworkEvent(serverEnt.Value, "technology", Loc.GetString("research-netlog-technology-unlocked", ("technology", Loc.GetString(prototype.Name)), ("user", GetResearchLogUserName(user))), user); // Arcane
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    [PublicAPI]
    public void AddTechnology(EntityUid uid, string technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return;
        AddTechnology(uid, prototype, component);
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    public void AddTechnology(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        //todo this needs to support some other stuff, too
        foreach (var generic in technology.GenericUnlocks)
        {
            if (generic.PurchaseEvent != null)
                RaiseLocalEvent(generic.PurchaseEvent);
        }

        // Arcane-Edit-Start
        var addedRecipes = technology.RecipeUnlocks
            .Select(recipe => recipe.Id)
            .Where(recipe => !component.UnlockedRecipes.Contains(recipe))
            .ToList();

        if (!component.ResearchedTechnologies.Contains(technology.ID))
            component.ResearchedTechnologies.Add(technology.ID);

        foreach (var experiment in technology.UnlockedExperiments)
        {
            if (component.CompletedExperiments.Contains(experiment) || component.SkippedExperiments.Contains(experiment))
                continue;

            if (!component.AvailableExperiments.Contains(experiment))
                component.AvailableExperiments.Add(experiment);
        }
        // Arcane-Edit-End

        // Arcane-Start
        foreach (var infrastructure in technology.InfrastructureUnlocks)
        {
            if (!component.UnlockedInfrastructure.Contains(infrastructure))
                component.UnlockedInfrastructure.Add(infrastructure);
        }

        RecalculateTechnologyState(uid, component);
        // Arcane-End
        Dirty(uid, component);

        var ev = new TechnologyDatabaseModifiedEvent(addedRecipes);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    private bool CanServerUnlockTechnology(EntityUid uid, TechnologyPrototype technology, out List<ResearchPointAmount> finalCosts, TechnologyDatabaseComponent? database = null, ResearchClientComponent? client = null) // Arcane-Edit: Was public
    {
        finalCosts = new List<ResearchPointAmount>(); // Arcane

        if (!Resolve(uid, ref client, ref database, false))
            return false;

        if (!TryGetClientServer(uid, out var serverUid, out var serverComp, client)) // Arcane-Edit
            return false;

        // Arcane-Start
        if (!TryComp<TechnologyDatabaseComponent>(serverUid, out var serverDatabase))
            return false;

        finalCosts = GetTechnologyFinalPointCosts(serverDatabase, technology);

        if (!CanUnlockTechnology(serverDatabase, technology))
            return false;

        return HasSufficientPoints(serverUid.Value, finalCosts, serverComp);
        // Arcane-End
    }

    // Arcane-Start
    public override bool CanUnlockTechnology(TechnologyDatabaseComponent component, TechnologyPrototype tech)
    {
        if (!component.VisibleTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: not visible.");
            return false;
        }

        if (component.ResearchedTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: already researched.");
            return false;
        }

        if (!tech.AllRequiredTechnologies.All(prereq => component.ResearchedTechnologies.Contains(prereq)))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: prerequisites not completed.");
            return false;
        }

        if (!HasRequiredExperiments(component, tech))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: required experiments not completed.");
            return false;
        }

        if (!component.AvailableTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: tech is not available.");
            return false;
        }

        return true;
    }
    // Arcane-End

    private void OnDatabaseRegistrationChanged(EntityUid uid, TechnologyDatabaseComponent component, ref ResearchRegistrationChangedEvent args)
    {
        if (args.Server != null)
            return;
        component.MainDiscipline = null;
        component.CurrentTechnologyCards = new List<string>();
        component.SupportedDisciplines = new List<ProtoId<TechDisciplinePrototype>>();
        // Arcane-Start
        component.VisibleTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.AvailableTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.ResearchedTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.AvailableExperiments = new List<string>();
        component.UnlockedExperiments = new List<string>();
        component.ActiveExperiments = new List<string>();
        component.CompletedExperiments = new List<string>();
        component.SkippedExperiments = new List<string>();
        component.ExperimentProgress = new List<ResearchExperimentProgress>();
        // Arcane-End
        component.UnlockedRecipes = new List<ProtoId<LatheRecipePrototype>>();
        // Arcane-Start
        component.IndependentRecipes = new List<ProtoId<LatheRecipePrototype>>();
        component.RevealedTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.DiscoveryProgress = new List<TechnologyDiscoveryProgress>();
        component.UnlockedInfrastructure = new List<string>();
        // Arcane-End
        Dirty(uid, component);
    }

    // Arcane-Start
    private static ResearchExperimentProgress CloneExperimentProgress(ResearchExperimentProgress source)
    {
        return new ResearchExperimentProgress
        {
            ExperimentId = source.ExperimentId,
            Progress = source.Progress,
            Target = source.Target,
            UniqueProgressKeys = new HashSet<string>(source.UniqueProgressKeys),
            ScannedEntities = new HashSet<NetEntity>(source.ScannedEntities),
            CompletedAt = source.CompletedAt,
        };
    }
    // Arcane-End

    // Arcane-Start
    private void InitializeDiscovery()
    {
        SubscribeLocalEvent<ResearchClientComponent, EntInsertedIntoContainerMessage>(OnDiscoveryMachineInsertion);
        SubscribeLocalEvent<MetaDataComponent, ConstructionBeforeDeleteEvent>(OnDiscoveryDeconstruct);
    }

    private void OnDiscoveryMachineInsertion(EntityUid uid, ResearchClientComponent component, ref EntInsertedIntoContainerMessage args)
    {
        if (component.Server is not { } server)
            return;

        NotifyDiscoveryEvent(server,
            new DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.MachineInsertion,
            Subject = args.Entity,
            Machine = uid,
        });
    }

    private void OnDiscoveryDeconstruct(EntityUid uid, MetaDataComponent component, ref ConstructionBeforeDeleteEvent args)
    {
        if (!HasComp<ResearchAnalyzableComponent>(uid))
            return;

        if (!TryGetResearchServerForEntity(uid, out var serverUid) || serverUid is not { } server)
            return;

        NotifyDiscoveryEvent(server,
            new DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.DeconstructEntity,
            Subject = uid,
        });
    }

    public bool NotifyDiscoveryEvent(EntityUid serverUid, DiscoveryEventData data, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return false;

        var revealedAny = false;
        foreach (var technology in ProtoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (!technology.Hidden || !database.SupportedDisciplines.Contains(technology.Discipline))
                continue;

            if (database.RevealedTechnologies.Contains(technology.ID))
                continue;

            var revealedByRequirements = false;
            if (technology.RevealRequirements.Count > 0)
            {
                ProcessTechnologyDiscoveryEvent(database, technology, data);
                revealedByRequirements = AreRevealRequirementsSatisfied(database, technology);
            }

            var revealedByUnlockLists = IsUnlockListRevealSatisfied(technology, data);

            if (!revealedByRequirements && !revealedByUnlockLists)
                continue;

            database.RevealedTechnologies.Add(technology.ID);
            revealedAny = true;
            _sawmill.Info($"Revealed hidden technology {technology.ID} via discovery event {data.Type}.");
            LogNetworkEvent(serverUid, "discovery", Loc.GetString("research-netlog-discovery-hidden-tech", ("technology", Loc.GetString(technology.Name)), ("user", GetResearchLogUserName(data.User))), data.User);
        }

        if (!revealedAny)
            return false;

        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);

        if (!TryComp<ResearchServerComponent>(serverUid, out var serverComp) ||
            serverComp.Clients.Count <= 0)
            return true;

        foreach (var console in serverComp.Clients)
        {
            SyncClientWithServer(console);
            UpdateConsoleInterface(console);
        }

        return true;
    }

    public bool TriggerDiscovery(EntityUid serverUid, string triggerId, TechnologyDatabaseComponent? database = null)
    {
        return NotifyDiscoveryEvent(serverUid,
            new DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.ServerTrigger,
            TriggerId = triggerId,
        },
        database);
    }

    private void ProcessTechnologyDiscoveryEvent(TechnologyDatabaseComponent database, TechnologyPrototype technology, DiscoveryEventData data)
    {
        foreach (var requirement in technology.RevealRequirements)
        {
            if (IsRevealRequirementSatisfied(database, technology.ID, requirement))
                continue;

            if (!DoesDiscoveryEventMatch(requirement, data))
                continue;

            IncrementDiscoveryProgress(database, technology.ID, requirement, 1);
        }
    }

    private bool IsUnlockListRevealSatisfied(TechnologyPrototype technology, DiscoveryEventData data)
    {
        if (data.Subject == null)
            return false;

        var subjectPrototype = GetPrototypeId(data.Subject);
        if (subjectPrototype == null)
            return false;

        return data.Type switch
        {
            ResearchDiscoveryEventType.MachineInsertion => technology.ItemUnlocks.Contains(subjectPrototype),
            ResearchDiscoveryEventType.DeconstructEntity => technology.DeconstructionUnlocks.Contains(subjectPrototype),
            _ => false,
        };
    }

    private static bool AreRevealRequirementsSatisfied(TechnologyDatabaseComponent database, TechnologyPrototype technology)
    {
        return technology.RevealRequirements.Count == 0 || technology.RevealRequirements.All(req => IsRevealRequirementSatisfied(database, technology.ID, req));
    }

    private static bool IsRevealRequirementSatisfied(TechnologyDatabaseComponent database,
        string technologyId,
        TechnologyRevealRequirement requirement)
    {
        return TryGetPassiveRevealState(database, requirement, out var satisfied)
            ? satisfied
            : GetDiscoveryProgress(database, technologyId, requirement.Id) >= Math.Max(1, requirement.Target);
    }

    private bool DoesDiscoveryEventMatch(TechnologyRevealRequirement requirement, DiscoveryEventData data)
    {
        switch (requirement)
        {
            case ScanEntityRevealRequirement scan when requirement.Kind == TechnologyRevealRequirementKind.ScanEntity:
                return data.Type == ResearchDiscoveryEventType.ScanEntity && MatchesScanRequirement(scan, data.Subject);

            case MachineInsertionRevealRequirement insertion:
                return data.Type == ResearchDiscoveryEventType.MachineInsertion && MatchesScanRequirement(insertion, data.Subject)
                    && (insertion.RequiredMachinePrototype == null || GetPrototypeId(data.Machine) == insertion.RequiredMachinePrototype);

            case DeconstructEntityRevealRequirement deconstruct:
                return data.Type == ResearchDiscoveryEventType.DeconstructEntity && MatchesDeconstructRequirement(deconstruct, data.Subject);

            case ServerTriggerRevealRequirement trigger:
                return data.Type == ResearchDiscoveryEventType.ServerTrigger && trigger.TriggerId == data.TriggerId;

            default:
                return false;
        }
    }

    private bool MatchesScanRequirement(ScanEntityRevealRequirement requirement, EntityUid? subject)
    {
        if (subject == null)
            return false;

        if (requirement.RequiredEntityPrototype != null && GetPrototypeId(subject) != requirement.RequiredEntityPrototype)
            return false;

        foreach (var tag in requirement.RequiredTags)
        {
            if (!_tag.HasTag(subject.Value, tag))
                return false;
        }

        return true;
    }

    private bool MatchesDeconstructRequirement(DeconstructEntityRevealRequirement requirement, EntityUid? subject)
    {
        if (subject == null)
            return false;

        if (requirement.RequiredEntityPrototype != null && GetPrototypeId(subject) != requirement.RequiredEntityPrototype)
            return false;

        foreach (var tag in requirement.RequiredTags)
        {
            if (!_tag.HasTag(subject.Value, tag))
                return false;
        }

        return true;
    }

    private void IncrementDiscoveryProgress(TechnologyDatabaseComponent database, string technologyId, TechnologyRevealRequirement requirement, int amount)
    {
        var target = Math.Max(1, requirement.Target);

        for (var i = 0; i < database.DiscoveryProgress.Count; i++)
        {
            var entry = database.DiscoveryProgress[i];
            if (entry.TechnologyId != technologyId || entry.RequirementId != requirement.Id)
                continue;

            if (entry.Progress >= target)
                return;

            entry.Progress = Math.Min(target, entry.Progress + amount);
            if (entry.Progress >= target)
                entry.CompletedAt = _timing.CurTime;

            database.DiscoveryProgress[i] = entry;
            return;
        }

        database.DiscoveryProgress.Add(new TechnologyDiscoveryProgress
        {
            TechnologyId = technologyId,
            RequirementId = requirement.Id,
            Progress = Math.Min(target, amount),
            Target = target,
            CompletedAt = amount >= target ? _timing.CurTime : null
        });
    }

    private static int GetDiscoveryProgress(TechnologyDatabaseComponent database, string technologyId, string requirementId)
    {
        foreach (var entry in database.DiscoveryProgress)
        {
            if (entry.TechnologyId == technologyId && entry.RequirementId == requirementId)
                return entry.Progress;
        }

        return 0;
    }

    public List<string> GetHiddenTechnologiesForRequiredItem(EntityUid serverUid, EntityUid subject, TechnologyDatabaseComponent? database = null)
    {
        var result = new List<string>();

        if (!Resolve(serverUid, ref database))
            return result;

        var subjectPrototype = GetPrototypeId(subject);
        if (subjectPrototype == null)
            return result;

        foreach (var technology in ProtoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (!technology.Hidden || !database.SupportedDisciplines.Contains(technology.Discipline))
                continue;

            if (database.RevealedTechnologies.Contains(technology.ID))
                continue;

            if (!technology.RequiredItemsToUnlock.Contains(subjectPrototype))
                continue;

            result.Add(technology.ID);
        }

        return result;
    }

    private string? GetPrototypeId(EntityUid? uid)
    {
        if (uid == null || !TryComp(uid.Value, out MetaDataComponent? meta))
            return null;

        return meta.EntityPrototype?.ID;
    }

    private bool TryGetResearchServerForEntity(EntityUid uid, out EntityUid? serverUid)
    {
        serverUid = null;

        if (TryComp<ResearchClientComponent>(uid, out var directClient) && directClient.Server is { } directServer)
        {
            serverUid = directServer;
            return true;
        }

        var transform = Transform(uid);
        if (TryComp<ResearchClientComponent>(transform.ParentUid, out var ownerClient) && ownerClient.Server is { } ownerServer)
        {
            serverUid = ownerServer;
            return true;
        }

        if (transform.GridUid is not { } grid)
            return false;

        var query = EntityQueryEnumerator<ResearchServerComponent, TransformComponent>();
        while (query.MoveNext(out var candidateUid, out _, out _))
        {
            if (Transform(candidateUid).GridUid != grid)
                continue;

            serverUid = candidateUid;
            return true;
        }

        return false;
    }

    public sealed record DiscoveryEventData
    {
        public ResearchDiscoveryEventType Type;
        public EntityUid? Subject;
        public EntityUid? Machine;
        public EntityUid? User;
        public string? TriggerId;
    }
    // Arcane-End

    // Arcane-Start
    public void RevealTechnology(EntityUid serverUid, string technologyId, EntityUid? user = null, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(technologyId, out var technology))
            return;

        if (database.RevealedTechnologies.Contains(technology.ID))
            return;

        database.RevealedTechnologies.Add(technology.ID);
        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);

        LogNetworkEvent(serverUid, "discovery", Loc.GetString("research-netlog-discovery-hidden-tech", ("technology", Loc.GetString(technology.Name)), ("user", GetResearchLogUserName(user))), user);
    }

    public void UnlockTechnology(EntityUid serverUid,
        string technologyId,
        EntityUid? user,
        TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(technologyId, out var technology))
            return;

        if (database.ResearchedTechnologies.Contains(technology.ID))
            return;

        AddTechnology(serverUid, technology, database);
        LogNetworkEvent(serverUid, "technology", Loc.GetString("research-netlog-technology-unlocked", ("technology", Loc.GetString(technology.Name)), ("user", GetResearchLogUserName(user))), user);
    }
    // Arcane-End
}
