using Content.Arcane.Shared.MartialArts;
using Content.Shared.Rotation;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.MartialArts;

/// <summary>
/// Plays the spins and somersaults of <see cref="CapoeiraFlowUserComponent"/>.
/// </summary>
public sealed partial class CapoeiraFlowVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const string FlipKey = "capoeira-flip";
    private const float FlipSeconds = 0.4f;
    private const int SpinSteps = 4;

    private static readonly TimeSpan SpinTime = TimeSpan.FromSeconds(0.4);

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CapoeiraFlowUserComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            UpdateSpin((uid, comp), sprite, now);
            UpdateFlip((uid, comp), sprite);
        }
    }

    private void UpdateSpin(Entity<CapoeiraFlowUserComponent> ent, SpriteComponent sprite, TimeSpan now)
    {
        var comp = ent.Comp;
        if (comp.Spins != comp.ShownSpins && now >= comp.SpinEnd)
        {
            comp.ShownSpins = comp.Spins;
            comp.SpinEnd = now + SpinTime;
            comp.SpinStart = _transform.GetWorldRotation(ent).GetCardinalDir();
            comp.Spinning = true;
        }

        if (!comp.Spinning)
            return;

        if (now >= comp.SpinEnd)
        {
            comp.Spinning = false;
            sprite.EnableDirectionOverride = false;
            return;
        }

        // direction overrides are not touched by combat mode turning towards the cursor
        var progress = 1f - (float) ((comp.SpinEnd - now) / SpinTime);
        var step = Math.Min((int) (progress * SpinSteps), SpinSteps - 1);
        var angle = comp.SpinStart.ToAngle() + Angle.FromDegrees(360f / SpinSteps * step);
        sprite.DirectionOverride = angle.GetCardinalDir();
        sprite.EnableDirectionOverride = true;
    }

    private void UpdateFlip(Entity<CapoeiraFlowUserComponent> ent, SpriteComponent sprite)
    {
        var comp = ent.Comp;
        var running = _animation.HasRunningAnimation(ent, FlipKey);
        if (!IsUpright(ent))
        {
            comp.ShownFlips = comp.Flips;
            if (!running)
                return;

            // the flip would finish upright and undo the lying down rotation
            _animation.Stop(ent.Owner, FlipKey);
            _appearance.OnChangeData(ent, sprite);
            return;
        }

        if (comp.Flips == comp.ShownFlips || running)
            return;

        comp.ShownFlips = comp.Flips;
        PlayFlip(ent, sprite.Rotation);
    }

    private void PlayFlip(EntityUid uid, Angle start)
    {
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(FlipSeconds),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(start, 0f),
                        new AnimationTrackProperty.KeyFrame(start + Angle.FromDegrees(180), FlipSeconds / 2),
                        new AnimationTrackProperty.KeyFrame(start + Angle.FromDegrees(360), FlipSeconds / 2),
                        new AnimationTrackProperty.KeyFrame(start, 0f),
                    },
                },
            },
        };

        _animation.Play(uid, animation, FlipKey);
    }

    private bool IsUpright(EntityUid uid)
        => !_appearance.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state)
            || state == RotationState.Vertical;
}
