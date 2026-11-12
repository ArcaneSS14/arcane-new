using Content.Arcane.Client.Serialization;
using Robust.Shared.ContentPack;

namespace Content.Arcane.Client.Entry;

public sealed class EntryPoint : GameClient
{
    public override void PostInit()
    {
        base.PostInit();

        new ComponentCopyWarmup().Start();
    }
}
