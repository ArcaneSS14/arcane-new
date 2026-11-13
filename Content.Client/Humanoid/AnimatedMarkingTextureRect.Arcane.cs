// Arcane-Start
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
// Arcane-End

namespace Content.Client.Humanoid; // Arcane

// Arcane-Start
/// <summary>
/// Plays marking RSI frames without changing the saved marking or the editor's layout.
/// </summary>
public sealed partial class AnimatedMarkingTextureRect : LayeredTextureRect
{
    [Dependency] private IGameTiming _timing = default!;

    private List<IRsiStateLike> _states = [];
    public bool Animate { get; set; }

    public AnimatedMarkingTextureRect() => IoCManager.InjectDependencies(this);

    public void SetStates(List<IRsiStateLike> states)
    {
        _states = states;
        Textures = states.Select(state => state.GetFrame(GetDirection(state), 0)).ToList();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!VisibleInTree)
            return;

        for (var i = 0; i < _states.Count; i++)
        {
            var state = _states[i];
            var frame = 0;
            if (Animate && state.IsAnimated)
            {
                var total = 0f;
                for (var j = 0; j < state.AnimationFrameCount; j++)
                    total += state.GetDelay(j);

                if (total > 0f)
                {
                    var time = (float) (_timing.RealTime.TotalSeconds % total);
                    while (frame < state.AnimationFrameCount - 1 && (time -= state.GetDelay(frame)) >= 0f)
                        frame++;
                }
            }

            // All frames of an RSI state have the same size, so a frame change needs no new layout measurement.
            Textures[i] = state.GetFrame(GetDirection(state), frame);
        }
    }

    private static RsiDirection GetDirection(IRsiStateLike state)
        => state.RsiDirections == RsiDirectionType.Dir1 ? RsiDirection.South : RsiDirection.North;
}
// Arcane-End
