// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq; // Arcane

namespace Content.Lavaland.Shared.Aggression;

public sealed partial class AggressorsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedBossMusicSystem _bossMusic = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private readonly List<EntityUid> _expiredAggressors = new(); // Arcane

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AggressiveComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<AggressiveComponent, EntityTerminatingEvent>(OnDeleted);
        SubscribeLocalEvent<AggressiveComponent, MobStateChangedEvent>(OnStateChange);

        SubscribeLocalEvent<AggressorComponent, MobStateChangedEvent>(OnAggressorStateChange);
        SubscribeLocalEvent<AggressorComponent, EntityTerminatingEvent>(OnAggressorDeleted);
        SubscribeLocalEvent<AggressorComponent, AggressiveAddedEvent>(OnAggressorAdded);
        SubscribeLocalEvent<AggressorComponent, AggressiveRemovedEvent>(OnAggressorRemoved);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        // All who are aggressive check their aggressors, and remove them if they are too far away.
        var query = EntityQueryEnumerator<AggressiveComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var aggressive, out var xform))
        {
            if (aggressive.ForgiveRange == null
                || aggressive.NextUpdate > curTime) // Arcane-Edit: < > >
                continue;

            aggressive.NextUpdate = curTime + aggressive.UpdateDelay;
            _expiredAggressors.Clear(); // Arcane

            foreach (var aggressor in aggressive.Aggressors)
            {
                // Arcane-Edit-Start
                if (!_xformQuery.TryComp(aggressor, out var aggroXform) || xform.MapID != aggroXform.MapID)
                {
                    _expiredAggressors.Add(aggressor);
                    continue;
                }
                // Arcane-Edit-End

                var aggroPos = _xform.GetWorldPosition(aggroXform);
                var aggressivePos = _xform.GetWorldPosition(xform);
                var distance = (aggressivePos - aggroPos).Length();

                // Arcane-Edit-Start
                if (distance > aggressive.ForgiveRange)
                    _expiredAggressors.Add(aggressor);
                // Arcane-Edit-End
            }

            // Arcane-Start
            foreach (var aggressor in _expiredAggressors)
                RemoveAggressor((uid, aggressive), aggressor);
            // Arcane-End
        }
    }

    #region Event Handling

    private void OnDamageChanged(Entity<AggressiveComponent> ent, ref DamageChangedEvent args)
    {
        var aggro = args.Origin;

        if (aggro == null
            || !HasComp<ActorComponent>(aggro))
            return;

        AddAggressor(ent, aggro.Value);
    }

    private void OnDeleted(Entity<AggressiveComponent> ent, ref EntityTerminatingEvent args)
        => RemoveAllAggressors(ent);

    private void OnStateChange(Entity<AggressiveComponent> ent, ref MobStateChangedEvent args)
        => RemoveAllAggressors(ent);

    private void OnAggressorAdded(Entity<AggressorComponent> ent, ref AggressiveAddedEvent args)
    {
        if (ent.Comp.Aggressives.TryFirstOrNull(out var boss))
            _bossMusic.StartBossMusic(boss.Value);
    }

    private void OnAggressorRemoved(Entity<AggressorComponent> ent, ref AggressiveRemovedEvent args)
        => _bossMusic.EndAllMusic(); // Stop the music if we are no longer get attacked by anyone.

    private void OnAggressorStateChange(Entity<AggressorComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsDead(ent.Owner))
            CleanAggressions((ent.Owner, ent.Comp));
    }

    private void OnAggressorDeleted(Entity<AggressorComponent> ent, ref EntityTerminatingEvent args)
        => CleanAggressions((ent.Owner, ent.Comp));

    #endregion

    #region Aggressive API

    public void AddAggressor(Entity<AggressiveComponent> ent, EntityUid aggressor)
    {
        var (uid, comp) = ent;
        ent.Comp.Aggressors.Add(aggressor);

        var aggComp = EnsureComp<AggressorComponent>(aggressor);
        aggComp.Aggressives.Add(uid);

        var ev = new AggressorAddedEvent(aggressor);
        RaiseLocalEvent(uid, ref ev);
        var ev2 = new AggressiveAddedEvent(uid);
        RaiseLocalEvent(aggressor, ref ev2);

        Dirty(uid, comp);
        Dirty(aggressor, aggComp);
    }

    public void RemoveAggressor(Entity<AggressiveComponent> ent, Entity<AggressorComponent?> aggressor)
    {
        // Arcane-Edit-Start
        if (!ent.Comp.Aggressors.Remove(aggressor))
            return;
        // Arcane-Edit-End

        // Arcane-Start
        Dirty(ent.Owner, ent.Comp);
        var ev = new AggressorRemovedEvent(aggressor);
        RaiseLocalEvent(ent, ref ev);
        // Arcane-End
        RemoveAggressorFrom(ent, aggressor);
    }

    public void RemoveAllAggressors(Entity<AggressiveComponent> ent)
    {
        // Arcane-Edit-Start
        foreach (var aggressor in ent.Comp.Aggressors.ToArray())
            RemoveAggressor(ent, aggressor);
        // Arcane-Edit-End
    }

    private void RemoveAggressorFrom(Entity<AggressiveComponent> ent, Entity<AggressorComponent?> aggressor)
    {
        if (!Resolve(aggressor, ref aggressor.Comp, false)) // Arcane-Edit: ignore a target already removed
            return;

        aggressor.Comp.Aggressives.Remove(ent);
        if (aggressor.Comp.Aggressives.Count > 0)
        // Arcane-Start
        {
            Dirty(aggressor.Owner, aggressor.Comp);
            return;
        }
        // Arcane-End
        var ev2 = new AggressiveRemovedEvent(ent);
        RaiseLocalEvent(aggressor, ref ev2);
        RemComp(aggressor, aggressor.Comp);
    }

    #endregion

    #region Aggressor API

    public void CleanAggressions(Entity<AggressorComponent?> aggressor)
    {
        if (!Resolve(aggressor, ref aggressor.Comp))
            return;

        foreach (var aggressive in aggressor.Comp.Aggressives.ToArray()) // Arcane-Edit: removing a relation mutates the set
        {
            if (TryComp<AggressiveComponent>(aggressive, out var aggressors))
                RemoveAggressor((aggressive, aggressors), aggressor);
        }

        RemComp<AggressorComponent>(aggressor.Owner); // Arcane-Edit: the last relation can already remove this component
    }

    #endregion
}
