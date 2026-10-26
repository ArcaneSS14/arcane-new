using Content.Shared.ActionBlocker;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Construction;

public sealed partial class ManualAnchoringSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private AnchorableSystem _anchorable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManualAnchoringComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
        SubscribeLocalEvent<ManualAnchoringComponent, ManualAnchoringDoAfterEvent>(OnManualAnchoringDoAfter);
    }

    private void OnGetAlternativeVerb(Entity<ManualAnchoringComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !TryComp(entity, out AnchorableComponent? anchorable) ||
            !TryComp(entity, out TransformComponent? transform))
            return;

        var anchor = !transform.Anchored;
        if (!CanUseDirection(anchorable, anchor))
            return;

        var user = args.User;
        var target = entity.Owner;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(anchor
                ? "arcane-verb-anchorable-title-anchor"
                : "arcane-verb-anchorable-title-unanchor"),
            Priority = 2,
            Act = () => TryStart(target, user, anchor)
        });
    }

    private void TryStart(EntityUid target, EntityUid user, bool anchor)
    {
        if (!CanUse(target, user, anchor) ||
            !_anchorable.TryGetManualAnchoringDelay(target, user, anchor, out var delay))
            return;

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new ManualAnchoringDoAfterEvent(anchor),
            target,
            target: target,
            used: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            DuplicateCondition = DuplicateConditions.SameEvent,
            BlockDuplicate = true,
            CancelDuplicate = true,
            FaceTarget = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnManualAnchoringDoAfter(Entity<ManualAnchoringComponent> entity,
        ref ManualAnchoringDoAfterEvent args)
    {
        if (args.Cancelled || !_net.IsServer || !CanUse(entity.Owner, args.User, args.Anchor))
            return;

        _anchorable.CompleteManualAnchoring(entity.Owner, args.User, args.Anchor);
    }

    private bool CanUse(EntityUid target, EntityUid user, bool anchor)
    {
        if (!Exists(target) || TerminatingOrDeleted(target) || !Exists(user) || TerminatingOrDeleted(user) ||
            !_actionBlocker.CanInteract(user, target) ||
            !_interaction.IsAccessible(user, target) ||
            !_interaction.InRangeUnobstructed(_transform.GetMapCoordinates(user), target))
            return false;

        if (!TryComp(target, out AnchorableComponent? anchorable) ||
            !TryComp(target, out TransformComponent? transform))
            return false;

        if (transform.Anchored == anchor)
            return false;

        return CanUseDirection(anchorable, anchor);
    }

    private static bool CanUseDirection(AnchorableComponent component, bool anchor)
    {
        var flag = anchor ? AnchorableFlags.Anchorable : AnchorableFlags.Unanchorable;
        return (component.Flags & flag) != 0;
    }
}

[Serializable, NetSerializable]
public sealed partial class ManualAnchoringDoAfterEvent : SimpleDoAfterEvent
{
    public bool Anchor;

    public ManualAnchoringDoAfterEvent(bool anchor)
    {
        Anchor = anchor;
    }

    private ManualAnchoringDoAfterEvent()
    {
    }
}
