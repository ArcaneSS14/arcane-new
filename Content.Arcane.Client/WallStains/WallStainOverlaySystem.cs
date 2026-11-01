namespace Content.Arcane.Client.WallStains;

public sealed partial class WallStainOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;

    private WallStainOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new WallStainOverlay();
        _overlays.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        _overlays.RemoveOverlay(_overlay);
        base.Shutdown();
    }
}
