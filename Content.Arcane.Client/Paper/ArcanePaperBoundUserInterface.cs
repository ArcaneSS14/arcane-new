using Content.Arcane.Shared.Paper;
using Content.Client.Paper.UI;
using Robust.Client.UserInterface;

namespace Content.Arcane.Client.Paper;

public sealed class ArcanePaperBoundUserInterface(EntityUid owner, Enum uiKey) : PaperBoundUserInterface(owner, uiKey)
{
    protected override PaperWindow CreatePaperWindow()
    {
        var window = this.CreateWindow<ArcanePaperWindow>();
        window.OnFieldSubmitted += (kind, index, value, snapshot) =>
            SendMessage(new PaperFieldMessage(kind, index, value, snapshot));
        return window;
    }
}
