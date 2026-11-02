using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.PainFlash;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.PainFlash;

public sealed partial class PainFlashSystem : SharedPainFlashSystem
{
    private const int PredictionRetentionTicks = 5000;

    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;

    private readonly List<PainFlashInstance> _painInstances = new();
    private PainFlashOverlay _overlay = default!;
    private bool _painFlashEnabled = true;
    private float _painFlashIntensity = 1f;

    private bool IsPainFlashVisible => _painFlashEnabled && _painFlashIntensity > 0f;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new PainFlashOverlay();

        SubscribeLocalEvent<PainFlashComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PainFlashComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PainFlashComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PainFlashComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<PainFlashMessage>(OnPainFlashMessage);

        _config.OnValueChanged(ArcaneCVars.PainFlashEnabled, OnPainFlashEnabledChanged, invokeImmediately: true);
        _config.OnValueChanged(ArcaneCVars.PainFlashIntensity, OnPainFlashIntensityChanged, invokeImmediately: true);
    }

    public override void Shutdown()
    {
        _config.UnsubValueChanged(ArcaneCVars.PainFlashEnabled, OnPainFlashEnabledChanged);
        _config.UnsubValueChanged(ArcaneCVars.PainFlashIntensity, OnPainFlashIntensityChanged);
        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Reset();
        _painInstances.Clear();

        base.Shutdown();
    }

    private void OnPainFlashEnabledChanged(bool enabled)
    {
        _painFlashEnabled = enabled;
        UpdateOverlayIntensity();
    }

    private void OnPainFlashIntensityChanged(float intensity)
    {
        _painFlashIntensity = float.IsFinite(intensity)
            ? Math.Clamp(intensity, 0f, 1f)
            : 1f;
        UpdateOverlayIntensity();
    }

    private void UpdateOverlayIntensity()
    {
        _overlay.Intensity = IsPainFlashVisible ? _painFlashIntensity : 0f;

        if (!IsPainFlashVisible)
            _overlay.Reset();
    }

    private void OnPlayerAttached(Entity<PainFlashComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        EnableOverlay();
    }

    private void OnPlayerDetached(Entity<PainFlashComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        DisableOverlay();
    }

    private void OnInit(Entity<PainFlashComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent.Owner)
            EnableOverlay();
    }

    private void OnShutdown(Entity<PainFlashComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            DisableOverlay();
    }

    private void EnableOverlay()
    {
        _overlayManager.AddOverlay(_overlay);
        _overlay.Reset();
    }

    private void DisableOverlay()
    {
        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Reset();
        _painInstances.Clear();
    }

    private void OnPainFlashMessage(PainFlashMessage message)
    {
        if (!IsPainFlashVisible)
            return;

        if (_painInstances.Remove(new PainFlashInstance(message.Damage, message.Tick)))
            return;

        _overlay.AddPain(message.Damage);
    }

    protected override void OnDamageChanged(Entity<PainFlashComponent> ent, ref DamageChangedEvent args)
    {
        if (!IsPainFlashVisible || _player.LocalEntity != ent.Owner)
            return;

        if (Timing.ApplyingState || !Timing.IsFirstTimePredicted)
            return;

        if (!TryGetPain(args, out var damage))
            return;

        _painInstances.Add(new PainFlashInstance(damage, Timing.CurTick));
        _overlay.AddPain(damage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_painInstances.Count == 0)
            return;

        _painInstances.RemoveAll(instance =>
            (int) Timing.CurTick.Value - (int) instance.Tick.Value > PredictionRetentionTicks);
    }
}

public readonly record struct PainFlashInstance(FixedPoint2 Damage, GameTick Tick);
