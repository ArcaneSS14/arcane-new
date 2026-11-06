using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Research.UI;

/// <summary>Presentation contract for an optional research journal supplied by a client module.</summary>
public abstract class ResearchJournalWindow : FancyWindow
{
    public static Func<ResearchJournalWindow>? Factory { get; set; }

    public abstract void SetExperimentEntries(List<Control> entries);
    public abstract void SetLogsMessage(FormattedMessage message);
}
