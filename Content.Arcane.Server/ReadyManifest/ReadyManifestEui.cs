using Content.Arcane.Shared.ReadyManifest;
using Content.Server.EUI;
using Content.Shared.Eui;

namespace Content.Arcane.Server.ReadyManifest;

public sealed class ReadyManifestEui(ReadyManifestSystem system) : BaseEui
{
    public override EuiStateBase GetNewState()
    {
        return new ReadyManifestEuiState(system.BuildManifest());
    }

    public override void Closed()
    {
        base.Closed();

        system.OnEuiClosed(Player);
    }
}
