using Content.Arcane.Shared.PainFlash;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Arcane.PainFlash;

public sealed partial class PainFlashTestSystem : EntitySystem
{
    public void Send(PainFlashMessage message, ICommonSession recipient)
    {
        RaiseNetworkEvent(message, recipient);
    }
}
