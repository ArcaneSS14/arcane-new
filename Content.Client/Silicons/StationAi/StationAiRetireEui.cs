using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiRetireEui : BaseEui
{
    private readonly DefaultWindow _window;

    public StationAiRetireEui()
    {
        var layout = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
        };
        layout.AddChild(new Label { Text = Loc.GetString("station-ai-retire-prompt") });
        var confirm = new Button { Text = Loc.GetString("station-ai-retire-confirm") };
        confirm.OnPressed += _ => SendMessage(new ConfirmStationAiRetireMessage());
        layout.AddChild(confirm);

        _window = new DefaultWindow { Title = Loc.GetString("station-ai-retire-title") };
        _window.Contents.AddChild(layout);
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened() => _window.OpenCentered();

    public override void Closed() => _window.Close();
}
