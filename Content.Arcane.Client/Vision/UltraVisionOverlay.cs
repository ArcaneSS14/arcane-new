using System.Numerics;
using Content.Arcane.Shared.Vision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.Vision;

public sealed partial class UltraVisionOverlay : Overlay
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IEyeManager _eyes = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private static readonly ProtoId<ShaderPrototype> UltraVisionShader = "UltraVision";
    private readonly ShaderInstance _shader;

    public UltraVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(UltraVisionShader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye == _eyes.CurrentEye
            && _player.LocalEntity is { } player
            && _entities.HasComponent<UltraVisionComponent>(player)
            && base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
