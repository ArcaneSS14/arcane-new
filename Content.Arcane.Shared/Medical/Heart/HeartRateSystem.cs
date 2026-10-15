using Content.Shared.Body.Events;
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
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _blood = default!;

    private static readonly float HeartStop = 0f;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;

        SubscribeLocalEvent<HeartRateComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HeartRateComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BodyComponent, FindWorkingHeartEvent>(OnFindHeart);
        SubscribeLocalEvent<HeartRateComponent, OrganRemovalDeathAttemptEvent>(OnRemovalDeathAttempt);
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
        var maybeBody = _body.GetBody(uid);
        var beyondCritical = Math.Max(0, heart.CurrentRate / heart.CriticalRate - 1);
        if (maybeBody is null // the heart is outside a body
            || !TryComp<MobStateComponent>(maybeBody, out var mobState) // or the body is not a mob
            || mobState.CurrentState == MobState.Dead // or the body is dead
            || beyondCritical > 0)
        {
            var chance = heart.CriticalStopChance * (1 + beyondCritical);
            if (_random.Prob(chance))
                SetRate(uid, heart, HeartStop, false);
        }

        if (maybeBody is { } body)
            DoBloodVolume(uid, heart, body);

        // fibrillating drifts AWAY from the normal heart rate (towards min/max)
        // being stable drifts TOWARDS the normal heart rate
        var cur = heart.CurrentRate;
        var (min, max) = heart.FibrillationCaps;
        var delta = heart.RateUpdateModifier * (float) Math.Cbrt(
            (cur - heart.NormalRate) * (cur - min) * (cur - max)
        );

        UpdateRate(uid, heart, delta, false);
    }

    private void OnRemovalDeathAttempt(Entity<HeartRateComponent> ent, ref OrganRemovalDeathAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnFindHeart(Entity<BodyComponent> ent, ref FindWorkingHeartEvent args)
    {
        if (HasComp<StasisBedBuckledComponent>(ent))
        {
            args.DoEffects = false;
            return;
        }

        var hearts = _body.GetOrgans<HeartRateComponent>(ent.AsType());
        foreach (var heart in hearts)
            if (GetState(heart.Comp) != HeartState.Stopped)
            {
                args.Found = true;
                return;
            }
    }

    private void DoBloodVolume(EntityUid uid, HeartRateComponent heart, EntityUid body)
    {
        if (!_blood.TryComp(body, out var blood)
            || !_solutionContainer.ResolveSolution(body, blood.BloodSolutionName, ref blood.BloodSolution, out var solution)
            || blood.BloodReferenceSolution.Volume <= 0
            || heart.CurrentRate <= HeartStop)
            return;

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

        // being at min or beyond just stops the heart
        heart.CurrentRate = Math.Max(rate, HeartStop);

        var newState = GetState(heart);
        Dirty(uid, heart);

        if (oldState == newState // nothing changed
            || _body.GetBody(uid) is not { } body)
            return;

        var ev = new HeartStateChangedEvent(oldState, newState);
        RaiseLocalEvent(body, ref ev);

        // update alerts
        if (heart.FibrillationAlert is { } fibAlert)
        {
            if (newState == HeartState.Fibrillating)
                _alerts.ShowAlert(body, fibAlert);
            else
                _alerts.ClearAlert(body, fibAlert);
        }

        if (heart.HeartStopAlert is { } stopAlert)
        {
            if (newState == HeartState.Stopped)
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
        float? highCap = null)
    {
        var newRate = heart.CurrentRate + delta;

        if (lowCap is { } someLowCap && newRate < someLowCap)
            newRate = someLowCap;

        if (highCap is { } someHighCap && newRate > someHighCap)
            newRate = someHighCap;

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
