using Content.Arcane.Common.Medical.Heart;
using Content.Medical.Common.Body;
using Content.Shared.Alert;
using Content.Shared.Bed.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.Medical.Heart;

public sealed partial class HeartRateSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private InitialBodySystem _initialBody = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _blood = default!;

    private static readonly float HeartStop = 0f;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;

        SubscribeLocalEvent<HeartRateComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HeartRateComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BloodstreamComponent, FindWorkingHeartEvent>(OnFindHeart);
        SubscribeLocalEvent<HeartRateComponent, BodyRelayedEvent<FindWorkingHeartEvent>>(OnRelayedFindHeart);
        SubscribeLocalEvent<HeartRateComponent, OrganRemovalDeathAttemptEvent>(OnRemovalDeathAttempt);
        SubscribeLocalEvent<HeartRateComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<HeartRateComponent, OrganGotRemovedEvent>(OnRemoved);
    }

    private void OnComponentInit(EntityUid uid, HeartRateComponent heart, ComponentInit args)
    {
        if (!_net.IsClient)
            SetRate(uid, heart, heart.NormalRate, true);
    }

    private void OnRejuvenate(EntityUid uid, HeartRateComponent heart, RejuvenateEvent args)
    {
        if (!_net.IsClient)
            SetRate(uid, heart, heart.NormalRate, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<HeartRateComponent>();
        while (eqe.MoveNext(out var uid, out var heart))
        {
            UpdateHeart(uid, heart);
        }
    }

    private void UpdateHeart(EntityUid uid, HeartRateComponent heart)
    {
        if (heart.CurrentRate <= HeartStop)
            return;

        var maybeBody = _body.GetBody(uid);
        var beyondCritical = Math.Max(0, heart.CurrentRate / heart.CriticalRate - 1);
        var inactive = maybeBody is null // the heart is outside a body
            || !TryComp<MobStateComponent>(maybeBody, out var mobState) // or the body is not a mob
            || mobState.CurrentState == MobState.Dead; // or the body is dead
        if (inactive || beyondCritical > 0)
        {
            var chance = heart.CriticalStopChance * (1 + beyondCritical);
            if (_random.Prob(chance))
                SetRate(uid, heart, HeartStop, false);
        }

        // Preserve the source's delayed stopping chance, without simulating circulation in a dead body.
        if (inactive || heart.CurrentRate <= HeartStop)
            return;

        if (maybeBody is { } body)
            DoBloodVolume(uid, heart, body);

        // fibrillating drifts AWAY from the normal heart rate (towards min/max)
        // being stable drifts TOWARDS the normal heart rate
        var cur = heart.CurrentRate;
        var (min, max) = heart.FibrillationCaps;
        var lowerDistance = cur - min;
        var upperDistance = cur - max;
        // Both bounds are stable. A one-BPM inward offset prevents extra equilibria there.
        if (lowerDistance == 0f)
            lowerDistance = 1f;
        if (upperDistance == 0f)
            upperDistance = -1f;

        var delta = heart.RateUpdateModifier * (float) Math.Cbrt(
            (cur - heart.NormalRate) * lowerDistance * upperDistance
        );

        if (cur >= min && cur <= max)
            delta = Math.Clamp(delta, Math.Min(0f, heart.NormalRate - cur), Math.Max(0f, heart.NormalRate - cur));

        UpdateRate(uid, heart, delta, false);
    }

    private void OnRemovalDeathAttempt(Entity<HeartRateComponent> ent, ref OrganRemovalDeathAttemptEvent args)
    {
        // BodySystem keeps the old body reference until removal events finish.
        args.Cancelled = _body.GetBody(ent) is { } body && _blood.HasComp(body);
    }

    private void OnInserted(Entity<HeartRateComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_net.IsClient && !_timing.ApplyingState)
            UpdateAlerts(args.Target, ent.Comp);
    }

    private void OnRemoved(Entity<HeartRateComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!_net.IsClient && !_timing.ApplyingState && !TerminatingOrDeleted(args.Target))
            UpdateAlerts(args.Target, ent.Comp);
    }

    public bool RequiresHeart(EntityUid body)
    {
        return _blood.HasComp(body)
            && (!TryComp<HeartCirculationComponent>(body, out var circulation) || circulation.RequiresHeart)
            && HasComp<BodyComponent>(body)
            && _initialBody.HasInitialOrgan(body, HeartCategory) != false;
    }

    private void OnFindHeart(Entity<BloodstreamComponent> ent, ref FindWorkingHeartEvent args)
    {
        args.DoEffects = RequiresHeart(ent) && !HasComp<StasisBedBuckledComponent>(ent);
        if (!args.DoEffects || !TryComp<BodyComponent>(ent, out var body))
            return;

        _body.RelayEvent((ent.Owner, body), ref args);
    }

    private void OnRelayedFindHeart(Entity<HeartRateComponent> ent, ref BodyRelayedEvent<FindWorkingHeartEvent> args)
    {
        var result = args.Args;
        result.Found |= GetState(ent.Comp) != HeartState.Stopped;
        args.Args = result;
    }

    private void DoBloodVolume(EntityUid uid, HeartRateComponent heart, EntityUid body)
    {
        // Compensating for blood loss must not cure bradycardia.
        if (heart.CurrentRate < heart.NormalRate)
            return;

        if (!_blood.TryComp(body, out var blood)
            || !_solutionContainer.ResolveSolution(body, blood.BloodSolutionName, ref blood.BloodSolution, out var solution)
            || blood.BloodReferenceSolution.Volume <= 0
            || heart.CurrentRate <= HeartStop)
            return;

        // This models circulating fluid volume; oxygenation handles the reagent composition.
        // Excess blood does not lower the pulse below its resting rate.
        var bloodPart = Math.Min(1f, (solution.Volume / blood.BloodReferenceSolution.Volume).Float());

        var newRate = heart.NormalRate * (2 - bloodPart); // i.e. 30% of bs volume = +70% to the bpm
        if (newRate > heart.CurrentRate)
            SetRate(uid, heart, newRate, false);
    }

    #region api

    public void SetRate(EntityUid uid,
        HeartRateComponent heart,
        float rate,
        bool canRestart)
    {
        var oldState = GetState(heart);
        if (oldState == HeartState.Stopped && !canRestart)
            return;

        rate = Math.Max(rate, HeartStop);
        if (heart.CurrentRate == rate)
            return;

        heart.CurrentRate = rate;

        var newState = GetState(heart);
        Dirty(uid, heart);

        if (oldState == newState // nothing changed
            || _body.GetBody(uid) is not { } body)
            return;

        UpdateAlerts(body, heart);
    }

    private void UpdateAlerts(EntityUid body, HeartRateComponent heart)
    {
        var state = HeartState.Stable;
        if (RequiresHeart(body))
        {
            state = HeartState.Stopped;
            foreach (var organ in _body.GetOrgans<HeartRateComponent>(body))
            {
                var organState = GetState(organ.Comp);
                if (organState == HeartState.Stopped)
                    continue;

                state = organState;
                if (state == HeartState.Stable)
                    break;
            }
        }

        if (heart.FibrillationAlert is { } fibAlert)
        {
            if (state == HeartState.Fibrillating)
                _alerts.ShowAlert(body, fibAlert);
            else
                _alerts.ClearAlert(body, fibAlert);
        }

        if (heart.HeartStopAlert is { } stopAlert)
        {
            if (state == HeartState.Stopped)
                _alerts.ShowAlert(body, stopAlert);
            else
                _alerts.ClearAlert(body, stopAlert);
        }
    }

    public void UpdateRate(EntityUid uid,
        HeartRateComponent heart,
        float delta,
        bool canRestart,
        float? lowCap = null,
        float? highCap = null,
        bool autoStabilisation = false)
    {
        var oldRate = heart.CurrentRate;
        var stabilising = autoStabilisation && delta >= 0;
        if (autoStabilisation)
            delta *= oldRate > heart.NormalRate ? -1 : 1;

        var newRate = heart.CurrentRate + delta;

        if (stabilising && (heart.NormalRate - oldRate) * (heart.NormalRate - newRate) <= 0)
            newRate = heart.NormalRate;

        // A cap must limit an effect, never reverse it when the rate is already outside the cap.
        if (delta < 0 && lowCap is { } someLowCap)
            newRate = Math.Min(oldRate, Math.Max(newRate, someLowCap));

        if (delta > 0 && highCap is { } someHighCap)
            newRate = Math.Max(oldRate, Math.Min(newRate, someHighCap));

        SetRate(uid, heart, newRate, canRestart);
    }

    public HeartState GetState(HeartRateComponent heart)
    {
        if (heart.CurrentRate <= HeartStop)
            return HeartState.Stopped;

        var (min, max) = heart.FibrillationCaps;
        if (heart.CurrentRate > max || heart.CurrentRate < min)
            return HeartState.Fibrillating;

        return HeartState.Stable;
    }

    #endregion
}
