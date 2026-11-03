using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Tag;

namespace Content.Arcane.Shared.Bed;

public sealed partial class DoubleBedSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private PlaceableSurfaceSystem _placeableSurface = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> BedsheetTag = "Bedsheet";
    // Covers both sheet positions: the left sheet sits 0.5 away from the bed's origin.
    private const float BedRange = 0.75f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoubleBedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DoubleBedComponent, StrapAttemptEvent>(OnStrapAttempt, before: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<DoubleBedComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<DoubleBedComponent, UnstrappedEvent>(OnUnstrapped, after: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<DoubleBedComponent, AfterInteractUsingEvent>(OnAfterInteractUsing, before: [typeof(PlaceableSurfaceSystem)]);
        SubscribeLocalEvent<DoubleBedComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnStartup(Entity<DoubleBedComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<StrapComponent>(ent, out var strap) && strap.BuckleOffset == Vector2.Zero)
            _buckle.SetDefaultBuckleOffset((ent, strap), ent.Comp.LeftOffset);
    }

    /// <summary>
    /// Points the default offset at the side nobody occupies; the attempt may still be refused, so nothing is
    /// stored for the entity until it is actually strapped.
    /// </summary>
    private void OnStrapAttempt(Entity<DoubleBedComponent> ent, ref StrapAttemptEvent args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        var leftTaken = false;
        foreach (var buckled in strap.BuckledEntities)
        {
            leftTaken |= SharedBuckleSystem.GetBuckleOffset(strap, buckled) == ent.Comp.LeftOffset;
        }

        _buckle.SetDefaultBuckleOffset((ent, strap), leftTaken ? ent.Comp.RightOffset : ent.Comp.LeftOffset);
    }

    private void OnStrapped(Entity<DoubleBedComponent> ent, ref StrappedEvent args)
    {
        // Pin the side this occupant got, so the default can point at the other one for the next.
        _buckle.SetBuckleOffset(args.Strap, args.Buckle.Owner, args.Strap.Comp.BuckleOffset);
    }

    private void OnUnstrapped(Entity<DoubleBedComponent> ent, ref UnstrappedEvent args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        // Whoever stays keeps their side; the next one takes the free side.
        var offset = ent.Comp.LeftOffset;
        foreach (var buckled in strap.BuckledEntities)
        {
            if (SharedBuckleSystem.GetBuckleOffset(strap, buckled) == ent.Comp.LeftOffset)
                offset = ent.Comp.RightOffset;
        }

        _buckle.SetDefaultBuckleOffset((ent, strap), offset);
    }

    private void OnInteractHand(Entity<DoubleBedComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        foreach (var sheet in _lookup.GetEntitiesInRange<DoubleBedSheetComponent>(Transform(ent).Coordinates, BedRange))
        {
            _transform.SetCoordinates(sheet, Transform(args.User).Coordinates);
            args.Handled = true;
            return;
        }
    }

    private void OnAfterInteractUsing(Entity<DoubleBedComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<PlaceableSurfaceComponent>(ent, out var surface)
            || !_tag.HasTag(args.Used, BedsheetTag)
            || HasComp<DoubleBedSheetComponent>(args.Used))
            return;

        var sheets = 0;
        foreach (var (uid, tag) in _lookup.GetEntitiesInRange<TagComponent>(Transform(ent).Coordinates, BedRange))
        {
            if (uid == args.Used || !_tag.HasTag(tag, BedsheetTag))
                continue;

            // A double sheet already covers the bed.
            if (HasComp<DoubleBedSheetComponent>(uid))
                return;

            sheets++;
        }

        var offset = sheets == 0 ? ent.Comp.RightBedsheetOffset : ent.Comp.LeftBedsheetOffset;
        _placeableSurface.SetPositionOffset(ent, offset, surface);
    }
}
