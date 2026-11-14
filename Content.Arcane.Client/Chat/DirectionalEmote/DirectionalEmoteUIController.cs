using Content.Client.Chat.TypingIndicator;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;

namespace Content.Arcane.Client.Chat.DirectionalEmote;

[UsedImplicitly]
public sealed partial class DirectionalEmoteUIController : UIController
{
    [UISystemDependency] private readonly DirectionalEmoteSystem _emote = default!;
    [UISystemDependency] private readonly TypingIndicatorSystem _typingIndicator = default!;

    [Dependency] private IPlayerManager _player = default!;

    private DirectionalEmoteWindow? _window;

    public void ToggleWindow(NetEntity target)
    {
        if (_window is { Disposed: false, IsOpen: true })
        {
            CloseWindow();
            return;
        }

        _window = UIManager.CreateWindow<DirectionalEmoteWindow>();
        _window.Target = target;
        _window.CanHideName = _emote.CanHideName(_player.LocalEntity);
        _window.OnClose += () => _typingIndicator.ClientChangedWindowStatus(false);
        _window.MessageChanged += () => _typingIndicator.ClientChangedChatText();
        _window.AcceptPressed += () =>
        {
            if (_window == null)
                return;

            _typingIndicator.ClientSubmittedChatText();
            _emote.Send(_window.Target, _window.Text, _window.HideName);
            CloseWindow();
        };

        _window.OpenCentered();
        _window.MoveToFront();
        _typingIndicator.ClientChangedWindowStatus(true);
    }

    private void CloseWindow()
    {
        _window?.Close();
        _window = null;
    }
}
