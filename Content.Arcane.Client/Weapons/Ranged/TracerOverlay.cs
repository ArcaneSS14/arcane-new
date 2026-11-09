using Robust.Shared.Enums;

namespace Content.Arcane.Client.Weapons.Ranged;

public sealed class TracerOverlay : Overlay
{
    private readonly TracerSystem _tracer;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public TracerOverlay(TracerSystem tracer)
    {
        _tracer = tracer;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _tracer.Draw(args.WorldHandle, args.MapId);
    }
}
