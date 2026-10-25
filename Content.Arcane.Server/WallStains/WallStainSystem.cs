using System.Numerics;
using Content.Arcane.Shared.WallStains;
using Content.Arcane.Shared.WallStains.Components;
using Content.Server.Atmos.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Content.Shared.Doors.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using ReactiveEffectEntry = Content.Shared.Chemistry.Reaction.ReactiveReagentEffectEntry;

namespace Content.Arcane.Server.WallStains;

public sealed partial class WallStainSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> WindowTag = "Window";
    private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
    private static readonly ProtoId<TagPrototype> SoapTag = "Soap";
    private static readonly ProtoId<ReagentPrototype> Water = "Water";
    private static readonly ProtoId<ReagentPrototype> SpaceCleaner = "SpaceCleaner";
    private static readonly Vector2i[] AdjacentOffsets = [new(0, 0), new(0, 1), new(0, -1), new(1, 0), new(-1, 0)];

    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPuddleSystem _puddles = default!;
    private ReactiveEffectEntry _cleanReaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        _cleanReaction = new ReactiveEffectEntry
        {
            Methods = [ReactionMethod.Touch],
            Reagents = [SpaceCleaner, "Bleach"],
            Effects = [new CleanWallStainReaction()]
        };

        SubscribeLocalEvent<SplashOnWallEvent>(OnSplash);
        SubscribeLocalEvent<SpillableComponent, AfterInteractEvent>(OnPour);
        SubscribeLocalEvent<SpillableComponent, PourOnWallDoAfterEvent>(OnPourDoAfter);
        SubscribeLocalEvent<StainedWallComponent, InteractUsingEvent>(OnClean);
        SubscribeLocalEvent<StainedWallComponent, CleanWallStainDoAfterEvent>(OnCleanDoAfter);
        SubscribeLocalEvent<StainedWallComponent, CleanWallStainsEvent>(OnCleanEvent);
        SubscribeLocalEvent<WallStainComponent, EntityTerminatingEvent>(OnStainTerminating);
    }

    private void OnSplash(ref SplashOnWallEvent ev)
    {
        if (ev.Solution.Volume <= 0)
            return;

        var gridUid = _transform.GetGrid(ev.Coordinates);
        if (gridUid is not { } gridEntity || !TryComp<MapGridComponent>(gridEntity, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridEntity, grid, ev.Coordinates);
        foreach (var offset in AdjacentOffsets)
        {
            var anchored = _map.GetAnchoredEntities(gridEntity, grid, tile + offset);
            while (anchored.MoveNext(out var target))
            {
                if (IsValidTarget(target.Value))
                    Apply(target.Value, ev.Solution, -offset, 0.25f);
            }
        }
    }

    private void OnPour(Entity<SpillableComponent> source, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach || !IsValidTarget(target) || !CanInteract(args.User, target))
            return;
        if (!_solutions.TryGetSolution(source.Owner, source.Comp.SolutionName, out _, out var solution) || solution.Volume <= 0)
            return;
        if (solution.GetTotalPrototypeQuantity(Water) == solution.Volume)
        {
            _popup.PopupEntity(Loc.GetString("wall-stain-pour-water-blocked"), target, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("wall-stain-pour-start", ("container", source.Owner)), target, args.User);
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(5), new PourOnWallDoAfterEvent(), source.Owner, target: target, used: source.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        });
    }

    private void OnPourDoAfter(Entity<SpillableComponent> source, ref PourOnWallDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target || !IsValidTarget(target) || !CanInteract(args.User, target))
            return;
        if (!_solutions.TryGetSolution(source.Owner, source.Comp.SolutionName, out _, out var solution) || solution.Volume <= 0)
            return;

        var direction = Vector2i.Zero;
        var delta = _transform.GetWorldPosition(args.User) - _transform.GetWorldPosition(target);
        if (MathF.Abs(delta.X) > MathF.Abs(delta.Y))
            direction.X = Math.Sign(delta.X);
        else
            direction.Y = Math.Sign(delta.Y);

        var poured = solution.Clone().SplitSolution(FixedPoint2.Min(FixedPoint2.New(15), solution.Volume));
        var transferred = Apply(target, poured, direction);
        if (transferred <= 0)
        {
            _popup.PopupEntity(Loc.GetString("wall-stain-pour-full"), target, args.User);
            args.Handled = true;
            return;
        }

        if (_solutions.TryGetSolution(source.Owner, source.Comp.SolutionName, out var sourceSolution))
            _solutions.SplitSolution(sourceSolution.Value, transferred);
        _popup.PopupEntity(Loc.GetString("wall-stain-pour-success", ("container", source.Owner)), target, args.User);
        args.Handled = true;
    }

    private void OnClean(Entity<StainedWallComponent> wall, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsCleaningTool(args.Used) || !CanInteract(args.User, wall.Owner))
            return;
        if (TryComp<AbsorbentComponent>(args.Used, out var absorbent) && _solutions.TryGetSolution(args.Used, absorbent.SolutionName, out _, out var ragSolution) && ragSolution.Volume <= 0)
            return;

        _popup.PopupEntity(Loc.GetString("wall-stain-cleaning-start"), wall.Owner, args.User);
        if (_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(5), new CleanWallStainDoAfterEvent(), wall.Owner, target: wall.Owner, used: args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        }))
            args.Handled = true;
    }

    private void OnCleanDoAfter(Entity<StainedWallComponent> wall, ref CleanWallStainDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !CanInteract(args.User, wall.Owner))
            return;
        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("wall-stain-cleaning-success"), wall.Owner, args.User);
        RaiseLocalEvent(wall.Owner, new CleanWallStainsEvent());
    }

    private void OnCleanEvent(Entity<StainedWallComponent> wall, ref CleanWallStainsEvent args)
    {
        var children = Transform(wall.Owner).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!TryComp<WallStainComponent>(child, out var stain) || !_solutions.TryGetSolution(child, stain.SolutionName, out var solution))
                continue;
            if (args.TransformToWater)
            {
                var volume = solution.Value.Comp.Solution.Volume;
                _solutions.RemoveAllSolution(solution.Value);
                _solutions.TryAddReagent(solution.Value, Water, volume, out _);
            }
            else
                QueueDel(child);
        }
        if (!args.TransformToWater)
            RemCompDeferred<StainedWallComponent>(wall.Owner);
    }

    private FixedPoint2 Apply(EntityUid wall, Solution source, Vector2i direction, float fraction = 1f)
    {
        if (!IsValidTarget(wall))
            return FixedPoint2.Zero;
        EnsureComp<StainedWallComponent>(wall);
        var reactive = EnsureComp<ReactiveComponent>(wall);
        reactive.Reactions ??= [];
        if (!reactive.Reactions.Contains(_cleanReaction))
            reactive.Reactions.Add(_cleanReaction);

        EntityUid stainUid = EntityUid.Invalid;
        WallStainComponent? stain = null;
        var children = Transform(wall).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (TryComp<WallStainComponent>(child, out var current) && current.Direction == direction)
            {
                stainUid = child;
                stain = current;
                break;
            }
        }

        if (stainUid == EntityUid.Invalid)
        {
            stainUid = Spawn("ArcaneWallStain", Transform(wall).Coordinates);
            _transform.SetParent(stainUid, wall);
            _transform.SetLocalPosition(stainUid, new Vector2(direction.X * 0.48f, direction.Y * 0.48f));
            stain = Comp<WallStainComponent>(stainUid);
            stain.Direction = direction;
            stain.SplatSeed = _random.Next();
        }

        if (stain == null || !_solutions.TryGetSolution(stainUid, stain.SolutionName, out var stainSolution))
            return FixedPoint2.Zero;
        var amount = FixedPoint2.Min(source.Volume * fraction, stain.MaxStainVolume - stainSolution.Value.Comp.Solution.Volume);
        if (amount <= 0)
            return FixedPoint2.Zero;
        _solutions.TryAddSolution(stainSolution.Value, source.Clone().SplitSolution(amount));
        Update(stainUid, stain, stainSolution.Value.Comp.Solution);
        var applied = new WallStainAppliedEvent(wall, source.Clone().SplitSolution(amount));
        RaiseLocalEvent(wall, ref applied);
        return amount;
    }

    private void Update(EntityUid uid, WallStainComponent component, Solution solution)
    {
        component.Color = solution.GetColor(ProtoMan).WithAlpha(0.6f);
        component.FillLevel = component.MaxStainVolume > 0 ? (solution.Volume / component.MaxStainVolume).Float() : 0;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<WallStainComponent>();
        while (query.MoveNext(out var uid, out var stain))
        {
            if (!_solutions.TryGetSolution(uid, stain.SolutionName, out var solution))
                continue;
            var evaporating = _puddles.GetEvaporationSpeeds(solution.Value.Comp.Solution);
            foreach (var (reagent, speed) in evaporating)
                _solutions.RemoveReagent(solution.Value, reagent.Id, FixedPoint2.Min(solution.Value.Comp.Solution.GetTotalPrototypeQuantity(reagent), speed * frameTime));
            if (solution.Value.Comp.Solution.Volume <= 0)
                QueueDel(uid);
            else
                Update(uid, stain, solution.Value.Comp.Solution);
        }
    }

    private void OnStainTerminating(Entity<WallStainComponent> stain, ref EntityTerminatingEvent args)
    {
        var parent = Transform(stain.Owner).ParentUid;
        if (!parent.IsValid() || TerminatingOrDeleted(parent))
            return;
        var children = Transform(parent).ChildEnumerator;
        while (children.MoveNext(out var child))
            if (child != stain.Owner && HasComp<WallStainComponent>(child))
                return;
        RemCompDeferred<StainedWallComponent>(parent);
        if (TryComp<ReactiveComponent>(parent, out var reactive))
            reactive.Reactions?.Remove(_cleanReaction);
    }

    private bool IsValidTarget(EntityUid uid) => HasComp<WallComponent>(uid) || _tags.HasTag(uid, WindowTag) || HasComp<AirlockComponent>(uid) || _tags.HasTag(uid, AirlockTag);
    private bool CanInteract(EntityUid user, EntityUid target) => _blocker.CanInteract(user, target) && _interaction.InRangeUnobstructed(user, target);
    private bool IsCleaningTool(EntityUid uid) => HasComp<AbsorbentComponent>(uid) || _tags.HasTag(uid, SoapTag);
}
