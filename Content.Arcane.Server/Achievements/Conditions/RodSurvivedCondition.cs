using Content.Server.ImmovableRod;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.GameTicking;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class RodSurvivedCondition : AchievementCondition;

public sealed partial class RodSurvivedConditionSystem : AchievementConditionSystem<RodSurvivedCondition>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly TimeSpan SurviveDelay = TimeSpan.FromSeconds(5);

    private readonly Dictionary<EntityUid, (TimeSpan Expires, NetUserId User)> _pending = new();
    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _pending.Clear();
        _toRemove.Clear();
    }

    private void OnCollide(Entity<MobStateComponent> ent, ref StartCollideEvent args)
    {
        if (Entries.Count == 0 || !HasComp<ImmovableRodComponent>(args.OtherEntity) || _mobState.IsDead(ent, ent.Comp)
            || !Achievements.CanProgress(ent.Owner, out var session))
            return;

        _pending.TryAdd(ent, (_timing.CurTime + SurviveDelay, session.UserId));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pending.Count == 0)
            return;

        var now = _timing.CurTime;
        foreach (var (uid, hit) in _pending)
        {
            if (hit.Expires > now)
                continue;

            _toRemove.Add(uid);

            if (!TerminatingOrDeleted(uid) && TryComp<MobStateComponent>(uid, out var state)
                && !_mobState.IsDead(uid, state) && Achievements.TryGetSession(uid, out var session)
                && session.UserId == hit.User)
                ProgressAll(uid);
        }

        foreach (var uid in _toRemove)
        {
            _pending.Remove(uid);
        }

        _toRemove.Clear();
    }
}
