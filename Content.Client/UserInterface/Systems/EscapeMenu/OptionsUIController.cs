using Content.Client.Options.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Console;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed partial class OptionsUIController : UIController
{
    [Dependency] private IConsoleHost _con = default!;

    public override void Initialize()
    {
        _con.RegisterCommand("options", Loc.GetString("cmd-options-desc"), Loc.GetString("cmd-options-help"), OptionsCommand);
    }

    private void OptionsCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            ToggleWindow();
            return;
        }
        OpenWindow();

        if (!int.TryParse(args[0], out var tab))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[0])));
            return;
        }

        _optionsWindow.Tabs.CurrentTab = tab;
    }

    private OptionsMenu _optionsWindow = default!;

    private void EnsureWindow()
    {
        if (_optionsWindow is { Disposed: false })
            return;

        _optionsWindow = UIManager.CreateWindow<OptionsMenu>();
    }

    public void OpenWindow()
    {
        EnsureWindow();

        _optionsWindow.ReloadLocalization(); // Arcane
        _optionsWindow.UpdateTabs();

        _optionsWindow.OpenCentered();
        _optionsWindow.MoveToFront();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_optionsWindow.IsOpen)
        {
            _optionsWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }

    // Arcane-Start
    /// <summary>Refreshes the existing open window without loading or saving settings.</summary>
    public void ReloadLocalization()
    {
        if (_optionsWindow is { Disposed: false, IsOpen: true })
            _optionsWindow.ReloadLocalization();
    }
    // Arcane-End
}
