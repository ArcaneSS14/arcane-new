using Content.Medical.Shared.Body;
using Content.Server.NPC.HTN;
using Content.Shared.Body;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Materials;
using Content.Server.Power.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Silicon.Bots;

/// <summary>
/// Owns Fillbot link changes and validates pickup and delivery at execution time.
/// Navigation remains owned by the existing HTN and steering systems.
/// </summary>
public sealed partial class FillbotSystem : EntitySystem
{
    public const string SourcePort = "FillItems";
    public const string SinkPort = "FillbotAnyItem";
    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    [Dependency] private SharedDeviceLinkSystem _links = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMaterialStorageSystem _materials = default!;
    [Dependency] private SharedDisposalUnitSystem _disposals = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FillbotComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<FillbotComponent, NewLinkEvent>(OnLinked);
        SubscribeLocalEvent<FillbotComponent, PortDisconnectedEvent>(OnUnlinked);
        SubscribeLocalEvent<FillbotComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FillbotComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnUnpaused(Entity<FillbotComponent> ent, ref EntityUnpausedEvent args)
    {
        foreach (var (target, retryAfter) in ent.Comp.BlockedItems)
            ent.Comp.BlockedItems[target] = retryAfter + args.PausedTime;
    }

    private void OnShutdown(Entity<FillbotComponent> ent, ref ComponentShutdown args)
    {
        if (!IsUsable(ent))
            return;

        if (TryComp<HTNComponent>(ent, out var htn))
            _htn.SetHTNEnabled((ent.Owner, htn), false);
        _hands.TryDrop(ent.Owner);
    }

    private void OnLinkAttempt(Entity<FillbotComponent> ent, ref LinkAttemptEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != SourcePort ||
            args.SinkPort != SinkPort || !IsReceiver(args.Sink))
            args.Cancel();
    }

    private void OnLinked(Entity<FillbotComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != SourcePort || args.SinkPort != SinkPort)
            return;

        // GetLinkedSinks returns a snapshot; link removal raises nested unlink events.
        foreach (var oldSink in _links.GetLinkedSinks(ent.Owner, SourcePort))
        {
            if (oldSink != args.Sink)
                _links.RemoveSinkFromSource(ent.Owner, oldSink);
        }

        ResetTask(ent);
    }

    private void OnUnlinked(Entity<FillbotComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port == SourcePort)
            ResetTask(ent);
    }

    private void ResetTask(Entity<FillbotComponent> ent)
    {
        if (!IsUsable(ent))
            return;

        _hands.TryDrop(ent.Owner);
        ent.Comp.BlockedItems.Clear();

        if (TryComp<HTNComponent>(ent, out var htn) && htn.Enabled)
        {
            _htn.SetHTNEnabled((ent.Owner, htn), false);
            // Unlink events are raised before the underlying link is removed.
            _htn.SetHTNEnabled((ent.Owner, htn), true, htn.PlanCooldown);
        }
    }

    public bool IsUsable(EntityUid uid)
        => !TerminatingOrDeleted(uid) && !EntityManager.IsQueuedForDeletion(uid);

    private bool IsReceiver(EntityUid uid)
        => IsUsable(uid) && (HasComp<MaterialStorageComponent>(uid) || HasComp<DisposalUnitComponent>(uid));

    public bool SameMap(EntityUid owner, EntityUid target)
        => IsUsable(owner) && IsUsable(target) &&
           TryComp(owner, out TransformComponent? ownerTransform) &&
           TryComp(target, out TransformComponent? targetTransform) &&
           ownerTransform.MapID == targetTransform.MapID;

    /// <summary>
    /// Uses the canonical link registry, including links restored from a saved map.
    /// No separate nonserialized sink cache can become stale.
    /// </summary>
    public bool TryGetSink(EntityUid owner, out EntityUid sink)
    {
        sink = default;
        if (!IsUsable(owner) || !HasComp<FillbotComponent>(owner))
            return false;

        var linked = _links.GetLinkedSinks(owner, SourcePort);
        if (linked.Count != 1)
            return false;

        foreach (var candidate in linked)
        {
            if (!IsReceiver(candidate) || !SameMap(owner, candidate) ||
                !_links.GetLinks(owner, candidate).Contains((SourcePort, SinkPort)))
                return false;

            sink = candidate;
        }

        return true;
    }

    public bool IsBlocked(EntityUid owner, EntityUid item, EntityUid sink)
        => TryComp<FillbotComponent>(owner, out var bot) &&
           bot.BlockedItems.TryGetValue((item, sink), out var retryAfter) && _timing.CurTime < retryAfter;

    public bool CanLoad(EntityUid item, EntityUid sink)
    {
        if (!IsUsable(item) || !IsReceiver(sink) || !HasComp<ItemComponent>(item) ||
            HasComp<BodyPartComponent>(item))
            return false;

        if (TryComp<MaterialStorageComponent>(sink, out var storage))
        {
            if (TryComp<ApcPowerReceiverComponent>(sink, out var power) && !power.Powered)
                return false;

            return _materials.CanInsertMaterialEntity(item, sink, storage);
        }

        return TryComp<DisposalUnitComponent>(sink, out var disposal) &&
               !HasComp<BodyComponent>(item) &&
               _tags.HasTag(item, TrashTag) &&
               !_whitelist.IsWhitelistFail(disposal.Whitelist, item) &&
               disposal.Container != null &&
               _containers.CanInsert(item, disposal.Container);
    }

    public bool CanCollect(EntityUid owner, EntityUid item, EntityUid sink)
        => SameMap(owner, item) && !IsBlocked(owner, item, sink) &&
           !_containers.IsEntityInContainer(item) &&
           _hands.GetActiveItem(owner) == null &&
           _hands.CanPickupAnyHand(owner, item) && CanLoad(item, sink);

    public bool TryPickup(EntityUid owner, EntityUid item, EntityUid expectedSink)
    {
        if (!TryGetSink(owner, out var sink) || sink != expectedSink ||
            !CanCollect(owner, item, sink))
            return false;

        if (_interaction.InRangeAndAccessible(owner, item) && _hands.TryPickupAnyHand(owner, item))
            return true;

        BlockAfterFailure(owner, item, sink);
        return false;
    }

    public bool TryDeliver(EntityUid owner, EntityUid item, EntityUid expectedSink)
    {
        if (!TryGetSink(owner, out var sink) || sink != expectedSink ||
            _hands.GetActiveItem(owner) != item ||
            !CanLoad(item, sink) || !_interaction.InRangeAndAccessible(owner, sink))
            return false;

        if (TryComp<MaterialStorageComponent>(sink, out var storage))
            return _materials.TryInsertMaterialEntity(owner, item, sink, storage);

        return TryComp<DisposalUnitComponent>(sink, out var disposal) &&
               _disposals.TryInsert((sink, disposal), item, owner);
    }

    public void DropAfterFailure(EntityUid owner, EntityUid item, EntityUid sink)
    {
        if (!BlockAfterFailure(owner, item, sink))
            return;

        if (_hands.GetActiveItem(owner) == item)
            _hands.TryDrop(owner);
    }

    private bool BlockAfterFailure(EntityUid owner, EntityUid item, EntityUid sink)
    {
        if (!IsUsable(owner) || !TryComp<FillbotComponent>(owner, out var bot))
            return false;

        var now = _timing.CurTime;
        foreach (var (target, retryAfter) in bot.BlockedItems)
        {
            if (retryAfter <= now)
                bot.BlockedItems.Remove(target);
        }

        bot.BlockedItems[(item, sink)] = now + bot.FailureRetryDelay;
        return true;
    }
}
