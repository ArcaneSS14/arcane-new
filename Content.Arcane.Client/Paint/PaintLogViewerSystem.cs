using System.Linq;
using System.Numerics;
using Content.Arcane.Shared.Paint;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Administration.UI.Logs;
using Content.Shared.Database;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Arcane.Client.Paint;

public sealed partial class PaintLogViewerSystem : EntitySystem
{
    [Dependency] private IClyde _clyde = default!;
    private readonly HashSet<DefaultWindow> _windows = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PaintLogImageResponseEvent>(OnImageResponse);
        AdminLogsControl.LogControlsAdded += AddLogControl;
    }

    public override void Shutdown()
    {
        AdminLogsControl.LogControlsAdded -= AddLogControl;
        foreach (var window in _windows.ToArray())
            window.Close();
        base.Shutdown();
    }

    private void AddLogControl(AdminLogLabel label, Control parent, int round)
    {
        if (label.Log.Type != LogType.Paint || !PaintLogToken.TryFind(label.Log.Message, out var token))
            return;

        var button = new Button
        {
            Text = Loc.GetString("paint-log-view-button"),
            HorizontalAlignment = Control.HAlignment.Left,
            Visible = label.Visible,
        };
        button.OnPressed += _ => RaiseNetworkEvent(new PaintLogImageRequestEvent(round, token));
        label.OnVisibilityChanged += control => button.Visible = control.Visible;
        parent.AddChild(button);
    }

    private void OnImageResponse(PaintLogImageResponseEvent ev)
    {
        var window = new DefaultWindow
        {
            Title = Loc.GetString("paint-log-viewer-title", ("token", ev.Token)),
            MinSize = new Vector2(420, 440),
        };
        OwnedTexture? texture = null;
        if (ev.Image is { } image && image.IsValid())
        {
            texture = PaintTextures.Create(_clyde, image, $"paint-log-{ev.Token}");
            window.Contents.AddChild(new PaintCanvasControl
            {
                Interactive = false,
                Texture = texture,
                ImageSize = image.Size,
                HorizontalExpand = true,
                VerticalExpand = true,
            });
        }
        else
            window.Contents.AddChild(new Label { Text = Loc.GetString("paint-log-viewer-missing") });

        _windows.Add(window);
        window.OnClose += () =>
        {
            _windows.Remove(window);
            window.Dispose();
            texture?.Dispose();
        };
        window.OpenCentered();
    }
}
