using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Shared.Medical.Heart;
using Content.Arcane.Shared.Medical.Respiration;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Supplies physiology-specific CPR outcomes while the inherited system owns the interaction and session.
/// </summary>
public sealed partial class CPRRecoverySystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private HeartRateSystem _heart = default!;
    [Dependency] private OxygenThresholdSystem _oxygen = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageableComponent, CPRCompletedEvent>(OnCompleted);
        SubscribeLocalEvent<MobStateComponent, CPRRevivedEvent>(OnRevived);
        SubscribeLocalEvent<CPRPerfusionComponent, CPRStoppedEvent>(OnStopped);
    }

    private void OnCompleted(Entity<DamageableComponent> ent, ref CPRCompletedEvent args)
    {
        if (args.CanVentilate)
        {
            _damage.ChangeDamage(ent.Owner, new DamageSpecifier { DamageDict = { ["Asphyxiation"] = -3 } },
                targetPart: TargetBodyPart.Vital, interruptsDoAfters: false, ignoreResistances: true);
        }

        if (_heart.GetHeart(ent) != null)
            EnsureComp<CPRPerfusionComponent>(ent);

        var threshold = _oxygen.GetThreshold(ent);
        var oxygenDamage = _damage.GetAllDamage(ent.Owner).DamageDict.GetValueOrDefault("Asphyxiation").Float();
        args.ReviveChance = threshold is { } limit ? Math.Clamp(1f - oxygenDamage / limit, 0f, 1f) : 0f;
        args.CanRevive = TryComp<PerishableComponent>(ent, out var perishable) &&
            !_rotting.IsRotten(ent) && _rotting.PerishStage((ent.Owner, perishable), SharedRottingSystem.MaxStages) <= 1 &&
            (!_heart.RequiresHeart(ent) || _heart.GetHeart(ent) != null);
    }

    private void OnRevived(Entity<MobStateComponent> ent, ref CPRRevivedEvent args)
    {
        if (_heart.GetHeart(ent) is { Comp: { } heart } installed && _heart.GetState(heart) == HeartState.Stopped)
            _heart.SetRate(installed.Owner, heart, heart.FibrillationCaps.X, true);
    }

    private void OnStopped(Entity<CPRPerfusionComponent> ent, ref CPRStoppedEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            RemComp<CPRPerfusionComponent>(ent);
    }
}
