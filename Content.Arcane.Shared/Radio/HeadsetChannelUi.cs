using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Radio;

[NetSerializable, Serializable]
public enum HeadsetChannelUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class HeadsetChannelUiUpdateMessage : BoundUserInterfaceMessage
{
    /// <summary>
    ///     The radio channels available on the headset.
    /// </summary>
    public List<ProtoId<RadioChannelPrototype>> Channels;

    public HashSet<int> MutedFrequencies;

    public HeadsetChannelUiUpdateMessage(List<ProtoId<RadioChannelPrototype>> channels, HashSet<int> mutedFrequencies)
    {
        Channels = channels;
        MutedFrequencies = mutedFrequencies;
    }
}

[Serializable, NetSerializable]
public sealed class HeadsetChannelUiRequestMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class HeadsetChannelMuteMessage : BoundUserInterfaceMessage
{
    public int Frequency;
    public bool Muted;

    public HeadsetChannelMuteMessage(int frequency, bool muted)
    {
        Frequency = frequency;
        Muted = muted;
    }
}
