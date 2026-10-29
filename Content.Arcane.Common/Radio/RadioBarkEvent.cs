using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Arcane.Common.Radio;

/// <summary>
/// Tells a client to play the bark of a radio message it just received (Orion #452, #456).
/// </summary>
[Serializable, NetSerializable]
public sealed class RadioBarkEvent(ResPath path, AudioParams audioParams) : EntityEventArgs
{
    public readonly ResPath Path = path;
    public readonly AudioParams Params = audioParams;
}
