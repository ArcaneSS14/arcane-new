using Content.Arcane.Shared.Thunderdome;
using Content.Client.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.Thunderdome;

[UsedImplicitly]
public sealed class ThunderdomeLoadoutEui : BaseEui
{
    private readonly DefaultWindow _window = new() { Title = Loc.GetString("thunderdome-loadout-title"), MinSize = new(380, 420) };
    private readonly BoxContainer _choices = new() { Orientation = BoxContainer.LayoutOrientation.Vertical };
    private readonly IEntitySystemManager _systems = IoCManager.Resolve<IEntitySystemManager>();
    private readonly IPrototypeManager _prototypes = IoCManager.Resolve<IPrototypeManager>();

    public ThunderdomeLoadoutEui()
    {
        _window.Contents.AddChild(new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = { _choices },
        });
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened() => _window.OpenCentered();

    public override void Closed() => _window.Close();

    public override void HandleState(EuiStateBase state)
    {
        if (state is not ThunderdomeLoadoutState loadouts)
            return;

        _choices.DisposeAllChildren();
        string? category = null;
        for (var index = 0; index < loadouts.Options.Length; index++)
        {
            var option = loadouts.Options[index];
            if (option.Category != category)
            {
                category = option.Category;
                _choices.AddChild(new Label { Text = category });
            }

            var choice = index;
            var row = new BoxContainer();
            if (_prototypes.TryIndex(option.Sprite, out var prototype))
                row.AddChild(new TextureRect { Texture = _systems.GetEntitySystem<SpriteSystem>().GetPrototypeIcon(prototype).Default });

            var button = new Button { Text = option.Name, ToolTip = option.Description, HorizontalExpand = true };
            button.OnPressed += _ => SendMessage(new ThunderdomeSelectLoadoutMessage(choice));
            row.AddChild(button);
            _choices.AddChild(row);
        }
    }
}
