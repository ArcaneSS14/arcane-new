// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Shared.Configuration;
using System.Numerics;

namespace Content.Client.UserInterface.Controls;

public partial class RadialMenu
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private readonly UiMotionContainer _motionExit = new()
    {
        EnabledSetting = UiMotionSettings.Radial,
        ClosingSetting = UiMotionSettings.Radial
    };

    private void InitializeRadialMotion()
    {
        OnOpen += () =>
        {
            UpdateRadialMotion();
            _motionExit.Show();
        };
    }

    private void UpdateRadialMotion()
    {
        _motionExit.Profile = new UiMotionProfile(TimeSpan.Zero,
            TimeSpan.Zero,
            0, false) { Opening = false, Sequence = false };
    }

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        if (childIndex == 0)
        {
            UpdateRadialMotion();
            _motionExit.CaptureControl(this, args);
        }
        base.RenderChildOverride(ref args, childIndex, position);
    }

    public override void Close()
    {
        if (IsOpen)
        {
            UpdateRadialMotion();
            _motionExit.CreateExitVisual(this);
        }
        base.Close();
    }

    protected override void ExitedTree()
    {
        _motionExit.ReleaseTexture();
        base.ExitedTree();
    }
}
// Arcane-End
