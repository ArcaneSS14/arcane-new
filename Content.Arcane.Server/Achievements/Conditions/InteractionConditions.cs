using Content.Goobstation.Common.Chemistry;
using Content.Server.Power.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Events;
using Content.Shared.Disposal.Unit;
using Content.Shared.Destructible;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Strip;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class AirlockPryCondition : AchievementCondition
{
    [DataField]
    public bool RequireUnpowered = true;
}

public sealed partial class PickpocketCondition : AchievementCondition;
public sealed partial class BrainInMmiCondition : AchievementCondition;
public sealed partial class DisposalFlushedCondition : AchievementCondition;
public sealed partial class ReturnToSenderCondition : AchievementCondition;
public sealed partial class RazoriumPatientCondition : AchievementCondition;

public sealed partial class DestroyCondition : AchievementCondition
{
    [DataField(required: true)]
    public List<EntProtoId> Prototypes = new();
}

public sealed partial class AirlockPryConditionSystem : AchievementConditionSystem<AirlockPryCondition>
{
    [Dependency] private PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockComponent, DoorPryTransitionEvent>(OnPried);
    }

    private void OnPried(Entity<AirlockComponent> ent, ref DoorPryTransitionEvent args)
    {
        var powered = _power.IsPowered(ent);
        foreach (var (proto, condition) in Entries)
        {
            if (!condition.RequireUnpowered || !powered)
                Achievements.AddProgress(args.User, proto);
        }
    }
}

public sealed partial class PickpocketConditionSystem : AchievementConditionSystem<PickpocketCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, HandItemStrippedEvent>(OnStripped);
    }

    private void OnStripped(Entity<ActorComponent> ent, ref HandItemStrippedEvent args) => ProgressAll(ent);
}

public sealed partial class BrainInMmiConditionSystem : AchievementConditionSystem<BrainInMmiCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BrainComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
    }

    private void OnInserted(Entity<BrainComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!TryComp<MMIComponent>(args.Container.Owner, out var mmi) || args.Container.ID != mmi.BrainSlotId)
            return;

        if (Achievements.TryGetSession(ent, out _))
            ProgressAll(ent);
        else
            ProgressAll(args.Container.Owner);
    }
}

public sealed partial class DisposalFlushedConditionSystem : AchievementConditionSystem<DisposalFlushedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
    }

    private void OnInserted(Entity<ActorComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (HasComp<DisposalHolderComponent>(args.Container.Owner))
            ProgressAll(ent);
    }
}

public sealed partial class ReturnToSenderConditionSystem : AchievementConditionSystem<ReturnToSenderCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimerTriggerComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnThrowHit(Entity<TimerTriggerComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<ActiveTimerTriggerComponent>(ent)
            || ent.Comp.User is not { } armer
            || args.Target != armer
            || args.Component.Thrower is not { } thrower
            || thrower == armer)
            return;

        ProgressAll(thrower);
    }
}

public sealed partial class RazoriumPatientConditionSystem : AchievementConditionSystem<RazoriumPatientCondition>
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    private readonly Dictionary<EntityUid, (EntityUid Patient, EntityUid Medic, float Before)> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodstreamComponent, TargetBeforeInjectEvent>(OnBeforeInject);
        SubscribeLocalEvent<InjectorComponent, AfterInjectedEvent>(OnInjected);
        SubscribeLocalEvent<InjectorComponent, ComponentShutdown>(OnInjectorShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private float GetRazorium(Entity<BloodstreamComponent> patient)
    {
        return _solution.TryGetSolution(patient.Owner, patient.Comp.BloodSolutionName, out _, out var solution)
            ? solution.GetTotalPrototypeQuantity("Razorium").Float()
            : 0f;
    }

    private void OnBeforeInject(Entity<BloodstreamComponent> ent, ref TargetBeforeInjectEvent args)
    {
        _pending.Remove(args.UsedInjector);
        if (Entries.Count == 0 || args.Cancelled || !_mobState.IsCritical(ent))
            return;

        _pending[args.UsedInjector] = (ent, args.EntityUsingInjector, GetRazorium(ent));
    }

    private void OnInjected(Entity<InjectorComponent> ent, ref AfterInjectedEvent args)
    {
        if (!_pending.Remove(ent, out var pending)
            || args.User != pending.Medic
            || args.Target != pending.Patient
            || !TryComp<BloodstreamComponent>(pending.Patient, out var bloodstream)
            || GetRazorium((pending.Patient, bloodstream)) <= pending.Before)
            return;

        ProgressAll(pending.Medic);
    }

    private void OnInjectorShutdown(Entity<InjectorComponent> ent, ref ComponentShutdown args) => _pending.Remove(ent);

    private void OnRoundRestart(RoundRestartCleanupEvent args) => _pending.Clear();
}

public sealed partial class DestroyConditionSystem : AchievementConditionSystem<DestroyCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnDestroyed(Entity<MetaDataComponent> ent, ref DestructionEventArgs args)
    {
        if (args.Cause is not { } cause || Prototype(ent) is not { } prototype)
            return;

        var attacker = Achievements.ResolveAttacker(cause);
        foreach (var (achievement, condition) in Entries)
        {
            if (condition.Prototypes.Contains(prototype.ID))
                Achievements.AddProgress(attacker, achievement);
        }
    }
}
