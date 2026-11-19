using Content.Shared.DoAfter;

namespace Content.Arcane.Shared.Chaplain;

[Serializable, NetSerializable]
public sealed partial class NullrodRecallDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>A different mind cannot finish the original chaplain's prayer.</summary>
    [DataField]
    public NetEntity Mind;
}
