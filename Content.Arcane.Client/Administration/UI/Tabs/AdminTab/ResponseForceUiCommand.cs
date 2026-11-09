using Robust.Shared.Console;

namespace Content.Arcane.Client.Administration.UI.Tabs.AdminTab;

/// <summary>
///     Opens the window for calling a response force.
/// </summary>
public sealed class ResponseForceUiCommand : LocalizedCommands
{
    public override string Command => "responseforceui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        new ResponseForceWindow().OpenCentered();
    }
}
