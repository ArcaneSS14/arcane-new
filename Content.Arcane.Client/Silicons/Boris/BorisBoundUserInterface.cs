using Content.Arcane.Shared.Silicons.Boris;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Arcane.Client.Silicons.Boris;

[UsedImplicitly]
public sealed class BorisBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BoxContainer? _deviceList;

    protected override void Open()
    {
        base.Open();
        var window = this.CreateWindow<FancyWindow>();
        window.Title = Loc.GetString("boris-title");
        window.MinSize = new Vector2(400, 240);
        _deviceList = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        var scroll = new ScrollContainer { HorizontalExpand = true, VerticalExpand = true };
        scroll.AddChild(_deviceList);
        window.ContentsContainer.AddChild(scroll);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not BorisDeviceState devices || _deviceList == null)
            return;

        _deviceList.RemoveAllChildren();
        if (devices.Devices.Count == 0)
            _deviceList.AddChild(new Label { Text = Loc.GetString("boris-no-devices") });

        foreach (var device in devices.Devices)
        {
            var row = new BoxContainer();
            row.AddChild(new Label { Text = device.Name, HorizontalExpand = true });
            var view = new Button { Text = Loc.GetString("boris-view") };
            var control = new Button { Text = Loc.GetString("boris-control") };
            view.OnPressed += _ => SendMessage(new BorisDeviceMessage(device.Target, BorisDeviceAction.View));
            control.OnPressed += _ => SendMessage(new BorisDeviceMessage(device.Target, BorisDeviceAction.Control));
            row.AddChild(view);
            row.AddChild(control);
            _deviceList.AddChild(row);
        }
    }
}
