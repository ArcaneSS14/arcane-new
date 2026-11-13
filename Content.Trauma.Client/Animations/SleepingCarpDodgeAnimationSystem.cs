using System.Numerics;
using Content.Trauma.Shared.MartialArts;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Trauma.Client.Animations;

public sealed partial class SleepingCarpDodgeAnimationSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animation = default!;

    private const string AnimationKey = "sleeping-carp-dodge";

    public override void Initialize()
    {
        SubscribeNetworkEvent<SleepingCarpDodgeEffectEvent>(OnDodge);
    }

    private void OnDodge(SleepingCarpDodgeEffectEvent ev)
    {
        if (!TryGetEntity(ev.Entity, out var uid) || !TryComp<SpriteComponent>(uid, out var sprite) ||
            _animation.HasRunningAnimation(uid.Value, AnimationKey))
            return;

        var direction = ev.Left ? -1f : 1f;
        var offset = sprite.Offset;
        var rotation = sprite.Rotation;
        var displacement = new Vector2(direction * 0.3f, 0f);
        var lean = Angle.FromDegrees(-direction * 28f);
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(0.35),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(offset, 0f),
                        new AnimationTrackProperty.KeyFrame(offset + displacement, 0.07f),
                        new AnimationTrackProperty.KeyFrame(offset + displacement, 0.1f),
                        new AnimationTrackProperty.KeyFrame(offset - displacement * 0.12f, 0.11f),
                        new AnimationTrackProperty.KeyFrame(offset, 0.07f),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(rotation, 0f),
                        new AnimationTrackProperty.KeyFrame(rotation + lean, 0.08f),
                        new AnimationTrackProperty.KeyFrame(rotation + lean, 0.09f),
                        new AnimationTrackProperty.KeyFrame(rotation + Angle.FromDegrees(direction * 5.04f), 0.11f),
                        new AnimationTrackProperty.KeyFrame(rotation, 0.07f),
                    },
                },
            },
        };
        _animation.Play(uid.Value, animation, AnimationKey);
    }
}
