using Content.Arcane.Client.Research.UI;
using Content.Client.Research.UI;

namespace Content.Arcane.Client.Research;

/// <summary>
/// Provides the Arcane journal window to research consoles.
/// </summary>
public sealed class ResearchJournalSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        ResearchJournalWindow.Factory = CreateWindow;
    }

    public override void Shutdown()
    {
        if (ResearchJournalWindow.Factory == CreateWindow)
            ResearchJournalWindow.Factory = null;
        base.Shutdown();
    }

    private static ResearchJournalWindow CreateWindow() => new FancyResearchConsoleLogWindow();
}
