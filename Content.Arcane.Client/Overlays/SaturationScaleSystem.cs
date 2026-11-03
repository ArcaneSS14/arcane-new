using Content.Arcane.Common.CCVar;
using Content.Shared.GameTicking;
using Content.Arcane.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Arcane.Client.Overlays;

public sealed partial class SaturationScaleSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private ISharedPlayerManager _playerMan = default!;
    [Dependency] private IConfigurationManager _cfgMan = default!;

    private SaturationScaleOverlay _overlay = default!;
    private bool _moodEffectsEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
        _moodEffectsEnabled = _cfgMan.GetCVar(ArcaneCVars.MoodVisualEffects);
        _cfgMan.OnValueChanged(ArcaneCVars.MoodVisualEffects, HandleMoodEffectsUpdated);

        SubscribeLocalEvent<SaturationScaleOverlayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SaturationScaleOverlayComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<SaturationScaleOverlayComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SaturationScaleOverlayComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);
    }

    public override void Shutdown()
    {
        _cfgMan.UnsubValueChanged(ArcaneCVars.MoodVisualEffects, HandleMoodEffectsUpdated);

        base.Shutdown();
    }

    private void HandleMoodEffectsUpdated(bool moodEffectsEnabled)
    {
        _moodEffectsEnabled = moodEffectsEnabled;

        if (!moodEffectsEnabled)
        {
            if (_overlayMan.HasOverlay<SaturationScaleOverlay>())
                _overlayMan.RemoveOverlay(_overlay);
            return;
        }

        // Turned back on: the local entity may already have the overlay component, no init or attach will follow.
        if (HasComp<SaturationScaleOverlayComponent>(_playerMan.LocalEntity) && !_overlayMan.HasOverlay<SaturationScaleOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        if (!_moodEffectsEnabled)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, SaturationScaleOverlayComponent component, PlayerDetachedEvent args)
    {
        if (!_moodEffectsEnabled)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, SaturationScaleOverlayComponent component, PlayerAttachedEvent args)
    {
        if (!_moodEffectsEnabled)
            return;

        if (!_overlayMan.HasOverlay<SaturationScaleOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, SaturationScaleOverlayComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity || !_moodEffectsEnabled)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(EntityUid uid, SaturationScaleOverlayComponent component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity || !_moodEffectsEnabled)
            return;

        if (!_overlayMan.HasOverlay<SaturationScaleOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }
}
