using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Tag;

namespace Content.Arcane.Shared.Bed;

public sealed partial class DoubleBedSystem : EntitySystem
{
    [Dependency] private PlaceableSurfaceSystem _placeableSurface = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> BedsheetTag = "Bedsheet";
    private const float BedRange = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoubleBedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DoubleBedComponent, StrapAttemptEvent>(OnStrapAttempt, before: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<DoubleBedComponent, UnstrappedEvent>(OnUnstrapped, after: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<DoubleBedComponent, AfterInteractUsingEvent>(OnAfterInteractUsing, before: [typeof(PlaceableSurfaceSystem)]);
        SubscribeLocalEvent<DoubleBedComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnStartup(Entity<DoubleBedComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<StrapComponent>(ent, out var strap) && strap.BuckleOffset == Vector2.Zero)
            _buckle.SetDefaultBuckleOffset((ent, strap), ent.Comp.LeftOffset);
    }

    private void OnStrapAttempt(Entity<DoubleBedComponent> ent, ref StrapAttemptEvent args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        var offset = strap.BuckledEntities.Count == 0 ? ent.Comp.LeftOffset : ent.Comp.RightOffset;
        _buckle.SetBuckleOffset((ent, strap), args.Buckle.Owner, offset);
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

        var query = EntityQueryEnumerator<DoubleBedSheetComponent, TransformComponent>();
        while (query.MoveNext(out var sheet, out _, out var xform))
        {
            if (!IsOnBed(ent, xform))
                continue;

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
        var query = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tag, out var xform))
        {
            if (uid == args.Used || !IsOnBed(ent, xform) || !_tag.HasTag(tag, BedsheetTag))
                continue;

            // A double sheet already covers the bed.
            if (HasComp<DoubleBedSheetComponent>(uid))
                return;

            sheets++;
        }

        var offset = sheets == 0 ? ent.Comp.RightBedsheetOffset : ent.Comp.LeftBedsheetOffset;
        _placeableSurface.SetPositionOffset(ent, offset, surface);
    }

    private bool IsOnBed(EntityUid bed, TransformComponent xform)
    {
        return xform.Coordinates.TryDistance(EntityManager, _transform, Transform(bed).Coordinates, out var distance)
               && distance < BedRange;
    }
}
