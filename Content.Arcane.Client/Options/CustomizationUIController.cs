using Content.Arcane.Client.Options.UI.Tabs;
using Content.Client.Options.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Arcane.Client.Options;

/// <summary>Registers the customization tab before settings can be opened from the main menu.</summary>
[UsedImplicitly]
public sealed class CustomizationUIController : UIController
{
    public override void Initialize()
    {
        base.Initialize();
        OptionsMenu.ConfigureExtensions += ConfigureOptions;
    }

    private static void ConfigureOptions(OptionsMenu menu)
    {
        var tab = new CustomizationTab();
        menu.Tabs.AddChild(tab);
        menu.Tabs.SetTabTitle(menu.Tabs.ChildCount - 1, Loc.GetString("ui-options-tab-customization"));
        menu.ReloadExtensions += tab.Control.ReloadValues;
    }
}
