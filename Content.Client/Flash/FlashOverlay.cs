using Content.Shared.CCVar;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics; // Arcane

namespace Content.Client.Flash;

public sealed partial class FlashOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> FlashedEffectShader = "ArcaneFlashedEffect"; // Arcane-Edit: Use a frozen afterimage with equal blindness in both accessibility modes.

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IClyde _clyde = default!; // Arcane

    private readonly StatusEffectsSystem _statusSys;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _shader;
    private bool _reducedMotion;
    public float PercentComplete;
    private IRenderTexture? _snapshot; // Arcane-Edit: Own the frozen frame instead of retaining the shared screen buffer.
    // Arcane-Start
    private TimeSpan? _effectEnd;
    private TimeSpan _visualStart;
    private float _elapsed;
    private float _duration = 1f;
    // Arcane-End

    public FlashOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(FlashedEffectShader).InstanceUnique();
        _statusSys = _entityManager.System<StatusEffectsSystem>();
        ZIndex = 15; // Arcane

        _configManager.OnValueChanged(CCVars.DisableFlashEffect, OnReducedMotionChanged, invokeImmediately: true); // Arcane-Edit: Unsubscribe when the overlay is disposed.
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (!_statusSys.HasEffectComp<FlashedStatusEffectComponent>(playerEntity))
            return;

        if (!_statusSys.TryGetTime(playerEntity.Value, SharedFlashSystem.FlashedKey, out var time))
            return;

        var curTime = _timing.CurTime;
        if (time.StartEffectTime == null || time.EndEffectTime == null)
            return;

        // Arcane-Start: Restart the visual phase on extension while preserving the frozen frame.
        if (_effectEnd == null)
            _visualStart = time.StartEffectTime.Value;
        else if (time.EndEffectTime > _effectEnd)
            _visualStart = curTime;
        _effectEnd = time.EndEffectTime;
        // Arcane-End
        // Arcane-Edit-Start: Calculate progress from the latest flash.
        _duration = Math.Max(0.001f, (float)(_effectEnd.Value - _visualStart).TotalSeconds);
        _elapsed = Math.Max(0f, (float)(curTime - _visualStart).TotalSeconds);
        PercentComplete = _elapsed / _duration;
        // Arcane-Edit-End
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;
        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return PercentComplete < 1.0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (RequestScreenTexture && ScreenTexture != null)
        {
            CaptureFrame(args, ScreenTexture); // Arcane-Edit: Copy the post-FOV frame before another overlay overwrites the shared buffer.
            RequestScreenTexture = false; // we only need the first frame, so we can stop the request now for performance reasons
        }
        // Arcane-Edit-Start: Keep both modes equally blinded; use white when no previously visible frame is available.
        var worldHandle = args.WorldHandle;
        _shader.SetParameter("elapsed", _elapsed);
        _shader.SetParameter("duration", _duration);
        _shader.SetParameter("reducedMotion", _reducedMotion);
        worldHandle.UseShader(_shader);
        if (_snapshot != null)
            worldHandle.DrawTextureRectRegion(_snapshot.Texture, args.WorldBounds);
        else
            worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
        // Arcane-Edit-End
    }

    // Arcane-Start
    private void CaptureFrame(in OverlayDrawArgs args, Texture source)
    {
        _snapshot?.Dispose();
        var snapshot = _clyde.CreateRenderTarget(source.Size,
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "flash-snapshot");
        _snapshot = snapshot;
        var handle = args.RenderHandle.DrawingHandleScreen;
        handle.RenderInRenderTarget(snapshot,
            () =>
            {
                handle.UseShader(null);
                handle.DrawTextureRect(source, UIBox2.FromDimensions(Vector2.Zero, snapshot.Size));
            }, Color.Black);
    }

    public void Start(bool captureFrame = true)
    {
        Reset();
        PercentComplete = 0f;
        RequestScreenTexture = captureFrame;
    }

    public void Reset()
    {
        PercentComplete = 1f;
        RequestScreenTexture = false;
        _effectEnd = null;
        _elapsed = 0;
        _duration = 1f;
        _snapshot?.Dispose();
        _snapshot = null;
    }

    private void OnReducedMotionChanged(bool enabled)
    {
        _reducedMotion = enabled;
    }
    // Arcane-End

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        // Arcane-Edit-Start: Release the owned frame, shader and configuration subscription.
        _configManager.UnsubValueChanged(CCVars.DisableFlashEffect, OnReducedMotionChanged);
        Reset();
        _shader.Dispose();
        // Arcane-Edit-End
    }
}
