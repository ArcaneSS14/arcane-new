using Content.Arcane.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Medical.Shared.Body;
using Content.Shared.ActionBlocker;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Xenobiology;

public sealed partial class SlimeCorpseEaterSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private BodyPartSystem _parts = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private const float EatRange = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeCorpseEaterComponent, SlimeEatCorpseEvent>(OnAction);
        SubscribeLocalEvent<SlimeCorpseEaterComponent, SlimeEatCorpseDoAfterEvent>(OnDoAfter);
    }

    private void OnAction(Entity<SlimeCorpseEaterComponent> ent, ref SlimeEatCorpseEvent args)
    {
        if (args.Handled || !TryStartEating(ent, args.Target, out _))
            return;

        args.Handled = true;
    }

    public bool CanTargetCorpse(EntityUid eater, EntityUid target)
    {
        return eater != target
            && !TerminatingOrDeleted(eater)
            && !TerminatingOrDeleted(target)
            && !EntityManager.IsQueuedForDeletion(target)
            && _mob.IsAlive(eater)
            && _mob.IsDead(target)
            && !_container.IsEntityInContainer(target)
            && TryComp<SlimeComponent>(eater, out var slime)
            && slime.LatchedTarget == null
            && slime.PendingLatchTarget == null;
    }

    public bool HasEdibleOrgans(EntityUid target) => FindEdibleOrgan(target) != null;

    public bool TryStartEating(EntityUid eater, EntityUid target, out DoAfterId? id)
    {
        id = null;
        if (!TryComp<SlimeCorpseEaterComponent>(eater, out var component)
            || component.ActiveTarget != null
            || !CanReachCorpse(eater, target)
            || !HasEdibleOrgans(target))
            return false;

        var args = new DoAfterArgs(EntityManager, eater, component.EatDuration, new SlimeEatCorpseDoAfterEvent(), eater, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        component.ActiveTarget = target;
        if (!_doAfter.TryStartDoAfter(args, out id))
        {
            component.ActiveTarget = null;
            return false;
        }

        if (component.ActiveTarget == null)
            return true;

        component.ActiveDoAfter = id;
        _jitter.DoJitter(target, component.EatDuration, true);
        _popup.PopupEntity(Loc.GetString("slime-eat-corpse-start", ("eater", eater), ("target", target)), eater, PopupType.MediumCaution);
        return true;
    }

    private void OnDoAfter(Entity<SlimeCorpseEaterComponent> ent, ref SlimeEatCorpseDoAfterEvent args)
    {
        if (ent.Comp.ActiveTarget == null || ent.Comp.ActiveTarget != args.Target
            || (ent.Comp.ActiveDoAfter is { } id && id != args.DoAfter.Id))
            return;

        ent.Comp.ActiveDoAfter = null;
        ent.Comp.ActiveTarget = null;
        if (args.Handled || args.Cancelled || args.Target is not { } target || !CanReachCorpse(ent, target)
            || FindEdibleOrgan(target) is not { } organ)
            return;

        if (_parts.GetRootPart(target)?.Owner == organ)
        {
            _gibbing.Gib(target, user: ent);
            args.Handled = EntityManager.IsQueuedForDeletion(target);
            return;
        }

        args.Handled = _body.GetBody(organ) == target && _body.RemoveOrgan(target, organ);
    }

    private bool CanReachCorpse(EntityUid eater, EntityUid target)
        => CanTargetCorpse(eater, target) && _actionBlocker.CanInteract(eater, target)
            && _interaction.InRangeUnobstructed(eater, target, EatRange);

    private EntityUid? FindEdibleOrgan(EntityUid target)
    {
        EntityUid? selected = null;
        var bestDepth = -1;
        foreach (var edible in _body.GetOrgans<EdibleComponent>(target))
        {
            if (TerminatingOrDeleted(edible) || EntityManager.IsQueuedForDeletion(edible))
                continue;

            var depth = 0;
            var parent = edible.Owner;
            while (_parts.GetParentPart(parent) is { } next && depth < 32)
            {
                depth++;
                parent = next;
            }

            if (depth <= bestDepth)
                continue;

            selected = edible.Owner;
            bestDepth = depth;
        }

        return selected;
    }
}
