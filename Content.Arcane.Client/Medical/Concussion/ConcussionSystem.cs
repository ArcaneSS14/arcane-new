using Content.Arcane.Shared.Medical.Concussion;
using Content.Shared.GameTicking;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Arcane.Client.Medical.Concussion;

public sealed partial class ConcussionSystem : SharedConcussionSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private AudioSystem _audio = default!;

    private ConcussionOverlay _overlay = default!;
    private EntityUid? _sound;
    private bool _hasOverlay;
    private bool _refreshRequired;

    public override void Initialize()
    {
        base.Initialize();
        InitializeHearing();
        _overlay = new ConcussionOverlay();
        SubscribeLocalEvent<ConcussionThresholdComponent, AfterAutoHandleStateEvent>(OnStateHandled);
        SubscribeLocalEvent<ConcussionThresholdComponent, ComponentShutdown>(OnThresholdShutdown);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (_refreshRequired)
        {
            _refreshRequired = false;
            Refresh();
        }
        if (_hasOverlay && _overlay.TargetIntensity == 0 && _overlay.SmoothedIntensity == 0)
        {
            _overlays.RemoveOverlay(_overlay);
            _hasOverlay = false;
        }
        UpdateUnoccludedAudio();
    }

    private void OnStateHandled(Entity<ConcussionThresholdComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            _refreshRequired = true;
    }

    private void Refresh()
    {
        if (!TryComp<ConcussionThresholdComponent>(_player.LocalEntity, out var concussion))
        {
            Reset();
            return;
        }
        var intensity = GetIntensity(concussion);
        UpdateHearing(intensity, concussion.HearingRecoveryThreshold);
        _overlay.TargetIntensity = intensity;
        if (_overlay.TargetIntensity > 0 && !_hasOverlay)
        {
            _overlays.AddOverlay(_overlay);
            _hasOverlay = true;
        }
        if (concussion.CurrentState == ConcussionState.Sane)
        {
            _sound = _audio.Stop(_sound);
        }
        else
        {
            var volume = -7f + 20f * MathF.Log10(Math.Max(intensity, 0.01f));
            if (!Exists(_sound))
                _sound = PlayTinnitus(concussion.Sound, volume);
            else
                _audio.SetVolume(_sound, volume);
        }
    }

    private void OnAttached(LocalPlayerAttachedEvent args)
    {
        Reset();
        _refreshRequired = true;
    }

    private void OnDetached(LocalPlayerDetachedEvent args)
    {
        Reset();
        _refreshRequired = true;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Reset();
    }

    private void OnThresholdShutdown(Entity<ConcussionThresholdComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            Reset();
    }

    private void Reset()
    {
        ResetHearing();
        _refreshRequired = false;
        _overlay.Reset();
        if (_hasOverlay)
            _overlays.RemoveOverlay(_overlay);
        _hasOverlay = false;
        _sound = _audio.Stop(_sound);
    }

    public override void Shutdown()
    {
        ShutdownHearing();
        Reset();
        _overlay.Dispose();
        base.Shutdown();
    }
}
