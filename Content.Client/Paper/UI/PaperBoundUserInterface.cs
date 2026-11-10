using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
[Virtual] // Arcane
public class PaperBoundUserInterface : BoundUserInterface // Arcane-Edit: owner-local paper window factory
{
    [ViewVariables]
    private PaperWindow? _window;
    private Guid _saveId; // Arcane

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = CreatePaperWindow(); // Arcane-Edit
        _window.OnSaved += InputOnTextEntered;

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }
    }

    protected virtual PaperWindow CreatePaperWindow() => this.CreateWindow<PaperWindow>(); // Arcane

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.Populate((PaperBoundUserInterfaceState) state);
    }

    private void InputOnTextEntered(string text)
    {
        // Arcane-Edit-Start
        _saveId = Guid.NewGuid();
        SendPredictedMessage(new PaperInputTextMessage(text, _saveId));
        // Retain the draft; a reply from an earlier save/window cannot acknowledge it.
        // Arcane-Edit-End
    }

    // Arcane-Start
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is PaperSaveResultMessage result && result.SaveId == _saveId)
            _window?.ApplySaveResult(result.Accepted);
    }
    // Arcane-End
}
