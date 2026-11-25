using Content.Shared.FixedPoint;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.PainFlash;

public sealed class PainFlashOverlay : Overlay
{
    private const float MaxVisiblePain = 100f;
    private const float MaxAccumulatedPain = 200f;
    private const float MinimumVisiblePain = 5f;
    private const float PainDecayPerSecond = 40f;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => false;

    public float Intensity { get; set; } = 1f;
    public float CurrentPain => _painAccumulator;

    private float _painAccumulator;

    public void Reset()
    {
        _painAccumulator = 0f;
    }

    public void AddPain(FixedPoint2 pain)
    {
        _painAccumulator = Math.Clamp(
            _painAccumulator + pain.Float(),
            MinimumVisiblePain,
            MaxAccumulatedPain);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_painAccumulator <= 0f)
            return;

        _painAccumulator = Math.Max(0f, _painAccumulator - args.DeltaSeconds * PainDecayPerSecond);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_painAccumulator <= 0f || Intensity <= 0f)
            return;

        var alpha = Math.Clamp(_painAccumulator / MaxVisiblePain, 0f, 1f) * Intensity;
        args.WorldHandle.DrawRect(args.WorldBounds, Color.Red.WithAlpha(alpha));
    }
}
