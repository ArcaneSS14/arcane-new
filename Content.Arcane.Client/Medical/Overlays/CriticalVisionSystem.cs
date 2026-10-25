using Content.Arcane.Shared.Medical.Respiration;
using Content.Client.DamageOverlay;
using Content.Client.Eye.Blinding;
using Content.Shared.Bed.Sleep;
using Content.Shared.DamageOverlay;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameStates;
using Robust.Client.Player;

namespace Content.Arcane.Client.Medical.Overlays;

/// <summary>
/// Keeps critical-state vision above damage effects without treating medical unconsciousness as ordinary sleep.
/// </summary>
public sealed partial class CriticalVisionSystem : EntitySystem
{
    private const float WhiteStartFraction = 0.8f;
    private const float WhiteFullFraction = 0.95f;
    private const float SoftCritInitialDarkness = 0.4f;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IClientGameStateManager _states = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    private CriticalVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new CriticalVisionOverlay();
        _overlays.AddOverlay(_overlay);
        _states.GameStateApplied += OnStateApplied;
    }

    public override void Shutdown()
    {
        _states.GameStateApplied -= OnStateApplied;
        _overlays.RemoveOverlay(_overlay);
        _overlay.Dispose();
        if (_overlays.TryGetOverlay<DamageOverlay>(out var legacy))
        {
            legacy.DrawOxygen = true;
            legacy.DrawCritical = true;
        }
        if (_overlays.TryGetOverlay<BlindOverlay>(out var blind))
            blind.ApplyGreyscale = true;
        base.Shutdown();
    }

    private void OnStateApplied(GameStateAppliedArgs args)
    {
        _overlay.Patient = _player.LocalEntity;
        if (_overlays.TryGetOverlay<DamageOverlay>(out var legacy))
        {
            legacy.DrawOxygen = false;
            legacy.DrawCritical = false;
        }

        if (_overlays.TryGetOverlay<BlindOverlay>(out var blind))
            blind.ApplyGreyscale = !MedicalSleepIsOnlyBlindness(_player.LocalEntity);

        _overlay.Darkness = 0f;
        _overlay.WhiteLevel = 0f;
        if (_player.LocalEntity is not { } patient ||
            !TryComp<DamageOverlayComponent>(patient, out var damage) ||
            damage.CurrentState is not (MobState.SoftCrit or MobState.Critical) ||
            !_thresholds.TryGetThresholdForState(patient, MobState.Critical, out var hard) ||
            !_thresholds.TryGetDeadThreshold(patient, out var dead))
            return;

        var soft = _thresholds.TryGetThresholdForState(patient, MobState.SoftCrit, out var threshold)
            ? threshold.Value.Float() : hard.Value.Float();
        var hardDamage = hard.Value.Float();
        var deadDamage = dead.Value.Float();
        var start = soft + WhiteStartFraction * (hardDamage - soft);
        var maximum = soft + WhiteFullFraction * (deadDamage - soft);
        if (maximum <= start)
            return;

        // CritLevel already carries authoritative vital damage relative to the death threshold.
        _overlay.WhiteLevel = Math.Clamp((damage.CritLevel * deadDamage - start) / (maximum - start), 0f, 1f);
        _overlay.Darkness = damage.CurrentState == MobState.Critical || hardDamage <= soft
            ? 1f : SoftCritInitialDarkness + (1f - SoftCritInitialDarkness) *
                Math.Clamp((damage.CritLevel * deadDamage - soft) / (hardDamage - soft), 0f, 1f);
        if (damage.CurrentState == MobState.Critical && TryComp<BlindableComponent>(patient, out var eyes) && eyes.IsBlind)
            _overlay.Darkness = 0f;
    }

    private bool MedicalSleepIsOnlyBlindness(EntityUid? patient)
    {
        if (patient is not { } uid ||
            !TryComp<MedicalSleepOwnershipComponent>(uid, out var ownership) || !ownership.OwnsSleep ||
            !TryComp<BlindableComponent>(uid, out var eyes) || eyes.EyeDamage >= eyes.MaxDamage)
            return false;

        if (_status.TryEffectsWithComp<ForcedSleepingStatusEffectComponent>(patient, out var effects))
        {
            foreach (var effect in effects)
            {
                if (!HasComp<MedicalSleepStatusEffectComponent>(effect))
                    return false;
            }
        }

        var attempt = new CanSeeAttemptEvent { IgnoreSleeping = true };
        RaiseLocalEvent(uid, attempt);
        return !attempt.Blind;
    }
}
