using Content.Arcane.Shared.Medical.Respiration;
using Content.Goobstation.Common.Body.Components;
using Content.Medical.Common.Body;
using Content.Server.Body.Components;
using Content.Shared.Bed.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Coordinates oxygen-related unconsciousness and postmortem damage when the body's condition changes.
/// </summary>
public sealed partial class OxygenKnockoutSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private OxygenThresholdSystem _threshold = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public static readonly EntProtoId OxygenSleep = "StatusEffectOxygenKnockout";

    public override void Initialize()
    {
        SubscribeLocalEvent<RespiratorComponent, MobThresholdChecked>(OnThresholds);
        SubscribeLocalEvent<RespiratorComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<RespiratorComponent, ComponentStartup>(OnRespiratorStartup);
        SubscribeLocalEvent<RespiratorComponent, ComponentShutdown>(OnRespiratorShutdown);
        SubscribeLocalEvent<BreathingImmunityComponent, ComponentStartup>(OnImmunityStartup);
        SubscribeLocalEvent<BreathingImmunityComponent, ComponentShutdown>(OnImmunityShutdown);
        SubscribeLocalEvent<SpecialBreathingImmunityComponent, ComponentStartup>(OnSpecialStartup);
        SubscribeLocalEvent<SpecialBreathingImmunityComponent, ComponentShutdown>(OnSpecialShutdown);
        SubscribeLocalEvent<StasisBedBuckledComponent, ComponentStartup>(OnStasisStarted);
        SubscribeLocalEvent<StasisBedBuckledComponent, ComponentShutdown>(OnStasisStopped);
    }

    private void OnThresholds(Entity<RespiratorComponent> ent, ref MobThresholdChecked args) => UpdatePatient(ent);
    private void OnStateChanged(Entity<RespiratorComponent> ent, ref MobStateChangedEvent args) => UpdatePatient(ent);
    private void OnRespiratorStartup(Entity<RespiratorComponent> ent, ref ComponentStartup args) => UpdatePatient(ent);
    private void OnRespiratorShutdown(Entity<RespiratorComponent> ent, ref ComponentShutdown args) => UpdatePatient(ent);
    private void OnImmunityStartup(Entity<BreathingImmunityComponent> ent, ref ComponentStartup args) => UpdatePatient(ent);
    private void OnImmunityShutdown(Entity<BreathingImmunityComponent> ent, ref ComponentShutdown args) => UpdatePatient(ent);
    private void OnSpecialStartup(Entity<SpecialBreathingImmunityComponent> ent, ref ComponentStartup args) => UpdatePatient(ent);
    private void OnSpecialShutdown(Entity<SpecialBreathingImmunityComponent> ent, ref ComponentShutdown args) => UpdatePatient(ent);

    private bool Active<T>(EntityUid patient) where T : Component
        => TryComp<T>(patient, out var component) && component.LifeStage < ComponentLifeStage.Stopping;

    private void UpdatePatient(EntityUid patient)
    {
        if (TerminatingOrDeleted(patient))
            return;

        RefreshPostmortem(patient);

        var needsSleep = TryComp<MobStateComponent>(patient, out var mob) && mob.CurrentState != MobState.Dead &&
            Active<RespiratorComponent>(patient) && !Active<BreathingImmunityComponent>(patient) &&
            !Active<SpecialBreathingImmunityComponent>(patient) && HasComp<DamageableComponent>(patient) &&
            _threshold.GetThreshold(patient) is { } limit &&
            _damage.GetAllDamage(patient).DamageDict.GetValueOrDefault(Asphyxiation).Float() >= limit;

        if (needsSleep)
        {
            if (!_status.TryGetStatusEffect(patient, OxygenSleep, out _))
                _status.TrySetStatusEffectDuration(patient, OxygenSleep);
        }
        else
            RemoveOxygenSleep(patient);
    }

    private void RemoveOxygenSleep(EntityUid patient)
    {
        if (!TerminatingOrDeleted(patient) && _status.TryGetStatusEffect(patient, OxygenSleep, out var effect))
            Del(effect.Value);
    }
}
