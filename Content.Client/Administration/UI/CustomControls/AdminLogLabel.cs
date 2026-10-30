using Content.Client.UserInterface.Controls;
using Content.Shared.Administration.Logs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
// Arcane-Start
using Content.Shared.Database;
using Robust.Shared.Utility;
// Arcane-End

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogLabel : RichTextLabel
{
    public AdminLogLabel(ref SharedAdminLog log, Separator separator)
    {
        Log = log;
        Separator = separator;

        var localTime = log.Date.ToLocalTime();

        // Arcane-Edit-Start: colored impact marker
        var formatted = new FormattedMessage();
        formatted.AddMarkupOrThrow($"[color={GetImpactColor(log.Impact)}]█[/color] [bold]{localTime:HH:mm:ss}[/bold]: ");
        formatted.AddText(log.Message);
        SetMessage(formatted);
        // Arcane-Edit-End
        OnVisibilityChanged += VisibilityChanged;
    }

    public new SharedAdminLog Log { get; }

    public Separator Separator { get; }

    private void VisibilityChanged(Control control)
    {
        Separator.Visible = Visible;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        OnVisibilityChanged -= VisibilityChanged;
    }

    // Arcane-Start
    private static string GetImpactColor(LogImpact impact) => impact switch
    {
        LogImpact.Extreme => "red",
        LogImpact.High => "orange",
        LogImpact.Medium => "yellow",
        LogImpact.Low => "lightgreen",
        _ => "gray"
    };
    // Arcane-End
}
