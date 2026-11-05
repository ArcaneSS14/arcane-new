// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Gameplay;
using Content.Client.GameTicking.Managers;
using Content.Client.UserInterface.Screens;
using Content.Trauma.Client.UserActions;
using Robust.Client.UserInterface.Controllers;

// Arcane-Start
using System.Globalization;
using Content.Client.Localizations;
// Arcane-End

namespace Content.Trauma.Client.UserInterface;

public sealed partial class UserActionsPanelUIController : UIController, IOnStateEntered<GameplayState>
{
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private DynamicLocalizationManager _dynamicLocalization = default!; // Arcane

    public override void Initialize()
    {
        base.Initialize();

        SeparatedChatGameScreen.OnCreated += InjectPanel;
        _dynamicLocalization.CultureChanged += OnCultureChanged; // Arcane
    }

    public void OnStateEntered(GameplayState state)
    {
        if (_uiManager.ActiveScreen is not SeparatedChatGameScreen screen)
            return;

        InjectPanel(screen);
    }

    public void InjectPanel(SeparatedChatGameScreen container)
    {
        UserActionsPanel? panel;

        if (container.UserActionsPlaceholder.ChildCount > 0)
        {
            panel = container.UserActionsPlaceholder.GetChild(0) as UserActionsPanel;
        }
        else
        {
            panel = new UserActionsPanel();
            container.UserActionsPlaceholder.AddChild(panel);
        }

        panel?.ReloadLocalization(); // Arcane-Edit: UpdateTabs() > ReloadLocalization()
    }

    // Arcane-Start
    private void OnCultureChanged(CultureInfo previous, CultureInfo current)
    {
        if (_uiManager.ActiveScreen is SeparatedChatGameScreen screen)
            InjectPanel(screen);
    }
    // Arcane-End
}
