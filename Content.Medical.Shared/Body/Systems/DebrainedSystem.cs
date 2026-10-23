// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;
using Content.Shared.Body.Components; // Arcane

namespace Content.Medical.Shared.Body;

// TODO SHITMED: kill this and HeartSystem fucking aids
/// <summary>
///     This system handles behavior on entities when they lose their head or their brains are removed.
///     MindComponent fuckery should still be mainly handled on BrainSystem as usual.
/// </summary>
public sealed partial class DebrainedSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebrainedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DebrainedComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<DebrainedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<DebrainedComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<DebrainedComponent, RejuvenateEvent>(OnRejuvenate,
            before: new []{ typeof(DamageableSystem), typeof(BodyRestoreSystem) });
    }

    private void OnComponentInit(EntityUid uid, DebrainedComponent _, ComponentInit args)
    {
        // the components are networked, don't need to let it do weird shit
        if (_timing.ApplyingState)
            return;

        EnsureComp<DelayedDeathComponent>(uid);
        EnsureComp<StunnedComponent>(uid);
        _standing.Down(uid);
    }

    private void OnComponentRemove(EntityUid uid, DebrainedComponent _, ComponentRemove args)
    {
        if (TerminatingOrDeleted(uid) || _timing.ApplyingState)
            return;

        RemComp<StunnedComponent>(uid);
        // Arcane-Edit-Start
        // Circulation handles a missing heart; bodies without it still need the legacy death timer.
        if (_body.GetOrgan(uid, Heart) != null || HasComp<BloodstreamComponent>(uid))
            RemComp<DelayedDeathComponent>(uid);
        // Arcane-Edit-End
    }

    private void OnSpeakAttempt(EntityUid uid, DebrainedComponent _, SpeakAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("speech-muted"), uid, uid);
        args.Cancel();
    }

    private void OnStandAttempt(EntityUid uid, DebrainedComponent _, StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRejuvenate(Entity<DebrainedComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }
}
