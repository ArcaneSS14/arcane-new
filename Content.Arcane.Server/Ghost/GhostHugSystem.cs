using Content.Shared.Ghost.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Ghost;

/// <summary>A narrowly scoped ghost interaction using the existing hug outcome, cooldown and feedback.</summary>
public sealed partial class GhostHugSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private InteractionPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidProfileComponent, GetVerbsEvent<AlternativeVerb>>(OnVerbs);
    }

    private void OnVerbs(Entity<HumanoidProfileComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        var target = ent.Owner;
        if (!CanHug(user, target))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ghost-hug-verb"),
            DoContactInteraction = false,
            Act = () => TryHug(user, target),
        });
    }

    public bool CanHug(EntityUid user, EntityUid target)
    {
        return !TerminatingOrDeleted(user)
               && !TerminatingOrDeleted(target)
               && HasComp<GhostComponent>(user)
               && TryComp<ActorComponent>(user, out var actor)
               && actor.PlayerSession.AttachedEntity == user
               && HasComp<HumanoidProfileComponent>(target)
               && TryComp<InteractionPopupComponent>(target, out var popup)
               && popup.InteractSuccessSpawn == null
               && popup.InteractFailureSpawn == null
               && _mobState.IsAlive(target)
               && _interaction.InRangeUnobstructed(user, target);
    }

    /// <returns>Whether a permitted attempt reached the popup system; the success event determines its outcome.</returns>
    public bool TryHug(EntityUid user, EntityUid target)
    {
        if (!CanHug(user, target) || !TryComp<InteractionPopupComponent>(target, out var popup))
            return false;

        var attempt = new InteractHandEvent(user, target);
        _popup.SharedInteract(target, popup, attempt, target, user, ghostHug: true);
        return true;
    }
}
