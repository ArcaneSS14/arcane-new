using Content.Shared.DoAfter;
using Robust.Shared.Map;

namespace Content.Arcane.Shared.ModSuits;

[Serializable, NetSerializable]
public sealed partial class ModClampGrabDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ModClampDropDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetCoordinates Destination { get; private set; }

    private ModClampDropDoAfterEvent()
    {
    }

    public ModClampDropDoAfterEvent(NetCoordinates destination)
    {
        Destination = destination;
    }

    public override DoAfterEvent Clone() => new ModClampDropDoAfterEvent(Destination);
}
