using Content.Arcane.Shared.ReadyManifest;
using Content.Client.GameTicking;
using Robust.Shared.Timing;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Arcane.Client.ReadyManifest;

/// <summary>
/// Hooks the lobby manifest button and keeps it enabled only before the round starts.
/// </summary>
[UsedImplicitly]
public sealed class ReadyManifestUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [UISystemDependency] private readonly ClientGameTicker? _gameTicker = default!;

    private LobbyGui? _lobby;

    public void OnStateEntered(LobbyState state)
    {
        _lobby = state.Lobby;
        if (_lobby == null)
            return;

        _lobby.ManifestButton.OnPressed += OnManifestPressed;
    }

    public void OnStateExited(LobbyState state)
    {
        if (_lobby != null)
            _lobby.ManifestButton.OnPressed -= OnManifestPressed;

        _lobby = null;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_lobby != null && _gameTicker != null)
            _lobby.ManifestButton.Disabled = _gameTicker.IsGameStarted;
    }

    private void OnManifestPressed(BaseButton.ButtonEventArgs args)
    {
        EntityManager.EntityNetManager.SendSystemNetworkMessage(new RequestReadyManifestMessage());
    }
}
