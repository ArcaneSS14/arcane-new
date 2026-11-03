using Content.Client.UserInterface.Controls;

namespace Content.Arcane.Client.Bitrunning.UI.Disk;

[GenerateTypedNameReferences]
public sealed partial class BitrunningDiskWindow : FancyWindow
{
    public event Action<string>? OnSelected;

    public BitrunningDiskWindow()
    {
        RobustXamlLoader.Load(this);
    }

    public void SetState(List<string> options, string? selectedOption)
    {
        OptionsContainer.RemoveAllChildren();

        if (selectedOption != null)
        {
            var selectedLabel = new Label
            {
                Text = Loc.GetString("bitrunning-disk-ui-selected"),
            };
            OptionsContainer.AddChild(selectedLabel);
            return;
        }

        foreach (var option in options)
        {
            // Options are locale ids; the id itself is still what gets sent back.
            var button = new Button
            {
                Text = Loc.TryGetString(option, out var localized) ? localized : option,
            };
            button.OnPressed += _ => OnSelected?.Invoke(option);
            OptionsContainer.AddChild(button);
        }
    }
}
