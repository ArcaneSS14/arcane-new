using Content.Client.Options.UI.Tabs;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Arcane.Client.Actions;

public sealed class ActionBarSettingsUIController : UIController
{
    public override void Initialize()
    {
        base.Initialize();
        MiscTab.PopulateOptions += OnPopulateOptions;
    }

    private void OnPopulateOptions(MiscTab tab, BoxContainer options)
    {
        var settings = new ActionBarSettings();
        settings.Bind(tab, UIManager.GetUIController<ActionsBarLayoutUIController>());
        options.AddChild(settings);
    }
}
