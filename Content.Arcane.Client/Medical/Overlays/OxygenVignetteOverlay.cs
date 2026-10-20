using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Arcane.Client.Medical.Overlays;

/// <summary>
/// Oxygen vignette with optional chromatic fringes over the world, leaving the HUD unaffected.
/// </summary>
public sealed partial class OxygenVignetteOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "OxygenVignette";
    private const float ChromaticStart = 0.6f;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => Intensity > ChromaticStart;
    public float Intensity { get; set; }

    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _chromaticShader;
    private float _lastIntensity = float.NaN;

    public OxygenVignetteOverlay()
    {
        IoCManager.InjectDependencies(this);
        var prototype = _prototypes.Index(Shader);
        _shader = prototype.InstanceUnique();
        _shader.SetParameter("chromaticEnabled", false);
        _chromaticShader = prototype.InstanceUnique();
        _chromaticShader.SetParameter("chromaticEnabled", true);
        _chromaticShader.SetParameter("chromaticStart", ChromaticStart);
        ZIndex = 12;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
        => Intensity > 0f && _entities.TryGetComponent(_player.LocalEntity, out EyeComponent? eye) &&
            args.Viewport.Eye == eye.Eye;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_lastIntensity != Intensity)
        {
            _shader.SetParameter("intensity", Intensity);
            _chromaticShader.SetParameter("intensity", Intensity);
            _lastIntensity = Intensity;
        }

        var shader = _shader;
        if (RequestScreenTexture && ScreenTexture is { } screen)
        {
            shader = _chromaticShader;
            shader.SetParameter("SCREEN_TEXTURE", screen);
        }

        var handle = args.WorldHandle;
        handle.UseShader(shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        _chromaticShader.Dispose();
        base.DisposeBehavior();
    }
}
