// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Controls;

public partial class FancyWindow
{
    private UiMotionContainer _motionHost = default!;
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();

    private void InitializeWindowMotion()
    {
        // FancyWindow's children overlap. Keep that arrangement in one capture surface;
        // named content controls and XamlChildren keep their original identity.
        var surface = new Control();
        while (ChildCount > 0)
        {
            var child = GetChild(0);
            RemoveChild(child);
            surface.AddChild(child);
        }
        _motionHost = new UiMotionContainer { ClosingSetting = UiMotionSettings.WindowClose };
        _motionHost.AddChild(surface);
        AddChild(_motionHost);
        OnOpen += () =>
        {
            UpdateWindowMotionProfile();
            _motionHost.Show();
        };
    }

    private void UpdateWindowMotionProfile()
    {
        _motionHost.Profile = new UiMotionProfile(TimeSpan.Zero,
            TimeSpan.FromMilliseconds(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.WindowExitMs, 0, 1000)),
            3, false) { Opening = false, Sequence = false };
    }

    public override void Close()
    {
        if (IsOpen)
        {
            UpdateWindowMotionProfile();
            _motionHost.CreateExitVisual();
        }
        base.Close();
    }
}
// Arcane-End
