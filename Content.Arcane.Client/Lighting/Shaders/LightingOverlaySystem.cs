using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.Lighting.Shaders;

public sealed partial class LightingOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private LightingOverlay? _lightingOverlay;
    private LightingHighlightsOverlay? _lightingHighlightsOverlay;
    private GlowHaloOverlay? _glowHaloOverlay;

    public override void Initialize()
    {
        base.Initialize();

        _lightingHighlightsOverlay = new LightingHighlightsOverlay();
        _overlayManager.AddOverlay(_lightingHighlightsOverlay);

        _lightingOverlay = new LightingOverlay(EntityManager, _prototypeManager);
        _overlayManager.AddOverlay(_lightingOverlay);

        _glowHaloOverlay = new GlowHaloOverlay();
        _overlayManager.AddOverlay(_glowHaloOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_lightingHighlightsOverlay != null)
        {
            _overlayManager.RemoveOverlay(_lightingHighlightsOverlay);
            _lightingHighlightsOverlay.Dispose();
            _lightingHighlightsOverlay = null;
        }

        if (_lightingOverlay != null)
        {
            _overlayManager.RemoveOverlay(_lightingOverlay);
            _lightingOverlay.Dispose();
            _lightingOverlay = null;
        }

        if (_glowHaloOverlay != null)
        {
            _overlayManager.RemoveOverlay(_glowHaloOverlay);
            _glowHaloOverlay.Dispose();
            _glowHaloOverlay = null;
        }
    }
}
