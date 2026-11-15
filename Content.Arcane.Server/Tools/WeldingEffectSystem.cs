using System.Diagnostics.CodeAnalysis;
using Content.Arcane.Shared.Tools;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Arcane.Server.Tools;

public sealed partial class WeldingEffectSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly EntProtoId AnchorPrototype = "ArcaneWeldingEffectAnchor";
    private readonly Dictionary<DoAfterId, ActiveWeld> _welds = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<WeldingToolStartedEvent>(OnWeldingStarted);
    }

    private void OnWeldingStarted(ref WeldingToolStartedEvent args)
    {
        var contact = args.Contact;
        var parent = contact.EntityId;
        if (!TryComp(parent, out TransformComponent? transform) || transform.MapID == MapId.Nullspace)
            return;

        var start = Vector2.Zero;
        var end = Vector2.Zero;
        if (args.DoorWeld && TryComp<WeldingEffectPathComponent>(parent, out var path))
        {
            contact = new EntityCoordinates(parent, Vector2.Zero);
            start = path.StartOffset;
            end = path.EndOffset;
            if (args.Reverse)
                (start, end) = (end, start);
        }

        var weld = new ActiveWeld(args.Tool, contact, transform.MapID, start, end,
            HasComp<ExperimentalWeldingEffectComponent>(args.Tool));
        if (_welds.TryAdd(args.Action, weld) && CanShow(args.Action, weld) &&
            TryGetRunningAction(args.Action, out var action))
        {
            Show(weld, action);
        }
    }

    public override void Update(float frameTime)
    {
        foreach (var (id, weld) in _welds)
        {
            if (!IsOnMap(id.Uid, weld.Map) || !IsOnMap(weld.Tool, weld.Map) ||
                !IsOnMap(weld.Contact.EntityId, weld.Map) || !TryGetRunningAction(id, out var action))
            {
                Hide(weld);
                // Dictionary supports removing the current entry during enumeration.
                _welds.Remove(id);
            }
            else if (!CanShow(id, weld))
                Hide(weld);
            else if (weld.Anchor is not { } anchor)
                Show(weld, action);
            else if (TryComp(anchor, out WeldingEffectComponent? state) && state.StartTime != action.StartTime)
            {
                // Automatic repairs reuse the action ID and start a new interval.
                state.StartTime = action.StartTime;
                Dirty(anchor, state);
            }
        }
    }

    private bool IsOnMap(EntityUid uid, MapId map)
    {
        return !TerminatingOrDeleted(uid) && !EntityManager.IsQueuedForDeletion(uid) &&
               TryComp(uid, out TransformComponent? transform) && transform.MapID == map;
    }

    private bool CanShow(DoAfterId id, ActiveWeld weld)
    {
        return !_metadata.EntityPaused(id.Uid) && !_metadata.EntityPaused(weld.Tool) &&
               !_metadata.EntityPaused(weld.Contact.EntityId);
    }

    private bool TryGetRunningAction(DoAfterId id, [NotNullWhen(true)] out DoAfter? action)
    {
        action = null;
        return TryComp<DoAfterComponent>(id.Uid, out var component) &&
               _doAfter.TryGetDoAfter(component, id.Index, out action) &&
               !action.Cancelled && !action.Completed;
    }

    private void Show(ActiveWeld weld, DoAfter action)
    {
        var anchor = SpawnAttachedTo(AnchorPrototype, weld.Contact);
        var state = Comp<WeldingEffectComponent>(anchor);
        state.StartTime = action.StartTime;
        state.Duration = _doAfter.GetArgs(action).Delay;
        state.StartOffset = weld.StartOffset;
        state.EndOffset = weld.EndOffset;
        state.Experimental = weld.Experimental;
        Dirty(anchor, state);
        weld.Anchor = anchor;
        weld.Audio = _audio.PlayPvs(state.WorkSound, anchor)?.Entity;
    }

    private void Hide(ActiveWeld weld)
    {
        weld.Audio = _audio.Stop(weld.Audio);
        if (weld.Anchor is { } anchor)
            QueueDel(anchor);
        weld.Anchor = null;
    }

    public override void Shutdown()
    {
        foreach (var weld in _welds.Values)
            Hide(weld);
        _welds.Clear();
    }

    private sealed record ActiveWeld(EntityUid Tool, EntityCoordinates Contact, MapId Map, Vector2 StartOffset,
        Vector2 EndOffset, bool Experimental)
    {
        public EntityUid? Anchor;
        public EntityUid? Audio;
    }
}
