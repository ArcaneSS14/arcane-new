using System.Linq;
using System.Numerics;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Arcane.Server.Voodoo;

/// <summary>Links rare dolls to body entities, not names, minds, or player sessions.</summary>
public sealed partial class VoodooSystem : EntitySystem
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private bool _propagating;
    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoodooComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VoodooComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VoodooComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<VoodooComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<VoodooComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<VoodooComponent, ThrowEvent>(OnThrow);
        SubscribeLocalEvent<VoodooTargetComponent, ComponentShutdown>(OnTargetShutdown);
    }

    /// <summary>Only a currently authorized administrator can assign or clear an admin doll.</summary>
    public bool TrySetAdminTarget(ICommonSession actor, EntityUid doll, EntityUid? target)
    {
        if (!_admins.HasAdminFlag(actor, AdminFlags.Fun) ||
            TerminatingOrDeleted(doll) || !TryComp<VoodooComponent>(doll, out var comp) || comp.BloodBinding ||
            (target is { } body && !CanBind(body)))
            return false;

        SetTarget((doll, comp), target);
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{actor.Name} assigned voodoo doll {ToPrettyString(doll)} to {(target is { } uid ? ToPrettyString(uid) : "none")}.");
        return true;
    }

    private void OnStartup(Entity<VoodooComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Target is { } target && CanBind(target))
            EnsureComp<VoodooTargetComponent>(target).Dolls.Add(ent.Owner);
        else
            ent.Comp.Target = null;
    }

    private void OnShutdown(Entity<VoodooComponent> ent, ref ComponentShutdown args)
    {
        SetTarget(ent, null);
    }

    private void OnTargetShutdown(Entity<VoodooTargetComponent> ent, ref ComponentShutdown args)
    {
        foreach (var doll in ent.Comp.Dolls.ToArray())
        {
            if (TryComp<VoodooComponent>(doll, out var link) && link.Target == ent.Owner)
                link.Target = null;
        }
    }

    private void SetTarget(Entity<VoodooComponent> ent, EntityUid? target)
    {
        if (ent.Comp.Target == target)
            return;

        if (ent.Comp.Target is { } previous && TryComp<VoodooTargetComponent>(previous, out var old))
        {
            old.Dolls.Remove(ent.Owner);
            if (old.Dolls.Count == 0)
                RemComp<VoodooTargetComponent>(previous);
        }

        ent.Comp.Target = target;
        if (target is { } body)
            EnsureComp<VoodooTargetComponent>(body).Dolls.Add(ent.Owner);
    }

    private void OnDamage(Entity<VoodooComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || ent.Comp.Destroyed)
            return;
        ApplyDamage(ent, ent.Comp.Damage);
    }

    private void OnDestroyed(Entity<VoodooComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Destroyed)
            return;
        ent.Comp.Destroyed = true;
        ApplyDamage(ent, ent.Comp.DestructionDamage);
    }

    private void ApplyDamage(Entity<VoodooComponent> ent, float amount)
    {
        if (_propagating || ent.Comp.Target is not { } target || TerminatingOrDeleted(target))
            return;
        _propagating = true;
        try
        {
            var brute = _prototypes.Index(Brute);
            _damage.TryChangeDamage(target, new DamageSpecifier(brute, amount), origin: ent.Owner);
        }
        finally
        {
            _propagating = false;
        }
    }

    private void OnThrow(Entity<VoodooComponent> ent, ref ThrowEvent args)
    {
        if (ent.Comp.Target is not { } target || TerminatingOrDeleted(target))
            return;
        var sourceTransform = Transform(ent);
        var targetTransform = Transform(target);
        if (sourceTransform.MapUid is null || sourceTransform.MapUid != targetTransform.MapUid)
            return;
        var direction = targetTransform.MapPosition.Position - sourceTransform.MapPosition.Position;
        if (direction == Vector2.Zero)
            direction = _random.NextVector2(1f);
        _throwing.TryThrow(target, direction, _random.NextFloat(3f, 5f), args.User, predicted: false);
    }

    private bool TryFindUniqueBody(string wanted, out EntityUid body, out BloodstreamComponent? bloodstream)
    {
        body = EntityUid.Invalid;
        bloodstream = null;
        var found = false;
        var bodies = AllEntityQuery<DnaComponent, BodyComponent>();
        while (bodies.MoveNext(out var uid, out var dna, out _))
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid) || dna.DNA != wanted)
                continue;
            if (found)
                return false;
            found = true;
            body = uid;
            TryComp(uid, out bloodstream);
        }
        return found && bloodstream != null;
    }

    private bool CanBind(EntityUid body) => !TerminatingOrDeleted(body) &&
        !EntityManager.IsQueuedForDeletion(body) && HasComp<BodyComponent>(body);

    private void OnInteractUsing(Entity<VoodooComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !ent.Comp.BloodBinding || ent.Comp.Destroyed || ent.Comp.Binding || TerminatingOrDeleted(args.Used))
            return;

        ent.Comp.Binding = true;
        try
        {
            BindFromSample(ent, ref args);
        }
        finally
        {
            ent.Comp.Binding = false;
        }
    }

    private void BindFromSample(Entity<VoodooComponent> ent, ref InteractUsingEvent args)
    {
        // Examine all accessible solutions: multiple distinct DNA values are ambiguous.
        var candidates = new List<(Entity<SolutionComponent> Solution, ReagentQuantity Reagent)>();
        var dnas = new HashSet<string>();
        var hasSolution = false;
        var missingBloodDna = false;
        foreach (var (_, solutionEnt) in _solutions.EnumerateSolutions(args.Used))
        {
            hasSolution = true;
            foreach (var quantity in solutionEnt.Comp.Solution.Contents)
            {
                var rawDna = quantity.Reagent.EnsureReagentData().OfType<DnaData>().Select(data => data.DNA).ToArray();
                var values = rawDna.Where(dna => !string.IsNullOrWhiteSpace(dna)).Distinct().ToArray();
                if (ent.Comp.BloodReagents.Contains(quantity.Reagent.Prototype) &&
                    (rawDna.Length == 0 || rawDna.Any(string.IsNullOrWhiteSpace) || values.Length != 1))
                    missingBloodDna = true;
                foreach (var value in values)
                    dnas.Add(value);
                if (values.Length == 1 && quantity.Quantity >= ent.Comp.BloodCost)
                    candidates.Add((solutionEnt, quantity));
            }
        }

        if (!hasSolution)
            return;
        args.Handled = true;
        if (ent.Comp.BloodCost <= FixedPoint2.Zero || missingBloodDna || dnas.Count != 1)
        {
            _popup.PopupEntity(Loc.GetString("voodoo-blood-invalid"), ent, args.User);
            return;
        }

        var wanted = dnas.Single();
        if (!TryFindUniqueBody(wanted, out var target, out var bloodstream) || bloodstream == null)
        {
            _popup.PopupEntity(Loc.GetString("voodoo-blood-invalid"), ent, args.User);
            return;
        }

        // Real blood is selected by this body's existing blood reference, not by arbitrary DNA data.
        var matches = candidates.Where(candidate => ent.Comp.BloodReagents.Contains(candidate.Reagent.Reagent.Prototype) &&
            bloodstream.BloodReferenceSolution.Contents.Any(reference =>
            reference.Reagent.Prototype == candidate.Reagent.Reagent.Prototype)).ToArray();
        if (matches.Length != 1)
        {
            _popup.PopupEntity(Loc.GetString("voodoo-blood-invalid"), ent, args.User);
            return;
        }

        var sample = matches[0];
        // Avoid proportional rounding across duplicated identical ReagentIds.
        if (sample.Solution.Comp.Solution.Contents.Count(entry => entry.Reagent == sample.Reagent.Reagent) != 1)
        {
            _popup.PopupEntity(Loc.GetString("voodoo-blood-invalid"), ent, args.User);
            return;
        }
        var previous = ent.Comp.Target;
        SetTarget(ent, target);
        // Recheck after changing the link because component lifecycle handlers can run synchronously.
        if (TerminatingOrDeleted(ent))
            return;
        if (TerminatingOrDeleted(args.Used))
        {
            SetTarget(ent, previous is { } old && !TerminatingOrDeleted(old) ? old : null);
            return;
        }
        var currentSolutions = _solutions.EnumerateSolutions(args.Used).ToArray();
        if (TerminatingOrDeleted(ent) || EntityManager.IsQueuedForDeletion(ent) ||
            TerminatingOrDeleted(target) || EntityManager.IsQueuedForDeletion(target) || TerminatingOrDeleted(args.Used) ||
            TerminatingOrDeleted(sample.Solution) || ent.Comp.Target != target ||
            !TryFindUniqueBody(wanted, out var checkedBody, out var checkedBlood) || checkedBody != target ||
            checkedBlood == null || !checkedBlood.BloodReferenceSolution.Contents.Any(reference =>
                reference.Reagent.Prototype == sample.Reagent.Reagent.Prototype) ||
            !currentSolutions.Any(entry => entry.Solution.Owner == sample.Solution.Owner &&
                ReferenceEquals(entry.Solution.Comp, sample.Solution.Comp)) ||
            sample.Solution.Comp.Solution.Contents.Count(entry => entry.Reagent == sample.Reagent.Reagent) != 1 ||
            !sample.Solution.Comp.Solution.Contents.Any(entry =>
                entry.Reagent == sample.Reagent.Reagent && entry.Quantity >= ent.Comp.BloodCost) ||
            currentSolutions.SelectMany(entry => entry.Solution.Comp.Solution.Contents)
                .Any(entry => ent.Comp.BloodReagents.Contains(entry.Reagent.Prototype) &&
                    (!entry.Reagent.EnsureReagentData().OfType<DnaData>().Any() ||
                     entry.Reagent.EnsureReagentData().OfType<DnaData>().Any(dna => string.IsNullOrWhiteSpace(dna.DNA)))) ||
            currentSolutions.SelectMany(entry => entry.Solution.Comp.Solution.Contents)
                .SelectMany(entry => entry.Reagent.EnsureReagentData().OfType<DnaData>())
                .Any(dna => !string.IsNullOrWhiteSpace(dna.DNA) && dna.DNA != wanted))
        {
            if (!TerminatingOrDeleted(ent))
                SetTarget(ent, previous is { } old && !TerminatingOrDeleted(old) ? old : null);
            return;
        }
        var exact = new ReagentQuantity(sample.Reagent.Reagent, ent.Comp.BloodCost);
        if (_solutions.RemoveReagent(sample.Solution, exact, ignoreReagentData: false) != ent.Comp.BloodCost)
        {
            if (!TerminatingOrDeleted(ent))
            {
                SetTarget(ent, previous is { } old && !TerminatingOrDeleted(old) ? old : null);
                _popup.PopupEntity(Loc.GetString("voodoo-blood-invalid"), ent, args.User);
            }
            return;
        }
        if (!TerminatingOrDeleted(ent))
            _popup.PopupEntity(Loc.GetString("voodoo-blood-bound"), ent, args.User);
    }
}
