using Content.Arcane.Common.Medical.Concussion;
using Content.Arcane.Shared.Medical.Concussion;
using Content.Shared.Alert;
using Content.Shared.Explosion;
using Content.Shared.Mind.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Common.Language.Systems;

namespace Content.Arcane.Server.Medical.Concussion;

public sealed partial class ConcussionSystem : SharedConcussionSystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private StatusEffectsSystem _effects = default!;
    [Dependency] private CommonLanguageSystem _languages = default!;

    private static readonly EntProtoId SpeechEffect = "StatusEffectConcussedSpeech";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ConcussionThresholdComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ConcussionThresholdComponent, BeforeExplodeEvent>(OnExplode);
        SubscribeLocalEvent<ConcussionThresholdComponent, FlashbangConcussionEvent>(OnFlashbang);
        SubscribeLocalEvent<ConcussionThresholdComponent, ConcussionStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<ConcussionThresholdComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ConcussionThresholdComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ConcussionThresholdComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<ConcussionThresholdComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.StoredDamage <= 0 || now < comp.NextUpdate)
                continue;
            var elapsed = (now - (comp.NextUpdate - comp.UpdateInterval)).TotalSeconds;
            SetDamage((uid, comp), comp.StoredDamage - Math.Max(0f, comp.HealRate) * elapsed);
            comp.NextUpdate = now + comp.UpdateInterval;
        }
    }

    private void OnMapInit(Entity<ConcussionThresholdComponent> ent, ref MapInitEvent args)
    {
        SetDamage(ent, ent.Comp.StoredDamage);
        ent.Comp.NextUpdate = Timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnExplode(Entity<ConcussionThresholdComponent> ent, ref BeforeExplodeEvent args)
    {
        AddConcussionDamage(ent, args.Damage.GetTotal() * ent.Comp.ExplosionDamageMultiplier);
    }

    private void OnFlashbang(Entity<ConcussionThresholdComponent> ent, ref FlashbangConcussionEvent args)
    {
        AddConcussionDamage(ent, args.Damage);
    }

    private void OnStateChanged(Entity<ConcussionThresholdComponent> ent, ref ConcussionStateChangedEvent args)
    {
        if (args.NewState == ConcussionState.Sane)
            ClearEffects(ent);
        else
        {
            EnsureComp<ConcussedComponent>(ent);
            _effects.TrySetStatusEffectDuration(ent, SpeechEffect);
            _alerts.ShowAlert(ent.Owner, "Concussion");
        }
        RefreshLanguages(ent);
    }

    private void RefreshLanguages(EntityUid body)
    {
        if (TryComp<LanguageSpeakerComponent>(body, out var speaker))
            _languages.UpdateEntityLanguages((body, speaker));
    }

    private void ClearEffects(EntityUid body)
    {
        RemComp<ConcussedComponent>(body);
        _effects.TryRemoveStatusEffect(body, SpeechEffect);
        _alerts.ClearAlert(body, "Concussion");
    }

    private void OnRejuvenate(Entity<ConcussionThresholdComponent> ent, ref RejuvenateEvent args)
    {
        SetDamage(ent, 0);
    }

    private void OnMindRemoved(Entity<ConcussionThresholdComponent> ent, ref MindRemovedMessage args)
    {
        SetDamage(ent, 0);
    }

    private void OnShutdown(Entity<ConcussionThresholdComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.StoredDamage = 0;
        ent.Comp.CurrentState = ConcussionState.Sane;
        if (!TerminatingOrDeleted(ent))
        {
            ClearEffects(ent);
            Movement.RefreshMovementSpeedModifiers(ent.Owner);
            RefreshLanguages(ent);
        }
    }
}
