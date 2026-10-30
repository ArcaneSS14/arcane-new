using System.Numerics;
using Content.Client.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Light;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.NamePeek;

public sealed class NamePeekOverlay : Overlay
{
    private const float MinimumLightLevel = 0.1f;
    private const float TextSpacing = 2f;
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private readonly NamePeekSystem _system;
    private readonly IEntityManager _entities;
    private readonly IPlayerManager _playerManager;
    private readonly EntityLookupSystem _lookup;
    private readonly ExamineSystem _examine;
    private readonly LightLevelSystem _lightLevel;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly IUserInterfaceManager _userInterface;
    private readonly Font _nameFont;
    private readonly Font _pronounFont;
    private readonly ShaderInstance _shader;
    private readonly HashSet<Entity<MobStateComponent>> _nearbyEntities = [];

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public NamePeekOverlay(
        NamePeekSystem system,
        IEntityManager entities,
        IPlayerManager playerManager,
        IUserInterfaceManager userInterface,
        IResourceCache resources,
        IPrototypeManager prototypes)
    {
        _system = system;
        _entities = entities;
        _playerManager = playerManager;
        _lookup = entities.System<EntityLookupSystem>();
        _examine = entities.System<ExamineSystem>();
        _lightLevel = entities.System<LightLevelSystem>();
        _transform = entities.System<SharedTransformSystem>();
        _sprite = entities.System<SpriteSystem>();
        _userInterface = userInterface;

        var font = resources.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf");
        _nameFont = new VectorFont(font, 12);
        _pronounFont = new VectorFont(font, 10);
        _shader = prototypes.Index(UnshadedShader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_system.Visible || args.ViewportControl == null || _playerManager.LocalEntity is not { } player)
            return false;

        return !_entities.TryGetComponent(player, out MobStateComponent? mobState)
               || mobState.CurrentState != MobState.Critical;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { } player || args.Viewport.Eye is not { } eye || args.ViewportControl is null)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        var uiScale = _userInterface.RootControl.UIScale;

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.DrawingHandle.UseShader(_shader);

        _nearbyEntities.Clear();
        _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds, _nearbyEntities, LookupFlags.Uncontained);

        foreach (var (uid, mobState) in _nearbyEntities)
        {
            if (uid == player || mobState.CurrentState != MobState.Alive)
                continue;

            if (!_entities.TryGetComponent(uid, out TransformComponent? transform)
                || !_entities.TryGetComponent(uid, out SpriteComponent? sprite)
                || !sprite.Visible
                || sprite.Color.A == 0)
            {
                continue;
            }

            var mapPosition = _transform.GetMapCoordinates(transform);
            if (mapPosition.MapId != args.MapId)
                continue;

            if (eye.DrawLight
                && eye.DrawFov
                && (!_lightLevel.TryCalculateLightLevel(mapPosition, out var lightLevel) || lightLevel <= MinimumLightLevel))
            {
                continue;
            }

            if (eye.DrawFov && !_examine.InRangeUnOccluded(player, uid))
                continue;

            var name = Identity.Name(uid, _entities, player);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var pronouns = GetPronouns(uid);
            var (worldPosition, worldRotation) = _transform.GetWorldPositionRotation(transform);
            var spriteBounds = _sprite.CalculateBounds((uid, sprite), worldPosition, worldRotation, eye.Rotation);
            var rotatedBounds = Matrix3Helpers.CreateRotation(eye.Rotation).TransformBox(spriteBounds);
            var top = new Vector2(rotatedBounds.Center.X, rotatedBounds.Top);
            var screenPosition = Vector2.Transform(top, matrix);

            DrawText(args.ScreenHandle, screenPosition, name, pronouns, uiScale);
        }

        args.DrawingHandle.UseShader(null);
    }

    private string GetPronouns(EntityUid uid)
    {
        if (!_entities.TryGetComponent(uid, out HumanoidProfileComponent? profile))
            return Loc.GetString("arcane-name-peek-pronouns-epicene");

        var key = profile.Gender switch
        {
            Gender.Male => "arcane-name-peek-pronouns-male",
            Gender.Female => "arcane-name-peek-pronouns-female",
            Gender.Neuter => "arcane-name-peek-pronouns-neuter",
            _ => "arcane-name-peek-pronouns-epicene"
        };

        return Loc.GetString(key);
    }

    private void DrawText(DrawingHandleScreen handle, Vector2 top, string name, string pronouns, float uiScale)
    {
        var nameSize = handle.GetDimensions(_nameFont, name, uiScale);
        var pronounSize = handle.GetDimensions(_pronounFont, pronouns, uiScale);
        var totalHeight = nameSize.Y + pronounSize.Y + TextSpacing * uiScale;
        var namePosition = new Vector2(
            MathF.Round(top.X - nameSize.X / 2f),
            MathF.Round(top.Y - totalHeight));
        var pronounPosition = new Vector2(
            MathF.Round(top.X - pronounSize.X / 2f),
            MathF.Round(namePosition.Y + nameSize.Y + TextSpacing * uiScale));

        handle.DrawString(_nameFont, namePosition, name, uiScale, Color.White, TextOutline.Default);
        handle.DrawString(_pronounFont, pronounPosition, pronouns, uiScale, Color.White, TextOutline.Default);
    }

    protected override void DisposeBehavior()
    {
        _nearbyEntities.Clear();
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
