// Arcane-Start
using System.Numerics;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void AddRoleHighlightEditorButton(BoxContainer jobContainer, JobPrototype job)
    {
        var button = new Button
        {
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(3f, 3f, 0f, 0f),
            MinWidth = 32f,
            ToolTip = Loc.GetString("role-highlight-editor-open-tooltip"),
        };

        button.AddChild(new TextureRect
        {
            TexturePath = "/Textures/_Arcane/Interface/ink_highlighter.svg.96dpi.png",
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            TextureScale = new Vector2(0.75f, 0.75f),
        });

        button.OnPressed += _ =>
        {
            var ev = new OpenRoleHighlightEditorEvent(job.ID);
            _entManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
        };

        jobContainer.AddChild(button);
    }
}
// Arcane-End
