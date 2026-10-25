using Content.Arcane.Common.CCVar;
using Robust.Client.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.IoC;

namespace Content.Arcane.Client.Lighting.Shaders;

public sealed partial class LightingHighlightsOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "LightingHighlights";
    private const int QualityNone = 0;
    private const int QualitySilly = 1;
    private const int QualityStrong = 2;

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private ILightManager _lights = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly ShaderInstance _shader;
    private float _lastIntensity = float.NaN;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => GetIntensity() > 0f;

    public LightingHighlightsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(Shader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (GetIntensity() <= 0f || !_lights.Enabled)
            return false;

        return _entities.TryGetComponent(_player.LocalEntity, out EyeComponent? eye) &&
            args.Viewport.Eye == eye.Eye &&
            eye.Eye.DrawLight;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is not { } screen)
            return;

        var intensity = GetIntensity();
        if (intensity <= 0f)
            return;

        if (_lastIntensity != intensity)
        {
            _shader.SetParameter("intensity", intensity);
            _lastIntensity = intensity;
        }

        _shader.SetParameter("SCREEN_TEXTURE", screen);
        _shader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    private float GetIntensity()
    {
        return _cfg.GetCVar(ArcaneCCVars.LightingShaderQuality) switch
        {
            QualitySilly => 0.06f,
            QualityStrong => 0.12f,
            _ => 0f,
        };
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
