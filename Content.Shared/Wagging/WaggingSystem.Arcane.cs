using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wagging;

public sealed partial class WaggingSystem
{
    [Dependency] private MarkingManager _markings = default!;
    [Dependency] private INetManager _network = default!;
    private readonly HashSet<EntityUid> _pendingRefresh = [];

    public override void Initialize()
    {
        base.Initialize();
        ProtoMan.PrototypesReloaded += OnMarkingPrototypesReloaded;
    }

    public override void Shutdown()
    {
        ProtoMan.PrototypesReloaded -= OnMarkingPrototypesReloaded;
        _pendingRefresh.Clear();
        base.Shutdown();
    }

    private void OnMarkingPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!_network.IsServer || !args.WasModified<MarkingPrototype>())
            return;
        var query = EntityQueryEnumerator<WaggingComponent>();
        while (query.MoveNext(out var uid, out _))
            _pendingRefresh.Add(uid);
    }

    [SubscribeLocalEvent]
    private void OnMarkingsChanged(Entity<WaggingComponent> ent, ref VisualBodyMarkingsChangedEvent args)
    {
        if (_network.IsServer)
            _pendingRefresh.Add(ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<WaggingComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (_network.IsServer)
            _pendingRefresh.Add(ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<WaggingComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (_network.IsServer)
            _pendingRefresh.Add(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var uid in _pendingRefresh)
        {
            if (!TerminatingOrDeleted(uid) && TryComp<WaggingComponent>(uid, out var component))
                RefreshWaggingAction((uid, component));
        }
        _pendingRefresh.Clear();
    }

    private void RefreshWaggingAction(Entity<WaggingComponent> ent)
    {
        if (!_network.IsServer || LifeStage(ent) < EntityLifeStage.MapInitialized)
            return;
        var supported = false;
        var wagging = false;
        if (_visualBody.TryGatherMarkingsData(ent.Owner, [ent.Comp.Layer], out _, out _, out var applied)
            && applied.TryGetValue(ent.Comp.Organ, out var layers)
            && layers.TryGetValue(ent.Comp.Layer, out var tail))
        {
            foreach (var marking in tail)
            {
                if (TryGetStill(marking.MarkingId, ent.Comp.Suffix, out _))
                    supported = wagging = true;
                else if (TryGetAnimated(marking.MarkingId, ent.Comp.Suffix, out _))
                    supported = true;
            }
        }
        if (ent.Comp.Wagging != wagging)
        {
            ent.Comp.Wagging = wagging;
            DirtyField(ent.AsNullable(), nameof(WaggingComponent.Wagging));
        }
        if (!supported)
        {
            RemoveWaggingAction(ent);
            return;
        }
        if (!Exists(ent.Comp.ActionEntity))
        {
            _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
            DirtyField(ent.AsNullable(), nameof(WaggingComponent.ActionEntity));
        }
    }

    private void RemoveWaggingAction(Entity<WaggingComponent> ent, bool dirty = true)
    {
        if (ent.Comp.ActionEntity is not { } action)
            return;
        _actions.RemoveAction(ent.Owner, action);
        if (_network.IsServer && !TerminatingOrDeleted(action))
            QueueDel(action);
        ent.Comp.ActionEntity = null;
        if (dirty)
            DirtyField(ent.AsNullable(), nameof(WaggingComponent.ActionEntity));
    }

    private bool TryGetAnimated(string id, string suffix, out string animated)
    {
        animated = id + suffix;
        if (suffix != "Animated")
            return suffix.Length > 0 && ProtoMan.HasIndex<MarkingPrototype>(animated);
        if (_markings.TryGetWaggingMarking(id, out var mapped))
        {
            animated = mapped;
            return true;
        }
        return false;
    }

    private bool TryGetStill(string id, string suffix, out string still)
    {
        if (suffix == "Animated" && _markings.TryGetStillMarking(id, out var mapped))
        {
            still = mapped;
            return true;
        }
        still = id;
        if (suffix == "Animated" || suffix.Length == 0 || !id.EndsWith(suffix, StringComparison.Ordinal))
            return false;
        still = id[..^suffix.Length];
        return ProtoMan.HasIndex<MarkingPrototype>(still);
    }
}
