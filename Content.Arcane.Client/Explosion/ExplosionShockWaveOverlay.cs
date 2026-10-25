using System.Numerics;
using Content.Shared.Explosion.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.Explosion;

public sealed partial class ExplosionShockWaveOverlay : Overlay
{
    private const int MaxCount = 10;
    private static readonly ProtoId<ShaderPrototype> ShockWaveShaderId = "ShockWave";

    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly ShaderInstance _shader;
    private readonly Vector2[] _positions = new Vector2[MaxCount];
    private readonly float[] _falloffPower = new float[MaxCount];
    private readonly float[] _sharpness = new float[MaxCount];
    private readonly float[] _width = new float[MaxCount];
    private SharedTransformSystem? _transform;
    private int _count;

    public ExplosionShockWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(ShockWaveShaderId).InstanceUnique();
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null || _transform is null && !_entities.TrySystem(out _transform))
            return false;

        var query = _entities.EntityQueryEnumerator<ExplosionShockWaveComponent, TransformComponent>();
        _count = 0;

        while (query.MoveNext(out var uid, out var shockWave, out var transform))
        {
            if (transform.MapID != args.MapId)
                continue;

            var position = _transform.GetWorldPosition(uid);
            var viewportPosition = args.Viewport.WorldToLocal(position);
            viewportPosition.Y = 1f - viewportPosition.Y / args.Viewport.Size.Y;
            viewportPosition.X /= args.Viewport.Size.X;

            _positions[_count] = viewportPosition;
            _falloffPower[_count] = shockWave.FalloffPower;
            _sharpness[_count] = shockWave.Sharpness;
            _width[_count] = shockWave.Width;
            _count++;

            if (_count == MaxCount)
                break;
        }

        return _count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        _shader.SetParameter("renderScale", args.Viewport.RenderScale * args.Viewport.Eye.Scale);
        _shader.SetParameter("count", _count);
        _shader.SetParameter("position", _positions);
        _shader.SetParameter("falloffPower", _falloffPower);
        _shader.SetParameter("sharpness", _sharpness);
        _shader.SetParameter("width", _width);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
