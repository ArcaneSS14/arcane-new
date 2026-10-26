using Content.Shared.CCVar;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;

// Arcane-Start
using Content.Shared.Physics;
using Robust.Shared.Graphics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
// Arcane-End

namespace Content.Client.Wall.Systems;

/// <summary>
/// Manages the directional visibility overlay for wall-mounted entities.
/// </summary>
public sealed partial class WallMountVisibilitySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<WallComponent> _wallQuery;

    private WallMountVisibilityOverlay _overlayInstance = default!;

    /// <summary>
    /// Whether directional visibility is currently enabled.
    /// </summary>
    public bool DirectionalVisibilityEnabled = true;

    /// <summary>
    /// Whether wall-mount visibility changes fade smoothly or snap instantly.
    /// </summary>
    public bool FadeEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _overlayInstance = new WallMountVisibilityOverlay();

        Subs.CVar(_cfg, CCVars.WallMountDirectionalVisibility, OnDirectionalVisibilityChanged, true);
        Subs.CVar(_cfg, CCVars.WallMountFade, OnFadeChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_overlayInstance);
    }

    private void OnDirectionalVisibilityChanged(bool enabled)
    {
        DirectionalVisibilityEnabled = enabled;

        if (enabled)
            _overlay.AddOverlay(_overlayInstance);
        else
        {
            _overlay.RemoveOverlay(_overlayInstance);
            _overlayInstance.RestoreAll();
        }
    }

    private void OnFadeChanged(bool enabled)
    {
        FadeEnabled = enabled;
    }

    /// <summary>
    /// Makes the entity visible again on component shutdown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWallMountShutdown(Entity<WallMountComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Makes the entity visible again if directional visibility is disabled for this mount.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWallMountAfterHandleState(Entity<WallMountComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.DirectionalVisibility)
            return;

        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Checks whether the tile contains any anchored blocking entity.
    /// </summary>
    public bool IsTileBlocked(Entity<MapGridComponent> grid, Vector2i tile)
    {
        var enumerator = _map.GetAnchoredEntities(grid.Owner, grid, tile);
        while (enumerator.MoveNext(out var anchored))
        {
            if (!_wallQuery.HasComp(anchored.Value))
                continue;

            return true;
        }
        return false;
    }

    // Arcane-Start
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public float GetVisibilityAlpha(
        WallMountComponent wallmount,
        TransformComponent xform,
        IEye eye,
        System.Numerics.Matrix3x2 matrix)
    {
        if (!DirectionalVisibilityEnabled || !wallmount.DirectionalVisibility)
            return 1f;

        var worldLightPosition = _xform.GetWorldPosition(xform);
        var eyeToLight = worldLightPosition - eye.Position.Position;
        var distance = eyeToLight.Length();
        var rayLength = distance - 0.05f;
        if (rayLength > 0f)
        {
            var ray = new CollisionRay(
                eye.Position.Position,
                eyeToLight / distance,
                (int) CollisionGroup.Impassable);

            foreach (var hit in _physics.IntersectRayWithPredicate(
                         xform.MapID,
                         ray,
                         maxLength: rayLength,
                         predicate: uid => !_wallQuery.HasComp(uid)))
            {
                return 0f;
            }
        }

        if (wallmount.Arc >= Math.Tau)
            return 1f;

        if (xform.GridUid is not { } gridUid || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return 1f;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var localPosition = xform.Coordinates.Position / grid.TileSize;
        var onVerticalBoundary = MathF.Abs(localPosition.X - MathF.Round(localPosition.X)) < 0.001f;
        var onHorizontalBoundary = MathF.Abs(localPosition.Y - MathF.Round(localPosition.Y)) < 0.001f;

        var blocked = IsTileBlocked((gridUid, grid), tile)
                      || onVerticalBoundary && IsTileBlocked((gridUid, grid), tile with { X = tile.X - 1 })
                      || onHorizontalBoundary && IsTileBlocked((gridUid, grid), tile with { Y = tile.Y - 1 })
                      || onVerticalBoundary && onHorizontalBoundary
                      && IsTileBlocked((gridUid, grid), tile with { X = tile.X - 1, Y = tile.Y - 1 });

        if (!blocked)
            return 1f;

        var (pos, rot) = _xform.GetWorldPositionRotation(xform);
        var facingAngle = rot + eye.Rotation + wallmount.Direction;

        var entityScreenPos = System.Numerics.Vector2.Transform(pos, matrix);
        var eyeScreenPos = System.Numerics.Vector2.Transform(eye.Position.Position, matrix);
        var toEntity = entityScreenPos - eyeScreenPos;
        var eyeToEntityAngle = (toEntity with { X = -toEntity.X }).ToWorldAngle();

        var angleDiff = Angle.ShortestDistance(eyeToEntityAngle, facingAngle);
        return Math.Abs(angleDiff) < wallmount.Arc / 2 ? 1f : 0f;
    }

    // Arcane-End
}
