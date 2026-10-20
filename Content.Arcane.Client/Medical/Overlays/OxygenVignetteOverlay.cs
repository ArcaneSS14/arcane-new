using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Arcane.Client.Medical.Overlays;

/// <summary>
/// Transparent black vignette drawn over the world without capturing the screen or covering the HUD.
/// </summary>
public sealed partial class OxygenVignetteOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "OxygenVignette";
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public float Intensity { get; set; }

    private readonly ShaderInstance _shader;
    private float _lastIntensity = float.NaN;

    public OxygenVignetteOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(Shader).InstanceUnique();
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
            _lastIntensity = Intensity;
        }

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
