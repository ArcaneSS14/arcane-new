// Arcane-Start
using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Motion;

/// <summary>Opt-in sequential appearance; layout, visibility and input remain available immediately.</summary>
public sealed class UiMotionGridContainer : GridContainer
{
    public UiMotionContainer? Motion { get; set; }
    public int MaxStaggeredChildren { get; set; } = int.MaxValue;

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        var original = args.Modulate;
        var count = Math.Min(ChildCount, Math.Max(1, MaxStaggeredChildren));
        var opacity = Motion?.GetStaggeredOpacity(childIndex, count) ?? 1f;
        args.Modulate = original.WithAlpha(original.A * opacity);
        base.RenderChildOverride(ref args, childIndex, position);
        args.Modulate = original;
    }
}
// Arcane-End
