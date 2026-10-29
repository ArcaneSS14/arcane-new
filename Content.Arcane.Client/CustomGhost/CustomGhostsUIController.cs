using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Arcane.Client.CustomGhost;

/// <summary>
/// Opens the custom ghost menu from the lobby.
/// </summary>
public sealed class CustomGhostsUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    private LobbyGui? _lobby;
    private CustomGhostsWindow? _window;

    public void OnStateEntered(LobbyState state)
    {
        _lobby = state.Lobby;
        if (_lobby != null)
            _lobby.CustomGhostsButton.OnPressed += OnPressed;
    }

    public void OnStateExited(LobbyState state)
    {
        if (_lobby != null)
            _lobby.CustomGhostsButton.OnPressed -= OnPressed;

        _lobby = null;
        _window?.Close();
        _window = null;
    }

    private void OnPressed(BaseButton.ButtonEventArgs args)
    {
        if (_window is { IsOpen: true })
        {
            _window.MoveToFront();
            return;
        }

        _window = new CustomGhostsWindow();
        _window.OnClose += () => _window = null;
        _window.OpenCenteredLeft();
    }
}
