using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiOverlay
{
    private static readonly ProtoId<ShaderPrototype> MachineViewShader = "ArcaneAiMachineView";
    private readonly HashSet<Entity<StationAiWhitelistComponent>> _machineViewCandidates = new();

    private void DrawMachineView(in OverlayDrawArgs args, DrawingHandleWorld worldHandle, EntityUid gridUid, MapGridComponent grid)
    {
        if (args.Viewport.Eye is not { } eye)
            return;

        var lookup = _entManager.System<EntityLookupSystem>();
        var ai = _entManager.System<SharedStationAiSystem>();
        var sprites = _entManager.System<SpriteSystem>();
        var xforms = _entManager.System<SharedTransformSystem>();
        var maps = _entManager.System<SharedMapSystem>();

        _machineViewCandidates.Clear();
        lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds, _machineViewCandidates,
            LookupFlags.Static | LookupFlags.Sundries | LookupFlags.Approximate);
        var shader = _proto.Index(MachineViewShader).Instance();

        foreach (var candidate in _machineViewCandidates)
        {
            if (!_entManager.TryGetComponent(candidate, out TransformComponent? transform) ||
                transform.GridUid != gridUid || !ai.IsMachineVisible(candidate) ||
                _visibleTiles.Contains(maps.LocalToTile(gridUid, grid, transform.Coordinates)) ||
                !_entManager.TryGetComponent<SpriteComponent>(candidate, out var sprite) || !sprite.Visible)
                continue;

            var (position, rotation) = xforms.GetWorldPositionRotation(transform);
            worldHandle.UseShader(shader);
            sprites.RenderSprite((candidate.Owner, sprite), worldHandle, eye.Rotation, rotation, position);
        }
    }
}
