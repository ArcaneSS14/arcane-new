using Content.Arcane.Shared.Thunderdome;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Arcane.Client.Thunderdome;

public sealed partial class ThunderdomeRevivalSystem : EntitySystem
{
    private DefaultWindow? _window;

    public override void Initialize()
    {
        SubscribeNetworkEvent<ThunderdomeRevivalOfferEvent>(OnOffer);
    }

    public override void Shutdown()
    {
        _window?.Dispose();
        _window = null;
        base.Shutdown();
    }

    private void OnOffer(ThunderdomeRevivalOfferEvent args)
    {
        _window?.Dispose();
        var window = new DefaultWindow { Title = Loc.GetString("thunderdome-revival-title") };
        _window = window;
        var content = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        content.AddChild(new Label { Text = Loc.GetString("thunderdome-revival-offer") });
        var accept = new Button { Text = Loc.GetString("thunderdome-revival-accept") };
        accept.OnPressed += _ =>
        {
            RaiseNetworkEvent(new ThunderdomeRevivalAcceptEvent());
            window.Close();
        };
        var decline = new Button { Text = Loc.GetString("thunderdome-revival-decline") };
        decline.OnPressed += _ => window.Close();
        content.AddChild(accept);
        content.AddChild(decline);
        window.Contents.AddChild(content);
        window.OpenCentered();
    }
}
