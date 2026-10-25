using Content.Shared.Interaction;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Arcane.Client.Interaction;

public sealed partial class InteractionParticleSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private TransformSystem _transform = default!;

    private const string AnimationKey = "arcane-interaction-particle";

    private static readonly Dictionary<ArcaneInteractionParticleType, EntProtoId> ParticlePrototypes = new()
    {
        { ArcaneInteractionParticleType.Use, "ArcaneInteractionParticleUse" },
        { ArcaneInteractionParticleType.Pull, "ArcaneInteractionParticlePull" },
        { ArcaneInteractionParticleType.InHand, "ArcaneInteractionParticleUse" },
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<ArcaneInteractionParticleEvent>(OnInteractionParticle);
    }

    private void OnInteractionParticle(ArcaneInteractionParticleEvent ev)
    {
        var performer = GetEntity(ev.Performer);
        var used = GetEntity(ev.Used);
        var target = GetEntity(ev.Target);
        if (!Exists(performer) || !Exists(target))
            return;

        var type = ev.Type;
        if (type == ArcaneInteractionParticleType.Pull)
            (performer, target) = (target, performer);

        var performerTransform = Transform(performer);
        var targetTransform = Transform(target);
        if (performerTransform.MapID == MapId.Nullspace || targetTransform.MapID == MapId.Nullspace)
            return;

        if (performerTransform.ParentUid != targetTransform.ParentUid)
        {
            if (type == ArcaneInteractionParticleType.Pull)
                return;

            type = ArcaneInteractionParticleType.InHand;
        }

        var particle = Spawn(ParticlePrototypes[type], performerTransform.Coordinates);
        var inHand = type == ArcaneInteractionParticleType.InHand;
        if (inHand)
        {
            used = target;
            _transform.SetParent(particle, performer);
        }

        if (used is { } usedEntity && Exists(usedEntity) && TryComp<SpriteComponent>(usedEntity, out var usedSprite))
        {
            _appearance.OnChangeData(usedEntity, usedSprite);
            _sprite.CopySprite((usedEntity, usedSprite), particle);
            _sprite.SetDrawDepth(particle, (int) global::Content.Shared.DrawDepth.DrawDepth.Effects);
        }

        var sprite = Comp<SpriteComponent>(particle);
        sprite.NoRotation = true;
        var delta = targetTransform.LocalPosition - performerTransform.LocalPosition;
        _animation.Play(particle,
            type == ArcaneInteractionParticleType.Pull
                ? GetPullAnimation(performerTransform.LocalPosition, performerTransform.LocalPosition + delta, sprite.Color)
                : GetUseAnimation(inHand ? Vector2.Zero : performerTransform.LocalPosition,
                    inHand ? new Vector2(0, 0.85f) : performerTransform.LocalPosition + delta,
                    sprite.Color,
                    inHand),
            AnimationKey);
    }

    private Animation GetUseAnimation(Vector2 start, Vector2 end, Color color, bool spriteOffset)
    {
        var rotationLength = TimeSpan.FromMilliseconds(600);
        var offsetLength = TimeSpan.FromMilliseconds(250);
        var startColor = color.WithAlpha(color.A * 0.7f);
        var positionTrack = new AnimationTrackComponentProperty
        {
            ComponentType = spriteOffset ? typeof(SpriteComponent) : typeof(TransformComponent),
            Property = spriteOffset ? nameof(SpriteComponent.Offset) : nameof(TransformComponent.LocalPosition),
            KeyFrames = { new AnimationTrackProperty.KeyFrame(start, 0f), new AnimationTrackProperty.KeyFrame(end, (float) offsetLength.TotalSeconds, Easings.OutBack) },
        };

        return new Animation
        {
            Length = rotationLength + offsetLength,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent), Property = nameof(SpriteComponent.Rotation),
                    KeyFrames = { new AnimationTrackProperty.KeyFrame(_random.NextAngle(Angle.FromDegrees(-40), Angle.FromDegrees(40)), 0f), new AnimationTrackProperty.KeyFrame(Angle.Zero, (float) rotationLength.TotalSeconds, Easings.OutBack) },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent), Property = nameof(SpriteComponent.Scale),
                    KeyFrames = { new AnimationTrackProperty.KeyFrame(new Vector2(0.3f), 0f), new AnimationTrackProperty.KeyFrame(Vector2.One, (float) rotationLength.TotalSeconds, Easings.OutBack) },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent), Property = nameof(SpriteComponent.Color),
                    KeyFrames = { new AnimationTrackProperty.KeyFrame(startColor, 0f), new AnimationTrackProperty.KeyFrame(startColor, (float) rotationLength.TotalSeconds), new AnimationTrackProperty.KeyFrame(color.WithAlpha(0f), (float) (rotationLength + offsetLength).TotalSeconds, Easings.InOutCirc) },
                },
                positionTrack,
            },
        };
    }

    private static Animation GetPullAnimation(Vector2 start, Vector2 end, Color color)
    {
        var length = TimeSpan.FromMilliseconds(8f * (1000f / 12f));
        var visibleLength = TimeSpan.FromMilliseconds(4f * (1000f / 12f));
        return new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(TransformComponent), Property = nameof(TransformComponent.LocalPosition),
                    KeyFrames = { new AnimationTrackProperty.KeyFrame(start, 0f), new AnimationTrackProperty.KeyFrame(end, (float) length.TotalSeconds, Easings.InOutCirc) },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent), Property = nameof(SpriteComponent.Color),
                    KeyFrames = { new AnimationTrackProperty.KeyFrame(color, 0f), new AnimationTrackProperty.KeyFrame(color, (float) visibleLength.TotalSeconds), new AnimationTrackProperty.KeyFrame(color.WithAlpha(0f), (float) length.TotalSeconds, Easings.InOutCirc) },
                },
            },
        };
    }
}
