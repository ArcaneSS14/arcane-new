using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class BlackAndWhiteOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "GreyscaleFullscreen";

    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _greyscaleShader;

    // Arcane-Start
    private float _lastIntensity = float.NaN;

    /// <summary>
    /// Desaturation strength, from zero to one. Standalone overlays retain full greyscale by default.
    /// </summary>
    public float Intensity { get; set; } = 1f;
    // Arcane-End

    public BlackAndWhiteOverlay()
    {
        IoCManager.InjectDependencies(this);
        _greyscaleShader = _prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = 10; // draw this over the DamageOverlay, RainbowOverlay etc.
    }

    // Arcane-Start
    protected override void DisposeBehavior()
    {
        _greyscaleShader.Dispose();
        base.DisposeBehavior();
    }
    // Arcane-End

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        // Arcane-Start
        if (_lastIntensity != Intensity)
        {
            _greyscaleShader.SetParameter("intensity", Intensity);
            _lastIntensity = Intensity;
        }
        // Arcane-End
        _greyscaleShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_greyscaleShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
