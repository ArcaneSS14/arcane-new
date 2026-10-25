using System.Numerics;
using Content.Arcane.Shared.WallStains.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.WallStains;

public sealed partial class WallStainOverlay : Overlay
{
    private const float PixelsPerMeter = 2f * EyeManager.PixelsPerMeter;
    private const int MinSplats = 1;
    private const int MaxSplats = 5;
    private const float SplatSpread = 0.5f;
    private static readonly ResPath StainPath = new("/Textures/_Arcane/Effects/wallstain.rsi");

    [Dependency] private IEntityManager _entities = default!;
    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprites;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private Texture? _texture;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public WallStainOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entities.System<TransformSystem>();
        _sprites = _entities.System<SpriteSystem>();
        _spriteQuery = _entities.GetEntityQuery<SpriteComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        _texture ??= _sprites.GetFrame(new SpriteSpecifier.Rsi(StainPath, "splatter"), TimeSpan.Zero);
        if (_texture == null)
            return;

        var draw = args.WorldHandle;
        var halfWidth = _texture.Width / PixelsPerMeter;
        var halfHeight = _texture.Height / PixelsPerMeter;
        var rect = new Box2(-halfWidth, -halfHeight, halfWidth, halfHeight);

        var query = _entities.EntityQueryEnumerator<WallStainComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stain, out var stainTransform))
        {
            var position = _transform.GetWorldPosition(stainTransform);
            if (stainTransform.MapID != args.MapId ||
                !_entities.TryGetComponent<StainedWallComponent>(stainTransform.ParentUid, out _) ||
                !IsVisibleSurface(stainTransform.ParentUid) ||
                !args.WorldBounds.Contains(position))
            {
                continue;
            }

            var (_, rotation) = _transform.GetWorldPositionRotation(stainTransform);
            var count = Math.Clamp(MinSplats + (int) MathF.Round(stain.FillLevel * (MaxSplats - MinSplats)), MinSplats, MaxSplats);
            var random = new Random(stain.SplatSeed);

            for (var i = 0; i < count; i++)
            {
                var offset = new Vector2(
                    ((float) random.NextDouble() - 0.5f) * SplatSpread,
                    ((float) random.NextDouble() - 0.5f) * SplatSpread);
                var splatPosition = position + offset;
                var splatRotation = rotation + GetDirectionAngle(stain.Direction);
                draw.SetTransform(Matrix3x2.CreateRotation((float) splatRotation.Theta) * Matrix3x2.CreateTranslation(splatPosition));
                draw.DrawTextureRect(_texture, rect, stain.Color);
            }
        }

        draw.SetTransform(Matrix3x2.Identity);
    }

    private bool IsVisibleSurface(EntityUid? parent)
    {
        if (parent is not { } uid || !_spriteQuery.TryGetComponent(uid, out var sprite))
            return false;

        return sprite.Visible;
    }

    private static Angle GetDirectionAngle(Vector2i direction)
    {
        if (direction == Vector2i.Zero)
            return Angle.Zero;

        return new Angle(MathF.Atan2(direction.Y, direction.X));
    }

    protected override void DisposeBehavior()
    {
        _texture = null;
        base.DisposeBehavior();
    }
}
