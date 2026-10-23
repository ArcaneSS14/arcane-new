using Content.Arcane.Shared.Medical.Respiration;
using Robust.Client.GameStates;

namespace Content.Arcane.Client.Medical.Overlays;

public sealed partial class OxygenVignetteSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IClientGameStateManager _states = default!;
    [Dependency] private OxygenThresholdSystem _thresholds = default!;

    private OxygenVignetteOverlay _overlay = default!;
    private EntityUid? _patient;
    private float _oxygenDamage;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new OxygenVignetteOverlay();
        SubscribeLocalEvent<LocalPlayerMedicalDamageEvent>(OnDamage);
        _states.GameStateApplied += OnStateApplied;
    }

    public override void Shutdown()
    {
        _states.GameStateApplied -= OnStateApplied;
        _overlays.RemoveOverlay(_overlay);
        _overlay.Dispose();
        base.Shutdown();
    }

    private void OnDamage(ref LocalPlayerMedicalDamageEvent args)
    {
        _patient = args.Player;
        _oxygenDamage = args.Damage.DamageDict.GetValueOrDefault("Asphyxiation").Float();
        Refresh();
    }

    private void OnStateApplied(GameStateAppliedArgs args)
    {
        // Thresholds and breathing immunity can change without a damage update.
        if (_oxygenDamage > 0f)
            Refresh();
    }

    private void Refresh()
    {
        var intensity = 0f;
        if (_oxygenDamage > 0f && _patient is { } patient &&
            TryComp<CPRRespirationComponent>(patient, out var respiration) &&
            respiration.LifeStage < ComponentLifeStage.Stopping && !respiration.BreathingImmune &&
            _thresholds.GetThreshold(patient) is { } threshold)
            intensity = Math.Clamp(_oxygenDamage / threshold, 0f, 1f);

        if (_overlay.Intensity == intensity)
            return;

        _overlay.Intensity = intensity;
        if (intensity > 0f)
            _overlays.AddOverlay(_overlay);
        else
            _overlays.RemoveOverlay(_overlay);
    }
}
