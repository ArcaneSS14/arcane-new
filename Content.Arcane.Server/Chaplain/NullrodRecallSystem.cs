using Content.Arcane.Shared.Chaplain;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Goobstation.Shared.Religion.Nullrod.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Bible.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Chaplain;

/// <summary>
/// Binds actual entities to a mind, including the children of an altar-issued holster.
/// Recall moves those entities through the hands owner and never finds substitutes.
/// </summary>
public sealed partial class NullrodRecallSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NullrodComponent, GetVerbsEvent<ActivationVerb>>(OnBindVerb);
        SubscribeLocalEvent<NullrodComponent, NullrodTransformAttemptEvent>(OnTransformAttempt);
        SubscribeLocalEvent<NullrodComponent, NullrodTransformedEvent>(OnTransformed);
        SubscribeLocalEvent<NullrodRecallAltarComponent, GetVerbsEvent<ActivationVerb>>(OnRecallVerb);
        SubscribeLocalEvent<NullrodRecallAltarComponent, NullrodRecallDoAfterEvent>(OnRecallDoAfter);
        SubscribeLocalEvent<BoundNullrodComponent, ComponentShutdown>(OnFormShutdown);
        SubscribeLocalEvent<NullrodBindingComponent, ComponentShutdown>(OnMindShutdown);
    }

    private void OnBindVerb(Entity<NullrodComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !CanBind(args.User, ent, out _))
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("arcane-nullrod-bind"),
            Act = () => TryBind(user, ent),
        });
    }

    /// <summary>Bind one unowned form without replacing a mind's surviving set.</summary>
    public bool TryBind(EntityUid user, EntityUid form)
    {
        if (!CanBind(user, form, out var mind)
            || !_actionBlocker.CanInteract(user, form)
            || !_interaction.IsAccessible(user, form)
            || !_interaction.InRangeUnobstructed(user, form))
            return false;

        var binding = EnsureComp<NullrodBindingComponent>(mind);
        binding.Forms.Add(form);
        EnsureComp<BoundNullrodComponent>(form).Mind = mind;
        _popup.PopupEntity(Loc.GetString("arcane-nullrod-bound"), user, user);
        return true;
    }

    private bool CanBind(EntityUid user, EntityUid form, out EntityUid mind)
    {
        if (!TryGetChaplainMind(user, out mind)
            || TerminatingOrDeleted(form)
            || EntityManager.IsQueuedForDeletion(form)
            || !HasComp<NullrodComponent>(form)
            || HasComp<BoundNullrodComponent>(form))
            return false;

        return !TryComp<NullrodBindingComponent>(mind, out var binding) || binding.Forms.Count == 0;
    }

    private bool TryGetChaplainMind(EntityUid user, out EntityUid mind)
    {
        mind = default;
        return !TerminatingOrDeleted(user)
            && !EntityManager.IsQueuedForDeletion(user)
            && HasComp<BibleUserComponent>(user)
            && _mind.TryGetMind(user, out mind, out var comp)
            && !TerminatingOrDeleted(mind)
            && !EntityManager.IsQueuedForDeletion(mind)
            && comp.OwnedEntity == user;
    }

    private void OnRecallVerb(Entity<NullrodRecallAltarComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryGetChaplainMind(args.User, out _))
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("arcane-nullrod-recall"),
            Act = () => TryStartRecall(user, ent),
        });
    }

    /// <summary>Start a prayer whose completion revalidates the original mind and altar.</summary>
    public bool TryStartRecall(EntityUid user, EntityUid altar)
    {
        if (TerminatingOrDeleted(altar)
            || EntityManager.IsQueuedForDeletion(altar)
            || !TryComp<NullrodRecallAltarComponent>(altar, out var comp)
            || !TryGetChaplainMind(user, out var mind)
            || !_actionBlocker.CanInteract(user, altar)
            || !_interaction.IsAccessible(user, altar)
            || !_interaction.InRangeUnobstructed(user, altar))
            return false;

        if (!TryComp<NullrodBindingComponent>(mind, out var binding) || binding.Forms.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("arcane-nullrod-no-binding"), user, user);
            return false;
        }

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, comp.Duration,
            new NullrodRecallDoAfterEvent { Mind = GetNetEntity(mind) }, altar, target: altar)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            NeedAnyFreeHand = true,
            BlockDuplicate = true,
            CancelDuplicate = false,
            MultiplyDelay = false,
        });
    }

    private void OnRecallDoAfter(Entity<NullrodRecallAltarComponent> ent, ref NullrodRecallDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled
            || TerminatingOrDeleted(ent)
            || EntityManager.IsQueuedForDeletion(ent.Owner)
            || !TryGetChaplainMind(args.User, out var mind)
            || GetNetEntity(mind) != args.Mind
            || !_actionBlocker.CanInteract(args.User, ent)
            || !_interaction.IsAccessible(args.User, ent.Owner)
            || !_interaction.InRangeUnobstructed(args.User, ent.Owner))
            return;

        args.Handled = true;
        var count = TryRecall(args.User);
        _popup.PopupEntity(Loc.GetString(count > 0 ? "arcane-nullrod-recalled" : "arcane-nullrod-recall-failed"),
            args.User, args.User);
    }

    /// <summary>Best-effort recall of surviving owned forms; each successful pickup is retained.</summary>
    private int TryRecall(EntityUid user)
    {
        if (!TryGetChaplainMind(user, out var mind)
            || !TryComp<NullrodBindingComponent>(mind, out var binding))
            return 0;

        var recalled = 0;
        // Pickup can raise lifecycle events, so iterate a snapshot of this mind's bounded set.
        foreach (var form in new List<EntityUid>(binding.Forms))
        {
            if (TerminatingOrDeleted(form)
                || EntityManager.IsQueuedForDeletion(form)
                || HasComp<NullrodRecallBlockedComponent>(form)
                || !TryComp<BoundNullrodComponent>(form, out var bound)
                || bound.Mind != mind
                || _hands.IsHolding(user, form)
                || !_hands.TryGetEmptyHand(user, out _))
                continue;

            if (TryRecallForm(user, form))
                recalled++;
        }

        return recalled;
    }

    private bool TryRecallForm(EntityUid user, EntityUid form)
    {
        TryComp<EmbeddableProjectileComponent>(form, out var embedded);
        if (embedded is { EmbeddedIntoUid: not null, DeleteOnRemove: true })
            return false;

        var unremoveable = TryComp<UnremoveableComponent>(form, out var restriction);
        var deleteOnDrop = restriction?.DeleteOnDrop ?? false;
        if (unremoveable)
            RemComp<UnremoveableComponent>(form);

        try
        {
            if (embedded?.EmbeddedIntoUid != null)
                _projectile.EmbedDetach(form, embedded);

            return _hands.TryPickupAnyHand(user, form);
        }
        finally
        {
            // A denied pickup must retain the exact original restriction as well.
            if (unremoveable && !TerminatingOrDeleted(form))
            {
                var restored = EnsureComp<UnremoveableComponent>(form);
                restored.DeleteOnDrop = deleteOnDrop;
                Dirty(form, restored);
            }
        }
    }

    private void OnTransformAttempt(Entity<NullrodComponent> ent, ref NullrodTransformAttemptEvent args)
    {
        var trained = TryGetChaplainMind(args.User, out var mind);
        if (TryComp<BoundNullrodComponent>(ent, out var bound) && (!trained || bound.Mind != mind))
            args.Cancelled = true;

        if (trained && TryComp<NullrodBindingComponent>(mind, out var binding)
            && binding.Forms.Count > 0
            && (binding.Forms.Count != 1 || !binding.Forms.Contains(ent)))
            args.Cancelled = true;
    }

    private void OnTransformed(Entity<NullrodComponent> ent, ref NullrodTransformedEvent args)
    {
        if (!TryGetChaplainMind(args.User, out var mind))
            return;

        var binding = EnsureComp<NullrodBindingComponent>(mind);
        binding.Forms.Remove(ent);
        if (HasComp<BoundNullrodComponent>(ent))
            RemComp<BoundNullrodComponent>(ent);

        BindSpawnedForms(args.Replacement, mind, binding);
    }

    private void BindSpawnedForms(EntityUid root, EntityUid mind, NullrodBindingComponent binding)
    {
        if (HasComp<NullrodComponent>(root))
        {
            binding.Forms.Add(root);
            EnsureComp<BoundNullrodComponent>(root).Mind = mind;
        }

        if (!HasComp<ContainerManagerComponent>(root))
            return;

        // Only inspect the just-created replacement's descendants, never other matching guns.
        foreach (var container in _containers.GetAllContainers(root))
        {
            foreach (var child in container.ContainedEntities)
                BindSpawnedForms(child, mind, binding);
        }
    }

    private void OnFormShutdown(Entity<BoundNullrodComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<NullrodBindingComponent>(ent.Comp.Mind, out var binding))
            binding.Forms.Remove(ent);
    }

    private void OnMindShutdown(Entity<NullrodBindingComponent> ent, ref ComponentShutdown args)
    {
        foreach (var form in new List<EntityUid>(ent.Comp.Forms))
        {
            if (!TerminatingOrDeleted(form))
                RemComp<BoundNullrodComponent>(form);
        }

        ent.Comp.Forms.Clear();
    }
}
