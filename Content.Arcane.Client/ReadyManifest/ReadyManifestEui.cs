using Content.Arcane.Shared.ReadyManifest;
using Content.Client.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Arcane.Client.ReadyManifest;

[UsedImplicitly]
public sealed class ReadyManifestEui : BaseEui
{
    private readonly ReadyManifestWindow _window = new();

    public ReadyManifestEui()
    {
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is ReadyManifestEuiState manifest)
            _window.Rebuild(manifest.JobCharacters);
    }
}
