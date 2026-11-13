using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Paint;

[Serializable, NetSerializable]
public sealed partial class PhotocopierDoAfterEvent : DoAfterEvent
{
    [DataField]
    public int Operation;

    [DataField]
    public PhotocopierState Stage;

    public override DoAfterEvent Clone() => (PhotocopierDoAfterEvent) MemberwiseClone();
}
