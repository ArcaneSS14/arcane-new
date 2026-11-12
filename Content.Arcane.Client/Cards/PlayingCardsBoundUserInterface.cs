using Content.Arcane.Shared.Cards;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Arcane.Client.Cards;

[UsedImplicitly]
public sealed class PlayingCardsBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PlayingCardsWindow? _window;
    private PlayingCardsSnapshotMessage? _snapshot;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<PlayingCardsWindow>();
        _window.DrawCard += DrawCard;
        if (_snapshot != null)
            _window.UpdateCards(_snapshot);
        SendMessage(new PlayingCardsRefreshMessage());
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is PlayingCardsSnapshotMessage snapshot)
        {
            _snapshot = snapshot;
            _window?.UpdateCards(snapshot);
        }
    }

    private void DrawCard(int revision, int index) => SendMessage(new PlayingCardsDrawMessage(revision, index));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _snapshot = null;
            if (_window != null)
            {
                _window.DrawCard -= DrawCard;
                _window.Dispose();
                _window = null;
            }
        }
        base.Dispose(disposing);
    }
}
