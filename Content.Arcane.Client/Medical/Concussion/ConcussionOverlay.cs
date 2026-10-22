using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.Medical.Concussion;

public sealed partial class ConcussionOverlay : Overlay
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerManager _player = default!;

    private static readonly ProtoId<ShaderPrototype> CircleMask = "GradientCircleMask";
    private readonly ShaderInstance _shader;
    public float TargetIntensity;
    public float SmoothedIntensity { get; private set; }
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ConcussionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(CircleMask).InstanceUnique();
        ZIndex = 16;
    }

    public void Reset()
    {
        TargetIntensity = 0;
        SmoothedIntensity = 0;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        var difference = TargetIntensity - SmoothedIntensity;
        SmoothedIntensity = Math.Abs(difference) < 0.001f
            ? TargetIntensity
            : SmoothedIntensity + difference * Math.Clamp(5f * args.DeltaSeconds, 0f, 1f);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (SmoothedIntensity <= 0 ||
            !_entities.TryGetComponent(_player.LocalEntity, out EyeComponent? eye) || args.Viewport.Eye != eye.Eye)
            return;

        var size = Math.Min(args.ViewportBounds.Width, args.ViewportBounds.Height);
        var level = SmoothedIntensity;
        var outerRadius = (0.9f - level * 0.3f) * size;
        var innerRadius = (0.6f - level * 0.45f) * size;
        _shader.SetParameter("color", Vector3.One);
        _shader.SetParameter("darknessAlphaOuter", level * 0.75f);
        _shader.SetParameter("outerCircleRadius", outerRadius);
        _shader.SetParameter("outerCircleMaxRadius", outerRadius);
        _shader.SetParameter("innerCircleRadius", innerRadius);
        _shader.SetParameter("innerCircleMaxRadius", innerRadius);
        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldAABB, Color.White);
        args.WorldHandle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
